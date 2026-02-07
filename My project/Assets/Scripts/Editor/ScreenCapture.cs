using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AIBeat.Editor
{
#if UNITY_EDITOR
    public class ScreenCaptureEditor : UnityEditor.Editor
    {
        [MenuItem("Tools/A.I. BEAT/Capture Screenshot")]
        public static void CaptureScreenshot()
        {
            string path = "h:/Claude_work/bit/screenshot.png";
            UnityEngine.ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[ScreenCapture] Screenshot saved to: {path}");
        }
    }
#endif
}
