using UnityEditor;
using UnityEngine;
using System.IO;

namespace AIBeat.Editor
{
    public static class ScreenshotCapture
    {
        private static string screenshotFolder = "Screenshots";
        private static string pendingFilePath = null;

        [MenuItem("Tools/A.I. BEAT/Capture Screenshot _F12")]
        public static void CaptureScreenshot()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ScreenCapture] Play 모드에서만 스크린샷을 캡처할 수 있습니다.");
                return;
            }

            // Screenshots 폴더 생성
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string folderPath = Path.Combine(projectPath, screenshotFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 파일명 생성 (타임스탬프)
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"AIBeat_{timestamp}.png";
            pendingFilePath = Path.Combine(folderPath, fileName);

            // Game View 포커스 및 리페인트
            var gameView = GetGameViewWindow();
            if (gameView != null)
            {
                gameView.Focus();
                gameView.Repaint();
            }

            // 다음 프레임에서 캡처 (렌더링 완료 후)
            EditorApplication.delayCall += DelayedCapture;
            Debug.Log("[ScreenCapture] 스크린샷 캡처 예약됨...");
        }

        private static void DelayedCapture()
        {
            if (string.IsNullOrEmpty(pendingFilePath)) return;

            // 한번 더 딜레이 (2프레임 대기)
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    Debug.LogWarning("[ScreenCapture] Play 모드가 종료되어 캡처 취소됨");
                    pendingFilePath = null;
                    return;
                }

                // Game View 강제 포커스
                var gameView = GetGameViewWindow();
                if (gameView != null)
                {
                    gameView.Focus();
                    gameView.Repaint();
                }

                // 런타임 헬퍼를 리플렉션 없이 찾아서 호출 (타입 이름으로 검색)
                TriggerRuntimeCapture(pendingFilePath);
                pendingFilePath = null;
            };
        }

        private static void TriggerRuntimeCapture(string filePath)
        {
            // ScreenshotHelper 찾기 또는 생성 (타입명으로 동적 생성)
            var helperGO = GameObject.Find("ScreenshotHelper");
            if (helperGO == null)
            {
                helperGO = new GameObject("ScreenshotHelper");
                helperGO.hideFlags = HideFlags.HideAndDontSave;
            }

            // AIBeat.Utils.ScreenshotHelper 컴포넌트 추가/가져오기
            var helperType = System.Type.GetType("AIBeat.Utils.ScreenshotHelper, Assembly-CSharp");
            if (helperType == null)
            {
                Debug.LogError("[ScreenCapture] ScreenshotHelper 타입을 찾을 수 없습니다.");
                return;
            }

            var helper = helperGO.GetComponent(helperType);
            if (helper == null)
            {
                helper = helperGO.AddComponent(helperType);
            }

            // CaptureAfterFrame 메서드 호출
            var method = helperType.GetMethod("CaptureAfterFrame");
            if (method != null)
            {
                method.Invoke(helper, new object[] { filePath });
            }
            else
            {
                Debug.LogError("[ScreenCapture] CaptureAfterFrame 메서드를 찾을 수 없습니다.");
            }
        }

        private static EditorWindow GetGameViewWindow()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null)
            {
                return EditorWindow.GetWindow(gameViewType, false, "Game", false);
            }
            return null;
        }

        [MenuItem("Tools/A.I. BEAT/Focus Game View")]
        public static void FocusGameViewMenuItem()
        {
            var gameView = GetGameViewWindow();
            if (gameView != null)
            {
                gameView.Focus();
                gameView.Repaint();
                Debug.Log("[ScreenCapture] Game View 포커스됨");
            }
        }

        [MenuItem("Tools/A.I. BEAT/Open Screenshots Folder")]
        public static void OpenScreenshotsFolder()
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string folderPath = Path.Combine(projectPath, screenshotFolder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace("/", "\\"));
            Debug.Log($"[ScreenCapture] 폴더 열기: {folderPath}");
        }
    }
}
