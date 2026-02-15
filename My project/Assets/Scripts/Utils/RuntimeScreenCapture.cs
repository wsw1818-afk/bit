using UnityEngine;
using System.IO;
using System.Collections;

namespace AIBeat.Utils
{
    /// <summary>
    /// 런타임에서 스크린샷을 캡처하는 컴포넌트
    /// WaitForEndOfFrame을 사용하여 UI 포함 전체 화면 캡처
    /// </summary>
    public class RuntimeScreenCapture : MonoBehaviour
    {
        private static RuntimeScreenCapture instance;
        public static RuntimeScreenCapture Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("RuntimeScreenCapture");
                    instance = go.AddComponent<RuntimeScreenCapture>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private string screenshotFolder = "Screenshots";
        private bool captureRequested = false;

        private void Start()
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string folderPath = Path.Combine(projectPath, screenshotFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public void RequestCapture()
        {
            if (!captureRequested)
            {
                captureRequested = true;
                StartCoroutine(CaptureEndOfFrame());
            }
        }

        private IEnumerator CaptureEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string folderPath = Path.Combine(projectPath, screenshotFolder);
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"AIBeat_{timestamp}.png";
            string filePath = Path.Combine(folderPath, fileName);

            // 현재 화면 캡처
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Destroy(screenshot);

            Debug.Log($"[RuntimeCapture] Screenshot saved: {filePath} ({bytes.Length} bytes)");
            captureRequested = false;
        }
    }
}
