using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// Procedural Asset Generator (NanoBanana Fallback)
    /// Creates Cyberpunk/Rhythm game assets at runtime using code.
    /// </summary>
    public static class ProceduralImageGenerator
    {
        // Cache to avoid regenerating
        private static Sprite cachedBackground;
        private static Sprite cachedButtonFrame;
        private static Sprite cachedLaneBackground;

        /// <summary>
        /// Generates a dark cyberpunk grid background
        /// </summary>
        public static Sprite CreateCyberpunkBackground()
        {
            if (cachedBackground != null) return cachedBackground;

            int width = 512;
            int height = 512;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            Color topColor = new Color(0.08f, 0.02f, 0.15f, 1f);     // 밝은 보라
            Color bottomColor = new Color(0.15f, 0.05f, 0.25f, 1f);  // 더 밝은 보라
            Color gridColor = new Color(1f, 0.0f, 0.8f, 0.04f);      // 네온 마젠타 (훨씬 더 투명)

            for (int y = 0; y < height; y++)
            {
                float vy = (float)y / height;
                Color bgColor = Color.Lerp(bottomColor, topColor, vy);

                for (int x = 0; x < width; x++)
                {
                    // Basic Gradient
                    Color finalColor = bgColor;

                    // Grid Lines (Perspective effect simplified)
                    // Vertical lines
                    if (x % 64 == 0)
                    {
                        finalColor += gridColor;
                    }
                    // Horizontal lines (get closer together at top)
                    // Simple linear for now
                    if (y % 64 == 0)
                    {
                        finalColor += gridColor;
                    }

                    // Add some noise
                    float noise = Random.Range(-0.02f, 0.02f);
                    finalColor.r += noise;
                    finalColor.g += noise;
                    finalColor.b += noise;

                    texture.SetPixel(x, y, finalColor);
                }
            }
            
            texture.Apply();
            cachedBackground = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            cachedBackground.name = "Procedural_Cyberpunk_BG";
            return cachedBackground;
        }

        /// <summary>
        /// Generates a neon button frame
        /// </summary>
        public static Sprite CreateButtonFrame()
        {
            if (cachedButtonFrame != null) return cachedButtonFrame;

            int width = 256;
            int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            Color borderColor = new Color(0f, 1f, 1f, 1f); // Cyan
            Color fillColor = new Color(0f, 0.2f, 0.3f, 0.8f);

            int borderSize = 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < borderSize || x >= width - borderSize || y < borderSize || y >= height - borderSize)
                    {
                        texture.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                }
            }

            texture.Apply();
            cachedButtonFrame = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(borderSize, borderSize, borderSize, borderSize));
            cachedButtonFrame.name = "Procedural_Button_Frame";
            return cachedButtonFrame;
        }

        /// <summary>
        /// Generates a vertical track lane background
        /// </summary>
        public static Sprite CreateLaneBackground()
        {
            if (cachedLaneBackground != null) return cachedLaneBackground;

            int width = 256;
            int height = 512;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            Color laneColor = new Color(0f, 0f, 0f, 0.7f);
            Color dividerColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = laneColor;
                    // Divider in middle
                    if (Mathf.Abs(x - width / 2) < 2)
                    {
                        c = dividerColor;
                    }
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply();
            cachedLaneBackground = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            cachedLaneBackground.name = "Procedural_Lane_BG";
            return cachedLaneBackground;
        }
    }
}
