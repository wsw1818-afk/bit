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

            int w = Screen.width;
            int h = Screen.height;

            // 에디터에서 Game View가 너무 작을 경우 업스케일 (최소 540px 너비 보장)
#if UNITY_EDITOR
            if (w < 540)
            {
                int scale = Mathf.CeilToInt(540f / w);
                w *= scale;
                h *= scale;
            }
#endif

            // 화면 전체 캡처 후 필요 시 업스케일
            Texture2D src = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            src.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            src.Apply();

            Texture2D tex = src;
            if (w != Screen.width)
            {
                // RenderTexture로 업스케일
                var rt = new RenderTexture(w, h, 0);
                Graphics.Blit(src, rt);
                tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                rt.Release();
                Destroy(src);
            }

            // PNG 저장
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"[ScreenCapture] Screenshot saved: {filePath} ({w}x{h})");

            Destroy(tex);
        }
    }
}
