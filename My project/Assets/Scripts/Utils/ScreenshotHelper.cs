using UnityEngine;
using System.Collections;
using System.IO;

namespace AIBeat.Utils
{
    /// <summary>
    /// 런타임 MonoBehaviour - EndOfFrame 코루틴으로 Screen Space Overlay UI 캡처
    /// Editor 스크립트에서 호출하여 사용
    /// </summary>
    public class ScreenshotHelper : MonoBehaviour
    {
        private static ScreenshotHelper _instance;
        public static ScreenshotHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ScreenshotHelper");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _instance = go.AddComponent<ScreenshotHelper>();
                }
                return _instance;
            }
        }

        public void CaptureAfterFrame(string filePath)
        {
            StartCoroutine(CaptureCoroutine(filePath));
        }

        private IEnumerator CaptureCoroutine(string filePath)
        {
            // 렌더링 완료 대기 (Screen Space Overlay 포함)
            yield return new WaitForEndOfFrame();

            // 화면 전체 캡처 (Overlay UI 포함)
            Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();

            // PNG 저장
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"[ScreenCapture] Screenshot saved: {filePath} ({Screen.width}x{Screen.height})");

            Destroy(tex);
        }
    }
}
