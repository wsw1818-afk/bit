#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using AIBeat.UI;

namespace AIBeat.Editor
{
    public class SceneBuilder
    {
        private const string RESOURCE_PATH = "Assets/Resources/AIBeat_Design/UI";
        private static TMPro.TMP_FontAsset defaultFont = null;

        [MenuItem("AIBeat/Build All Scenes")]
        public static void BuildAll()
        {
            // Ensure assets exist first
            if (!Directory.Exists(Application.dataPath + "/Resources/AIBeat_Design/UI"))
            {
                EditorUtility.DisplayDialog("Error", "UI Assets not found. Please run 'AIBeat/Generate Design Assets' first.", "OK");
                return;
            }

            // Load default font once at the start
            LoadDefaultFont();

            BuildSplashScene();
            BuildMainMenuScene();
            BuildSongSelectScene();
            BuildSongListItemPrefab(); // Build Prefab first or last? Prefab used in SongSelect... so maybe before?
            // Actually SongSelect just needs ScrollView, prefab usually instantiated at runtime.
            // But let's build prefab for user usage.
        }

        [MenuItem("AIBeat/Build SongListItem Prefab")]
        public static void BuildSongListItemPrefab()
        {
            // Ensure font is loaded
            if (defaultFont == null) LoadDefaultFont();

            GameObject go = new GameObject("SongListItem");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 150);
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.8f); // Dark background
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Title
            var titleObj = CreateText(go, "Title", "Song Title", 40, new Vector2(20, 30), new Vector2(600, 50));
            // Artist
            var artistObj = CreateText(go, "Artist", "Artist Name", 24, new Vector2(20, -20), new Vector2(600, 30));
            // Difficulty
            var diffObj = CreateText(go, "Difficulty", "HARD 10", 36, new Vector2(650, 0), new Vector2(150, 50));
            diffObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

            var script = go.AddComponent<SongListItem>();
            
            // Link references via SerializedObject since fields are private [SerializeField]
            // Or just make them public for Builder? No, stick to clean code.
            // We can use Reflection or SerializedObject.
            // Simplified: Component references
            // Let's rely on GetComponent in script or public fields for Editor construction?
            // For now, let's assume user might link manually or use Find?
            // Wait, I can use SerializedObject.
            
            // Save Prefab
            string prefabPath = "Assets/Prefabs/UI";
            if (!Directory.Exists(prefabPath)) Directory.CreateDirectory(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath + "/SongListItem.prefab");
            GameObject.DestroyImmediate(go);
            
            Debug.Log("[SceneBuilder] SongListItem.prefab created.");
        }

        private static void BuildSplashScene()
        {
            NewScene("Assets/Scenes/SplashScene.unity");
            
            var canvas = CreateCanvas();
            var bg = CreateBackground(canvas, "Backgrounds/Splash_BG");
            
            var logo = CreateLogo(canvas, 0, 100, 1.2f);
            
            var textObj = CreateText(canvas.gameObject, "TouchText", "Touch to Start", 48, new Vector2(0, -400), new Vector2(600, 100));
            var tmpro = textObj.GetComponent<TextMeshProUGUI>();
            tmpro.alignment = TextAlignmentOptions.Center;
            
            // Add SceneLoader
            var loaderObj = new GameObject("SceneLoader");
            loaderObj.AddComponent<SceneLoader>();

            // Add SplashController for auto transition on click/touch
            var controllerObj = new GameObject("SplashController");
            controllerObj.AddComponent<SplashController>();

            SaveScene("Assets/Scenes/SplashScene.unity");
        }

        private static void BuildMainMenuScene()
        {
            // Update existing scene if possible, or create new
            string path = "Assets/Scenes/MainMenuScene.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            
            // Clear old UI if we want fresh start, or try to find existing
            // Strategy: Clear Canvas/Camera and rebuild.
            var rootObjs = scene.GetRootGameObjects();
            foreach(var r in rootObjs) 
            {
                if (r.name == "Canvas" || r.name == "Main Camera" || r.name == "SceneLoader")
                    GameObject.DestroyImmediate(r);
            }

            // Add Main Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camObj.tag = "MainCamera";

            var canvas = CreateCanvas();
            CreateBackground(canvas, "Backgrounds/Menu_BG");
            CreateLogo(canvas, 0, 500, 1.0f);

            // Container for buttons
            var panel = new GameObject("ButtonPanel");
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -300);
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;

            // Buttons
            var startBtn = CreateMenuButton(panel, "Start", "START");
            startBtn.GetComponent<Button>().onClick.AddListener(() => {
                 GameObject.FindObjectOfType<SceneLoader>()?.LoadSongSelect();
            });

            CreateMenuButton(panel, "Settings", "SETTINGS");

            var quitBtn = CreateMenuButton(panel, "Quit", "QUIT");
            quitBtn.GetComponent<Button>().onClick.AddListener(() => {
                 GameObject.FindObjectOfType<SceneLoader>()?.QuitGame();
            });

            new GameObject("SceneLoader").AddComponent<SceneLoader>();

            SaveScene(path);
        }

        private static void BuildSongSelectScene()
        {
            string path = "Assets/Scenes/SongSelectScene.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
             
            // Clear old UI
            var rootObjs = scene.GetRootGameObjects();
            foreach(var r in rootObjs) 
            {
                if (r.name == "Canvas" || r.name == "Main Camera" || r.name == "SceneLoader")
                    GameObject.DestroyImmediate(r);
            }

            // Add Main Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camObj.tag = "MainCamera";

            var canvas = CreateCanvas();
            CreateBackground(canvas, "Backgrounds/SongSelect_BG");

            // Play Button (최상단)
            var playBtn = CreateMenuButton(canvas.gameObject, "PlayBtn", "PLAY");
            var playRt = playBtn.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.5f, 1);
            playRt.anchorMax = new Vector2(0.5f, 1);
            playRt.anchoredPosition = new Vector2(0, -150);
            playBtn.GetComponent<Button>().onClick.AddListener(() => {
                 GameObject.FindObjectOfType<SceneLoader>()?.LoadGame();
            });

            // TODO: 곡 목록 ScrollView (나중에 추가)
            // Header as placeholder
            var header = CreateText(canvas.gameObject, "Header", "SELECT MUSIC", 60, new Vector2(0, 0), new Vector2(800, 100));
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1);
            headerRt.anchorMax = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = new Vector2(0, -450);

            // Settings Button (하단 위)
            var settingsBtn = CreateMenuButton(canvas.gameObject, "SettingsBtn", "SETTINGS");
            var settingsRt = settingsBtn.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(0.5f, 0);
            settingsRt.anchorMax = new Vector2(0.5f, 0);
            settingsRt.anchoredPosition = new Vector2(0, 220);

            // Back Button (맨 아래)
            var backBtn = CreateMenuButton(canvas.gameObject, "BackBtn", "BACK");
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0);
            backRt.anchorMax = new Vector2(0.5f, 0);
            backRt.anchoredPosition = new Vector2(0, 90);
            backBtn.GetComponent<Button>().onClick.AddListener(() => {
                 GameObject.FindObjectOfType<SceneLoader>()?.LoadMainMenu();
            });
            
            new GameObject("SceneLoader").AddComponent<SceneLoader>();

            SaveScene(path);
        }


        // Helpers
        private static void LoadDefaultFont()
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (defaultFont == null)
            {
                Debug.LogError("[SceneBuilder] Failed to load default TMP font. Make sure TMP Essential Resources are imported.");
            }
            else
            {
                Debug.Log($"[SceneBuilder] Loaded default font: {defaultFont.name}");
            }
        }

        private static void NewScene(string path)
        {
             var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
             // Ensure path directory
             string dir = Path.GetDirectoryName(path);
             if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

             // Add Main Camera for UI rendering
             var camObj = new GameObject("Main Camera");
             var cam = camObj.AddComponent<Camera>();
             cam.clearFlags = CameraClearFlags.SolidColor;
             cam.backgroundColor = Color.black;
             cam.orthographic = true;
             camObj.tag = "MainCamera";
        }

        private static void SaveScene(string path)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            Debug.Log($"[SceneBuilder] Created {path}");
        }

        private static Canvas CreateCanvas()
        {
             // Check if Canvas exists (if we didn't clear)
             // But we cleared.
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f; // Match width/height equally
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateBackground(Canvas canvas, string resourceName)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(canvas.transform, false);
            var img = go.AddComponent<Image>();
            
            Sprite sp = Resources.Load<Sprite>("AIBeat_Design/UI/" + resourceName);
            if (sp == null) 
            {
                 // Try load texture and create sprite if sprite missing (generated as PNG)
                 Texture2D tex = Resources.Load<Texture2D>("AIBeat_Design/UI/" + resourceName);
                 if (tex != null)
                 {
                     sp = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
                 }
            }
            img.sprite = sp;
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateLogo(Canvas canvas, float x, float y, float scale)
        {
            var go = new GameObject("Logo");
            go.transform.SetParent(canvas.transform, false);
            var img = go.AddComponent<Image>();
            Texture2D tex = Resources.Load<Texture2D>("AIBeat_Design/UI/Logo/MainLogo");
            if (tex != null)
                img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(512 * scale, 128 * scale);
            return go;
        }

        private static GameObject CreateText(GameObject parent, string name, string content, int fontSize, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var txt = go.AddComponent<TextMeshProUGUI>();

            // Assign font BEFORE setting text to avoid warning
            if (defaultFont != null)
            {
                txt.font = defaultFont;
                Debug.Log($"[SceneBuilder] Assigned font to {name}");
            }
            else
            {
                Debug.LogWarning($"[SceneBuilder] defaultFont is null when creating {name}");
            }

            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Left;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static GameObject CreateMenuButton(GameObject parent, string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            
            // Load sprites
            img.sprite = LoadSprite("Buttons/Btn_Normal");
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = LoadSprite("Buttons/Btn_Hover");
            ss.pressedSprite = LoadSprite("Buttons/Btn_Pressed");
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = ss;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 120);

            // Text
            var txtObj = CreateText(go, "Text", text, 40, Vector2.zero, new Vector2(600, 120));
            var tmpro = txtObj.GetComponent<TextMeshProUGUI>();
            tmpro.alignment = TextAlignmentOptions.Center;
            tmpro.color = new Color(0, 1, 1); // Cyan text

            return go;
        }

        private static Sprite LoadSprite(string path)
        {
             Texture2D tex = Resources.Load<Texture2D>("AIBeat_Design/UI/" + path);
             if (tex == null) return null;
             return Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
        }
    }
}
#endif
