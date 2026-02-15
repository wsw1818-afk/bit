using UnityEditor;
using UnityEngine;
using System.IO;

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

            // Game View를 포커스
            FocusGameView();

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

            // Unity 내장 스크린샷 캡처
            UnityEngine.ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log($"[ScreenCapture] Screenshot saved: {filePath}");
        }

        private static void FocusGameView()
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
                }
            }
        }

        [MenuItem("Tools/A.I. BEAT/Focus Game View")]
        public static void FocusGameViewMenuItem()
        {
            FocusGameView();
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
