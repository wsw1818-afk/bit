using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;

namespace AIBeat.Editor
{
    public static class ScreenshotCapture
    {
        private static string screenshotFolder = "Screenshots";

        [MenuItem("Tools/A.I. BEAT/Capture Screenshot _F12")]
        public static void CaptureScreenshot()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ScreenCapture] Play 모드에서만 스크린샷을 캡처할 수 있습니다.");
                return;
            }

            // Game View를 포커스하고 Repaint 강제
            FocusAndRepaintGameView();

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
            string filePath = Path.Combine(folderPath, fileName);

            // Camera 기반 RenderTexture 캡처 (더 안정적)
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // 모든 카메라 중 첫 번째 활성 카메라 찾기
                var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var cam in cameras)
                {
                    if (cam.gameObject.activeInHierarchy)
                    {
                        mainCamera = cam;
                        break;
                    }
                }
            }

            if (mainCamera == null)
            {
                Debug.LogError("[ScreenCapture] 활성 카메라를 찾을 수 없습니다.");
                return;
            }

            // RenderTexture 생성 (1080x1920 세로 해상도)
            int width = 1080;
            int height = 1920;
            RenderTexture rt = new RenderTexture(width, height, 24);
            rt.antiAliasing = 2;

            // 카메라 렌더링
            RenderTexture prevRT = mainCamera.targetTexture;
            mainCamera.targetTexture = rt;
            mainCamera.Render();
            mainCamera.targetTexture = prevRT;

            // RenderTexture → Texture2D 변환
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // PNG 저장
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            // 정리
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"[ScreenCapture] Screenshot saved: {filePath} ({width}x{height})");
        }

        private static void FocusAndRepaintGameView()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null)
            {
                var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
                if (gameView != null)
                {
                    gameView.Focus();
                    gameView.Repaint();
                    // 강제 리페인트를 위해 SendEvent 호출
                    gameView.SendEvent(EditorGUIUtility.CommandEvent("Repaint"));
                }
            }
        }

        [MenuItem("Tools/A.I. BEAT/Focus Game View")]
        public static void FocusGameViewMenuItem()
        {
            FocusAndRepaintGameView();
            Debug.Log("[ScreenCapture] Game View 포커스됨");
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
