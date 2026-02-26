using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace AIBeat.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 게임 화면을 캡처하는 유틸리티 (EditorWindow)
    /// </summary>
    public class ScreenCaptureEditor : EditorWindow
    {
        private enum CaptureSize
        {
            Original = 1,
            Double = 2,
            Triple = 3,
            Quad = 4
        }

        private string saveFolderPath = "Screenshots";
        private string filePrefix = "AIBeat";
        private CaptureSize superSize = CaptureSize.Original;
        private bool openAfterCapture = true;
        private bool includeTimestamp = true;
        private bool captureGameView = true;

        private Texture2D lastCapture;
        private string lastCapturePath;
        private Vector2 scrollPos;

        [MenuItem("Window/A.I. BEAT/Screen Capture")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScreenCaptureEditor>("Screen Capture");
            window.minSize = new Vector2(300, 350);
        }

        [MenuItem("Tools/A.I. BEAT/Force Capture Screenshot")]
        public static void ForceCaptureWithStep()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ScreenCapture] Play 모드에서만 사용 가능합니다");
                return;
            }

            // runInBackground 강제 설정
            Application.runInBackground = true;

            // 먼저 몇 프레임 진행시켜 렌더링 파이프라인 안정화
            for (int i = 0; i < 5; i++)
                EditorApplication.Step();

            string dir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(dir, fileName);

            // ScreenshotHelper를 통한 캡처 시도 (WaitForEndOfFrame - Overlay UI 포함)
            var helper = Utils.ScreenshotHelper.Instance;
            if (helper != null)
            {
                helper.CaptureAfterFrame(fullPath);
                // 코루틴 실행을 위해 추가 프레임 진행
                for (int i = 0; i < 3; i++)
                    EditorApplication.Step();
            }
            else
            {
                // Fallback: ScreenCapture API (superSize 4 = 4배 해상도)
                UnityEngine.ScreenCapture.CaptureScreenshot(fullPath, 4);
                for (int i = 0; i < 5; i++)
                    EditorApplication.Step();
            }

            Debug.Log($"[ScreenCapture] Force capture: {fullPath}");
        }

        [MenuItem("Tools/A.I. BEAT/Capture Screenshot _F12")]
        public static void QuickCapture()
        {
            string dir = Path.Combine(Application.dataPath, "..", "Screenshots");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = $"AIBeat_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(dir, fileName);

            // Play 모드에서는 ScreenshotHelper를 사용 (Overlay UI 포함)
            if (EditorApplication.isPlaying)
            {
                var helper = Utils.ScreenshotHelper.Instance;
                if (helper != null)
                {
                    helper.CaptureAfterFrame(fullPath);
                    Debug.Log($"[ScreenCapture] 스크린샷 캡처 예약됨...");
                    return;
                }
            }

            // Edit 모드에서는 Camera.Render 방식으로 즉시 저장
            Camera cam = Camera.main;
            if (cam != null)
            {
                int width = cam.pixelWidth;
                int height = cam.pixelHeight;

                var rt = new RenderTexture(width, height, 24);
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

                RenderTexture prevRT = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = prevRT;
                RenderTexture.active = null;

                byte[] pngData = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, pngData);

                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);

                Debug.Log($"[ScreenCapture] Screenshot saved: {fullPath}");
            }
            else
            {
                // Fallback to ScreenCapture API
                UnityEngine.ScreenCapture.CaptureScreenshot(fullPath, 1);
                Debug.Log($"[ScreenCapture] Screenshot saved (async): {fullPath}");
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.LabelField("Screen Capture Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Save settings
            EditorGUILayout.BeginHorizontal();
            saveFolderPath = EditorGUILayout.TextField("Save Folder", saveFolderPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Screenshot Folder",
                    Path.Combine(Application.dataPath, ".."), "Screenshots");
                if (!string.IsNullOrEmpty(selected))
                    saveFolderPath = selected;
            }
            EditorGUILayout.EndHorizontal();

            filePrefix = EditorGUILayout.TextField("File Prefix", filePrefix);
            superSize = (CaptureSize)EditorGUILayout.EnumPopup("Resolution Scale", superSize);
            includeTimestamp = EditorGUILayout.Toggle("Include Timestamp", includeTimestamp);
            openAfterCapture = EditorGUILayout.Toggle("Open After Capture", openAfterCapture);
            captureGameView = EditorGUILayout.Toggle("Capture Game View", captureGameView);

            EditorGUILayout.Space(8);

            // Capture buttons
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Capture", GUILayout.Height(32)))
            {
                CaptureScreenshot();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Capture (Transparent)", GUILayout.Height(32)))
            {
                CaptureTransparent();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Open Screenshot Folder"))
            {
                string absPath = GetAbsoluteSavePath();
                if (Directory.Exists(absPath))
                    EditorUtility.RevealInFinder(absPath);
                else
                    Debug.LogWarning($"[ScreenCapture] Folder not found: {absPath}");
            }

            // Preview
            EditorGUILayout.Space(8);
            if (lastCapture != null)
            {
                EditorGUILayout.LabelField("Last Capture:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(lastCapturePath ?? "", EditorStyles.miniLabel);

                float previewWidth = position.width - 20;
                float aspect = (float)lastCapture.height / lastCapture.width;
                float previewHeight = Mathf.Min(previewWidth * aspect, 300f);

                Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                EditorGUI.DrawPreviewTexture(previewRect, lastCapture, null, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.EndScrollView();
        }

        private string GetAbsoluteSavePath()
        {
            if (Path.IsPathRooted(saveFolderPath))
                return saveFolderPath;
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", saveFolderPath));
        }

        private string GenerateFileName(string extension = "png")
        {
            string timestamp = includeTimestamp
                ? $"_{System.DateTime.Now:yyyyMMdd_HHmmss}"
                : "";
            return $"{filePrefix}{timestamp}.{extension}";
        }

        private void CaptureScreenshot()
        {
            string dir = GetAbsoluteSavePath();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = GenerateFileName();
            string fullPath = Path.Combine(dir, fileName);

            if (captureGameView)
            {
                // Game View capture via ScreenCapture API
                UnityEngine.ScreenCapture.CaptureScreenshot(fullPath, (int)superSize);
                Debug.Log($"[ScreenCapture] Game view captured: {fullPath}");
                // Delayed load for preview (file written next frame)
                EditorApplication.delayCall += () => LoadPreview(fullPath);
            }
            else
            {
                // Scene View capture via Camera.Render
                CaptureFromCamera(fullPath);
            }

            lastCapturePath = fullPath;
        }

        private void CaptureTransparent()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[ScreenCapture] No Main Camera found for transparent capture.");
                return;
            }

            string dir = GetAbsoluteSavePath();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = GenerateFileName();
            string fullPath = Path.Combine(dir, fileName);

            int scale = (int)superSize;
            int width = cam.pixelWidth * scale;
            int height = cam.pixelHeight * scale;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);

            CameraClearFlags prevFlags = cam.clearFlags;
            Color prevBg = cam.backgroundColor;
            RenderTexture prevRT = cam.targetTexture;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // Restore camera
            cam.clearFlags = prevFlags;
            cam.backgroundColor = prevBg;
            cam.targetTexture = prevRT;
            RenderTexture.active = null;

            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

            lastCapture = tex;
            lastCapturePath = fullPath;
            Object.DestroyImmediate(rt);

            Debug.Log($"[ScreenCapture] Transparent capture saved: {fullPath}");

            if (openAfterCapture)
                EditorUtility.RevealInFinder(fullPath);

            Repaint();
        }

        private void CaptureFromCamera(string fullPath)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[ScreenCapture] No Main Camera found.");
                return;
            }

            int scale = (int)superSize;
            int width = cam.pixelWidth * scale;
            int height = cam.pixelHeight * scale;

            var rt = new RenderTexture(width, height, 24);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            RenderTexture prevRT = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRT;
            RenderTexture.active = null;

            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

            lastCapture = tex;
            Object.DestroyImmediate(rt);

            Debug.Log($"[ScreenCapture] Camera capture saved: {fullPath}");

            if (openAfterCapture)
                EditorUtility.RevealInFinder(fullPath);

            Repaint();
        }

        private void LoadPreview(string path)
        {
            if (!File.Exists(path)) return;

            byte[] data = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            lastCapture = tex;
            Repaint();

            if (openAfterCapture)
                EditorUtility.RevealInFinder(path);
        }
    }
#endif
}
