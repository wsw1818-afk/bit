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
        
        // Resource Paths
        public const string RESOURCE_PATH = "Assets/Resources/AIBeat_Design";


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

        // ==================================================================================
        // NEW: Note Generation (NanoBanana Style)
        // ==================================================================================

        public static Texture2D CreateNoteTexture(NoteType type)
        {
            int width = 128;
            int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];
            
            // Clear to transparent
            for(int i=0; i<colors.Length; i++) colors[i] = Color.clear;

            Color baseColor = Color.white;
            Color glowColor = Color.white;

            switch (type)
            {
                case NoteType.Tap: // Gold Note
                    baseColor = new Color(1f, 0.84f, 0f); 
                    glowColor = new Color(1f, 1f, 0.5f);
                    GenerateTapNote(colors, width, height, baseColor, glowColor);
                    break;
                case NoteType.Long: // Purple Long Note Head
                    baseColor = new Color(0.6f, 0.2f, 1f);
                    glowColor = new Color(0.8f, 0.5f, 1f);
                    GenerateLongNoteHead(colors, width, height, baseColor, glowColor);
                    break;
                case NoteType.Scratch: // Orange Scratch Note
                    baseColor = new Color(1f, 0.5f, 0f);
                    glowColor = new Color(1f, 0.8f, 0.2f);
                    GenerateScratchNote(colors, width, height, baseColor, glowColor);
                    break;
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateLongNoteBodyTexture()
        {
            int width = 128;
            int height = 128; // Square for tiling
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            Color coreColor = new Color(0.6f, 0.2f, 1f, 0.8f);
            Color glowColor = new Color(0.8f, 0.6f, 1f, 0.4f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float v = (float)y / height;
                    
                    // Simple beam effect
                    float distFromCenter = Mathf.Abs(u - 0.5f) * 2f; // 0 at center, 1 at edge
                    
                    Color c = Color.clear;
                    if (distFromCenter < 0.8f)
                    {
                        float alpha = Mathf.SmoothStep(0.8f, 0f, distFromCenter);
                        c = Color.Lerp(coreColor, glowColor, distFromCenter);
                        c.a *= alpha;
                        
                        // Vertical scanline effect
                        if ((y / 4) % 2 == 0) c *= 1.2f;
                    }
                    colors[y * width + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static void GenerateTapNote(Color[] colors, int w, int h, Color baseCol, Color glowCol)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Rounded Rectangle Box
                    float u = (float)x / w;
                    float v = (float)y / h;
                    
                    if (x < 5 || x > w-5 || y < 5 || y > h-5) continue; // Margin

                    // Border
                    if (x < 10 || x > w-10 || y < 10 || y > h-10)
                    {
                        colors[y * w + x] = glowCol;
                    }
                    else
                    {
                        colors[y * w + x] = baseCol;
                    }
                }
            }
        }

        private static void GenerateLongNoteHead(Color[] colors, int w, int h, Color baseCol, Color glowCol)
        {
            // Arrow shape
            float cx = w / 2f;
            float cy = h / 2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Triangle pointing down check? Or Up? usually Down for falling notes.
                    // Let's make a simple box for now but with different style
                    float u = (float)x / w;
                    float v = (float)y / h;

                    if (x < 5 || x > w-5 || y < 5 || y > h-5) continue; 

                    colors[y * w + x] = baseCol;
                    
                    // Center glow
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist < h/3f) colors[y * w + x] = glowCol;
                }
            }
        }

         private static void GenerateScratchNote(Color[] colors, int w, int h, Color baseCol, Color glowCol)
        {
            // Zigzag pattern
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                     float u = (float)x / w;
                     // Zigzag offset
                     float offset = Mathf.Sin(y * 0.2f) * 10f;
                     
                     if (x > 10 + offset && x < w - 10 + offset)
                     {
                         if (x < 15 + offset || x > w - 15 + offset)
                             colors[y*w+x] = glowCol;
                         else
                             colors[y*w+x] = baseCol;
                     }
                }
            }
        }


        // ==================================================================================
        // NEW: Judgement Effect Generation (Sprite Sheet 4x4)
        // ==================================================================================
        
        /// <summary>
        /// Generates a 4x4 sprite sheet (512x512 total, 128x128 per frame)
        /// 16 frames of animation
        /// </summary>
        public static Texture2D CreateJudgementSheet(string type)
        {
            int frameSize = 128;
            int gridSize = 4;
            int totalSize = frameSize * gridSize; // 512
            
            Texture2D tex = new Texture2D(totalSize, totalSize, TextureFormat.RGBA32, false);
            Color[] colors = new Color[totalSize * totalSize];
            for(int i=0; i<colors.Length; i++) colors[i] = Color.clear;

            Color mainColor = Color.white;
            switch(type)
            {
                case "Perfect": mainColor = new Color(1f, 0.84f, 0f); break; // Gold
                case "Great": mainColor = new Color(0f, 0.8f, 0.9f); break; // Teal
                case "Good": mainColor = new Color(0.6f, 0.9f, 0.2f); break; // Green
                case "Bad": mainColor = new Color(1f, 0.5f, 0f); break; // Orange
            }

            // Generate 16 frames
            for (int i = 0; i < 16; i++)
            {
                int gridX = i % 4;
                int gridY = 3 - (i / 4); // Top to bottom

                // Time 0.0 to 1.0
                float t = i / 15f; 
                
                DrawExplosionFrame(colors, totalSize, gridX * frameSize, gridY * frameSize, frameSize, t, mainColor, type);
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static void DrawExplosionFrame(Color[] pixels, int texWidth, int offsetX, int offsetY, int size, float t, Color color, string type)
        {
            float cx = offsetX + size / 2f;
            float cy = offsetY + size / 2f;
            float maxRadius = size * 0.45f;

            // Explosion logic based on type
            for (int y = offsetY; y < offsetY + size; y++)
            {
                for (int x = offsetX; x < offsetX + size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx*dx + dy*dy);
                    float normDist = dist / maxRadius;

                    Color pixColor = Color.clear;

                    // Expanding ring animation
                    float currentRadius = t * maxRadius;
                    float ringWidth = maxRadius * 0.1f * (1f - t); // Gets thinner
                    
                    if (Mathf.Abs(dist - currentRadius) < ringWidth)
                    {
                        float alpha = 1f - t; // Fade out
                        pixColor = color;
                        pixColor.a = alpha;
                        
                        // Core flash at start
                        if (t < 0.2f && normDist < 0.2f)
                        {
                            pixColor = Color.white;
                        }
                    }
                    
                    if (pixColor.a > 0)
                    {
                         // Additive blending (simplified)
                         int idx = y * texWidth + x;
                         Color existing = pixels[idx];
                         pixels[idx] = existing + pixColor; 
                    }
                }
            }
        }

        // ==================================================================================
        // File Saving I/O
        // ==================================================================================
        
        public static void SaveTextureAsPNG(Texture2D tex, string fullPath)
        {
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);
#if UNITY_EDITOR
            Debug.Log($"[ProceduralGen] Saved to {fullPath}");
#endif
        }
    }
}
