using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace AIBeat.Editor
{
    public class TextureGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Tools/A.I. BEAT/Generate Assets")]
        public static void GenerateAllAssets()
        {
            EnsureDirectoryExists();
            
            GenerateCyberpunkBackground();
            GeneratePanelBackground();
            GenerateLaneSeparator();
            GenerateGlowParticle();
            
            AssetDatabase.Refresh();
            Debug.Log("[TextureGenerator] All assets generated in Assets/Textures/Generated/");
        }

        private static void EnsureDirectoryExists()
        {
            string path = Application.dataPath + "/Textures/Generated";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void SaveTexture(Texture2D texture, string fileName)
        {
            byte[] bytes = texture.EncodeToPNG();
            string path = Application.dataPath + "/Textures/Generated/" + fileName;
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[TextureGenerator] Saved {fileName}");
        }

        private static void GenerateCyberpunkBackground()
        {
            int width = 1080;
            int height = 1920;
            Texture2D tex = new Texture2D(width, height);

            Color topColor = new Color(0.05f, 0.0f, 0.1f); // Deep Purple
            Color bottomColor = new Color(0.2f, 0.0f, 0.4f); // Brighter Purple
            Color gridColor = new Color(0.0f, 1.0f, 1.0f, 0.3f); // Cyan Grid

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                Color bgColor = Color.Lerp(bottomColor, topColor, v);

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    
                    // Perspective Grid Effect
                    bool isGridLine = false;
                    
                    // Vertical lines (fan out)
                    // Simple perspective approx: x deviation from center increases with y (closer)
                    // Actually for "retro" look, vertical lines are straight or converge to vanish point
                    // Let's do straight vertical lines for simplicity first, maybe fanned slightly
                    
                    // Horizontal lines (get closer as we go up/further)
                    // exponential spacing
                    
                    float gridX = (x - width/2f);
                    // Standard Grid
                    if (x % 150 == 0 || y % 150 == 0) 
                    {
                        // isGridLine = true; 
                    }

                    // Cyberpunk Grid
                    // Vertical
                    if (Mathf.Abs(x - width/2) % 180 < 2) isGridLine = true;
                    // Horizontal - condense near center (horizon)? Let's just do uniform for now
                    if (y % 180 < 2) isGridLine = true;

                    Color finalColor = isGridLine ? Color.Lerp(bgColor, gridColor, 0.5f) : bgColor;
                    
                    // Vignette
                    float dist = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                    finalColor *= (1.0f - dist * 0.5f);

                    tex.SetPixel(x, y, finalColor);
                }
            }

            tex.Apply();
            SaveTexture(tex, "Background.png");
        }

        private static void GeneratePanelBackground()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size);
            Color contentColor = new Color(0f, 0f, 0f, 0.7f);
            Color borderColor = new Color(0f, 1f, 1f, 1f); // Cyan

            int borderSize = 8;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple Box with Border
                    bool isBorder = (x < borderSize || x > size - borderSize || y < borderSize || y > size - borderSize);
                    
                    if (isBorder)
                        tex.SetPixel(x, y, borderColor);
                    else
                        tex.SetPixel(x, y, contentColor);
                }
            }
            tex.Apply();
            SaveTexture(tex, "PanelBackground.png");
        }

        private static void GenerateLaneSeparator()
        {
            int width = 4;
            int height = 256;
            Texture2D tex = new Texture2D(width, height);
            Color color = new Color(1f, 1f, 1f, 0.3f);

            for(int y=0; y<height; y++)
            {
                for(int x=0; x<width; x++)
                {
                    // Fade out at ends
                    float alpha = 0.3f * Mathf.Sin((float)y/height * Mathf.PI);
                    tex.SetPixel(x, y, new Color(1,1,1, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, "LaneSeparator.png");
        }

        private static void GenerateGlowParticle()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            
            Vector2 center = new Vector2(size/2f, size/2f);

            for (int y=0; y<size; y++)
            {
                for (int x=0; x<size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x,y), center);
                    float normDist = dist / (size/2f);
                    float alpha = Mathf.Clamp01(1f - normDist);
                    alpha = Mathf.Pow(alpha, 2); // Soft falloff

                    tex.SetPixel(x, y, new Color(1,1,1, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, "GlowParticle.png");
        }
#endif
    }
}
