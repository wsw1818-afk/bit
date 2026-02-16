#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using AIBeat.UI;
using AIBeat.Core;
using AIBeat.Gameplay;

namespace AIBeat.Editor
{
    public class SceneBuilder
    {
        private const string RESOURCE_PATH = "Assets/Resources/AIBeat_Design/UI";
        private static TMPro.TMP_FontAsset defaultFont = null;

        [MenuItem("AIBeat/Setup Build Settings")]
        public static void SetupBuildSettings()
        {
            // Unity 6+ Build Profiles 지원
            var scenePaths = new string[]
            {
                "Assets/Scenes/SplashScene.unity",
                "Assets/Scenes/MainMenuScene.unity",
                "Assets/Scenes/SongSelectScene.unity",
                "Assets/Scenes/GameplayScene.unity",
            };

            var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in scenePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    sceneList.Add(new EditorBuildSettingsScene(path, true));
                    Debug.Log($"[SceneBuilder] Added scene: {path}");
                }
                else
                {
                    Debug.LogWarning($"[SceneBuilder] Scene not found: {path}");
                }
            }

            EditorBuildSettings.scenes = sceneList.ToArray();

            // Active Build Profile에도 적용 시도 (Unity 6+)
            try
            {
                var buildProfileType = System.Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor");
                if (buildProfileType != null)
                {
                    var getActiveMethod = buildProfileType.GetMethod("GetActiveBuildProfile",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (getActiveMethod != null)
                    {
                        var activeProfile = getActiveMethod.Invoke(null, null);
                        if (activeProfile != null)
                        {
                            var scenesProperty = buildProfileType.GetProperty("scenes");
                            if (scenesProperty != null)
                            {
                                scenesProperty.SetValue(activeProfile, sceneList.ToArray());
                                Debug.Log("[SceneBuilder] Active Build Profile updated");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SceneBuilder] Build Profile update skipped: {e.Message}");
            }

            Debug.Log($"[SceneBuilder] Build Settings updated with {sceneList.Count} scenes");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("AIBeat/Build All Scenes")]
        public static void BuildAll()
        {
            // Ensure assets exist first
            if (!Directory.Exists(Application.dataPath + "/Resources/AIBeat_Design/UI"))
            {
                // Warning only, continue if possible or create defaults
                 Debug.LogWarning("UI Assets folder not found at expected path. Procedural generation might be used.");
            }

            // Load default font once at the start
            LoadDefaultFont();

            BuildSplashScene();
            BuildMainMenuScene();
            BuildSongSelectScene();
            BuildGameplayScene();
            BuildSongListItemPrefab(); 
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

            // Add SplashController (assumed to exist) for auto transition on click/touch
            var controllerObj = new GameObject("SplashController");
            // Check if SplashController exists
            if (System.Type.GetType("AIBeat.UI.SplashController, Assembly-CSharp") != null)
                controllerObj.AddComponent(System.Type.GetType("AIBeat.UI.SplashController, Assembly-CSharp"));
            
            // Simple fallback if script missing: SceneLoader button
            if (controllerObj.GetComponent<MonoBehaviour>() == null)
            {
                var btn = canvas.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => {
                     GameObject.FindObjectOfType<SceneLoader>()?.LoadMainMenu();
                });
            }

            SaveScene("Assets/Scenes/SplashScene.unity");
        }

        private static void BuildMainMenuScene()
        {
            NewScene("Assets/Scenes/MainMenuScene.unity");
            
            var canvas = CreateCanvas();
            
            // Attach MainMenuUI - it handles background, buttons (if missing), and animations
            var uiScript = canvas.gameObject.AddComponent<MainMenuUI>();
            
            // Create MusicianBackground panel for MainMenuUI to find
            var musicianBg = new GameObject("MusicianBackground");
            musicianBg.transform.SetParent(canvas.transform, false);
            musicianBg.transform.SetAsFirstSibling(); // Behind other UI
            
            var mbRect = musicianBg.AddComponent<RectTransform>();
            mbRect.anchorMin = Vector2.zero;
            mbRect.anchorMax = Vector2.one;
            mbRect.offsetMin = Vector2.zero;
            mbRect.offsetMax = Vector2.zero;
            
            // Musician Placeholders (Scripts will load sprites)
            CreateMusicianPlaceholder(musicianBg, "Drummer", new Vector2(-250, -100));
            CreateMusicianPlaceholder(musicianBg, "Pianist", new Vector2(250, -100));
            CreateMusicianPlaceholder(musicianBg, "Guitarist", new Vector2(0, 50));
            CreateMusicianPlaceholder(musicianBg, "DJ", new Vector2(0, 200));

            // MainMenuUI.AutoSetupReferences will create buttons if they don't exist.
            // But let's create the layout container so it doesn't just pile them up.
            // Actually MainMenuUI's code (EnsureButtonMobileSize) creates a container and parents them.
            // So we just need to ensure the SceneLoader/GameManager is present.

            var loader = new GameObject("SceneLoader");
            loader.AddComponent<SceneLoader>();
            
            // GameManager is usually a singleton from Boot, but for testing MainMenu independently:
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();

            SaveScene("Assets/Scenes/MainMenuScene.unity");
        }

        private static void BuildSongSelectScene()
        {
             NewScene("Assets/Scenes/SongSelectScene.unity");

            var canvas = CreateCanvas();
            
            // Attach SongSelectUI
            var uiScript = canvas.gameObject.AddComponent<SongSelectUI>();
            
            // Create BackButton (SongSelectUI expects it)
            var backBtn = CreateMenuButton(canvas.gameObject, "BackButton", "<");
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 1);
            backRt.anchorMax = new Vector2(0, 1);
            backRt.pivot = new Vector2(0, 1);
            backRt.anchoredPosition = new Vector2(20, -20);
            backRt.sizeDelta = new Vector2(100, 100);

            // Create SongLibraryManager if needed (Singleton logic in script handles it, but good to have empty GO)
            
            var loader = new GameObject("SceneLoader");
            loader.AddComponent<SceneLoader>();

            SaveScene("Assets/Scenes/SongSelectScene.unity");
        }

        private static void BuildGameplayScene()
        {
            NewScene("Assets/Scenes/GameplayScene.unity");
            
            var canvas = CreateCanvas();
            
            // GameplayUI
            var uiScript = canvas.gameObject.AddComponent<GameplayUI>();
            
            // Controllers
            var controllerGo = new GameObject("GameplayController");
            // Check types to avoid compilation error if scripts missing
            controllerGo.AddComponent<GameplayController>();
            
            var judgeGo = new GameObject("JudgementSystem");
            judgeGo.AddComponent<JudgementSystem>();
            
            var noteArea = new GameObject("NoteArea");
            // noteArea.transform.SetParent(canvas.transform, false); // Or World Space? Usually NoteArea is separate.
            // Assuming NoteArea is World Space or managed by Controller.
            
            // HUD Structuring
            // GameplayUI expects ScorePanel, ComboText etc.
            // AutoSetupReferences tries to find them. Let's create basic hierarchy.
            
            var scorePanel = new GameObject("ScorePanel");
            scorePanel.transform.SetParent(canvas.transform, false);
            var spRect = scorePanel.AddComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 1); 
            spRect.anchorMax = new Vector2(0.5f, 1);
            spRect.anchoredPosition = new Vector2(0, -100);
            var scoreObj = CreateText(scorePanel, "ScoreText", "000000", 60, Vector2.zero, new Vector2(400, 80));
            scoreObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            var comboObj = CreateText(canvas.gameObject, "ComboText", "0", 80, new Vector2(0, 100), new Vector2(400, 100)); // Center screen
            comboObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var judgeObj = CreateText(canvas.gameObject, "JudgementText", "", 60, new Vector2(0, -200), new Vector2(600, 100));
            judgeObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            SaveScene("Assets/Scenes/GameplayScene.unity");
        }
        
        // Helpers
        private static void LoadDefaultFont()
        {
            defaultFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static void NewScene(string path)
        {
             var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
             string dir = Path.GetDirectoryName(path);
             if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

             var camObj = new GameObject("Main Camera");
             camObj.transform.position = new Vector3(0, 0, -10); // 노트(Z=0)를 볼 수 있도록
             var cam = camObj.AddComponent<Camera>();
             cam.clearFlags = CameraClearFlags.SolidColor;
             cam.backgroundColor = Color.black;
             cam.orthographic = true;
             cam.orthographicSize = 15f; // 노트가 lookAhead=3초 전에 스폰, Y=judgeY+15까지 보여야 함
             camObj.tag = "MainCamera";
             // Add AudioListener
             camObj.AddComponent<AudioListener>();
        }

        private static void SaveScene(string path)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            Debug.Log($"[SceneBuilder] Created {path}");
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f; 
            go.AddComponent<GraphicRaycaster>();
            
            // Add SafeAreaApplier
            if (System.Type.GetType("AIBeat.UI.SafeAreaApplier, Assembly-CSharp") != null)
               go.AddComponent(System.Type.GetType("AIBeat.UI.SafeAreaApplier, Assembly-CSharp"));

            return canvas;
        }

        private static GameObject CreateBackground(Canvas canvas, string resourceName)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(canvas.transform, false);
            var img = go.AddComponent<Image>();
            
            Sprite sp = Resources.Load<Sprite>("AIBeat_Design/UI/" + resourceName);
            if (sp != null) img.sprite = sp;
            
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

            if (defaultFont != null) txt.font = defaultFont;
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
            
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 120);

            // Text
            var txtObj = CreateText(go, "Text", text, 40, Vector2.zero, new Vector2(600, 120));
            var tmpro = txtObj.GetComponent<TextMeshProUGUI>();
            tmpro.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static void CreateMusicianPlaceholder(GameObject parent, string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = Color.clear; // Invisible until sprite loaded
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 400); // Default size
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
