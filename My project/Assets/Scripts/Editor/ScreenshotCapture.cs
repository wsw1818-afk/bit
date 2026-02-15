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
                Debug.LogWarning("[Screenshot] Play 모드에서만 스크린샷을 캡처할 수 있습니다.");
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
            string filePath = Path.Combine(folderPath, fileName);

            // 스크린샷 캡처
            ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log($"[Screenshot] 캡처 완료: {filePath}");
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

            // Windows 탐색기로 폴더 열기
            System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace("/", "\\"));
            Debug.Log($"[Screenshot] 폴더 열기: {folderPath}");
        }
    }
}
