using UnityEngine;
using System.IO;
using AIBeat.UI;
using AIBeat.Data; // For NoteType

namespace AIBeat.Core
{
    public static class AssetGenTrigger
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("AIBeat/Generate Design Assets")]
        public static void GenerateAssetsMenu()
        {
            GenerateAndSaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoGenerateOnPlay()
        {
            // Optional: Check if assets exist before generating to save time
            // For now, generate to ensure they are fresh/exist
            if (Application.isEditor)
            {
                GenerateAndSaveAssets();
            }
        }

        public static void GenerateAndSaveAssets()
        {
            string basePath = Path.Combine(Application.dataPath, "Resources", "AIBeat_Design");
            string notePath = Path.Combine(basePath, "Notes");
            string judgePath = Path.Combine(basePath, "Judgements");

            // Ensure directories
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            if (!Directory.Exists(notePath)) Directory.CreateDirectory(notePath);
            if (!Directory.Exists(judgePath)) Directory.CreateDirectory(judgePath);

            // 1. Notes
            GenerateNote(NoteType.Tap, Path.Combine(notePath, "NormalNote.png"));
            GenerateNote(NoteType.Long, Path.Combine(notePath, "LongNote.png")); // Head
            GenerateNote(NoteType.Scratch, Path.Combine(notePath, "ScratchNote.png"));
            
            // Long Note Body
            var bodyTex = ProceduralImageGenerator.CreateLongNoteBodyTexture();
            ProceduralImageGenerator.SaveTextureAsPNG(bodyTex, Path.Combine(notePath, "LongNoteBody.png"));

            // 2. Judgements
            GenerateJudgement("Perfect", Path.Combine(judgePath, "Perfect_Sheet.png"));
            GenerateJudgement("Great", Path.Combine(judgePath, "Great_Sheet.png"));
            GenerateJudgement("Good", Path.Combine(judgePath, "Good_Sheet.png"));
            GenerateJudgement("Bad", Path.Combine(judgePath, "Bad_Sheet.png"));

            // 3. Background
            var bgTex = ProceduralImageGenerator.CreateCyberpunkBackground();
            // CreateCyberpunkBackground returns a Sprite, we need the Texture
            if (bgTex != null && bgTex.texture != null)
            {
                 ProceduralImageGenerator.SaveTextureAsPNG(bgTex.texture, Path.Combine(basePath, "GameBackground.png"));
            }

            // 4. UI Assets (New)
            string uiPath = Path.Combine(basePath, "UI");
            string uiBgPath = Path.Combine(uiPath, "Backgrounds");
            string uiBtnPath = Path.Combine(uiPath, "Buttons");
            string uiLogoPath = Path.Combine(uiPath, "Logo");
            
            if (!Directory.Exists(uiBgPath)) Directory.CreateDirectory(uiBgPath);
            if (!Directory.Exists(uiBtnPath)) Directory.CreateDirectory(uiBtnPath);
            if (!Directory.Exists(uiLogoPath)) Directory.CreateDirectory(uiLogoPath);

            // Backgrounds
            // Splash: Deep Purple -> Black
            var splashBG = ProceduralImageGenerator.CreateGradientBackground(512, 1024, new Color(0.2f, 0f, 0.4f), Color.black, new Color(1f, 0f, 1f));
            ProceduralImageGenerator.SaveTextureAsPNG(splashBG, Path.Combine(uiBgPath, "Splash_BG.png"));

            // Main Menu: Blue/Purple
            var menuBG = ProceduralImageGenerator.CreateGradientBackground(512, 1024, new Color(0.1f, 0.1f, 0.3f), new Color(0.05f, 0.05f, 0.1f), new Color(0f, 1f, 1f));
            ProceduralImageGenerator.SaveTextureAsPNG(menuBG, Path.Combine(uiBgPath, "Menu_BG.png"));

            // Song Select: Darker
            var songBG = ProceduralImageGenerator.CreateGradientBackground(512, 1024, new Color(0.05f, 0.05f, 0.1f), Color.black, new Color(0.5f, 0.5f, 0.5f));
            ProceduralImageGenerator.SaveTextureAsPNG(songBG, Path.Combine(uiBgPath, "SongSelect_BG.png"));

            // Buttons
            var btnNormal = ProceduralImageGenerator.CreateRoundedButton(256, 64, new Color(0.2f, 0.2f, 0.3f), new Color(0f, 1f, 1f), false);
            ProceduralImageGenerator.SaveTextureAsPNG(btnNormal, Path.Combine(uiBtnPath, "Btn_Normal.png"));
            
            var btnHover = ProceduralImageGenerator.CreateRoundedButton(256, 64, new Color(0.3f, 0.3f, 0.5f), new Color(0.5f, 1f, 1f), true);
            ProceduralImageGenerator.SaveTextureAsPNG(btnHover, Path.Combine(uiBtnPath, "Btn_Hover.png"));
            
            var btnPressed = ProceduralImageGenerator.CreateRoundedButton(256, 64, new Color(0.1f, 0.1f, 0.2f), new Color(0f, 0.8f, 0.8f), false);
            ProceduralImageGenerator.SaveTextureAsPNG(btnPressed, Path.Combine(uiBtnPath, "Btn_Pressed.png"));

            // Logo
            var logo = ProceduralImageGenerator.CreateSimpleLogo();
            ProceduralImageGenerator.SaveTextureAsPNG(logo, Path.Combine(uiLogoPath, "MainLogo.png"));

            // Create README

            File.WriteAllText(Path.Combine(basePath, "README.txt"), "Generated by AIBeat Procedural Generator\nContains: Notes, Judgement Effects, Background.");
            
            Debug.Log($"[AssetGen] Generation Complete. Assets saved to {basePath}");

            // Zip it ? (Requires external lib or system command usually, we can skip or do it via shell)
        }

        private static void GenerateNote(NoteType type, string path)
        {
             var tex = ProceduralImageGenerator.CreateNoteTexture(type);
             ProceduralImageGenerator.SaveTextureAsPNG(tex, path);
        }

        private static void GenerateJudgement(string type, string path)
        {
            var tex = ProceduralImageGenerator.CreateJudgementSheet(type);
            ProceduralImageGenerator.SaveTextureAsPNG(tex, path);
        }
    }
}
