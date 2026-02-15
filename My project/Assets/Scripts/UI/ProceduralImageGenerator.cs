using UnityEngine;
using AIBeat.Data;

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
        
        // ==================================================================================
        // NEW: UI Asset Generation (Cyberpunk Style)
        // ==================================================================================

        public static Texture2D CreateGradientBackground(int width, int height, Color top, Color bottom, Color gridCol, float gridAlpha = 0.05f)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float vy = (float)y / height;
                Color bgColor = Color.Lerp(bottom, top, vy);
                for (int x = 0; x < width; x++)
                {
                    Color c = bgColor;
                    // Grid
                    if (x % 64 == 0 || y % 64 == 0)
                    {
                        Color g = gridCol;
                        g.a = gridAlpha;
                        c = c + g * g.a; // Simple additive
                    }
                    // Vignette
                    float u = (float)x / width - 0.5f;
                    float v = (float)y / height - 0.5f;
                    float dist = Mathf.Sqrt(u*u + v*v);
                    c *= (1f - dist * 0.8f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateRoundedButton(int width, int height, Color baseColor, Color borderColor, bool isHover = false)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];
            
            float radius = 10f;
            Color glowColor = isHover ? new Color(1f, 1f, 1f, 0.3f) : Color.clear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Rounded Rect Logic
                    // Check corners
                    bool inside = true;
                    if (x < radius && y < radius) inside = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
                    else if (x > width-radius && y < radius) inside = Vector2.Distance(new Vector2(x, y), new Vector2(width-radius, radius)) <= radius;
                    else if (x < radius && y > height-radius) inside = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height-radius)) <= radius;
                    else if (x > width-radius && y > height-radius) inside = Vector2.Distance(new Vector2(x, y), new Vector2(width-radius, height-radius)) <= radius;

                    if (!inside) { colors[y*width+x] = Color.clear; continue; }

                    // Border
                    bool border = false;
                    float borderThick = 2f;
                    if (x < borderThick || x > width-borderThick || y < borderThick || y > height-borderThick) border = true;
                    // Corner borders... simplified
                    
                    Color c = baseColor;
                    if (border) c = borderColor;
                    else 
                    {
                        // Gradient fill
                        c = Color.Lerp(baseColor, baseColor * 0.7f, (float)y/height);
                        c += glowColor;
                    }
                    colors[y*width+x] = c;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateSimpleLogo()
        {
            int w = 512;
            int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // Clear
             Color[] colors = new Color[w * h];
             for(int i=0; i<colors.Length; i++) colors[i] = Color.clear;
             tex.SetPixels(colors);

            // Draw "A.I. BEAT" roughly with boxes/lines
            // Just drawing a big Neon Box with text "AI BEAT" might be hard.
            // Let's create a cool "Waveform" pattern instead.
            
            Color neon = new Color(0f, 1f, 1f); // Cyan
            for(int x=0; x<w; x++)
            {
                float normalizedX = (float)x/w;
                // Wave function
                float waveY = Mathf.Sin(normalizedX * 20f) * 30f * Mathf.Sin(normalizedX*3f);
                int centerY = h/2;
                int yPos = centerY + (int)waveY;
                
                // Draw vertical bar at x
                int barHeight = (int)(Mathf.Abs(waveY) * 1.5f) + 5;
                for(int y=centerY - barHeight; y <= centerY + barHeight; y++)
                {
                    if (y >=0 && y < h)
                        tex.SetPixel(x, y, neon);
                }
            }
            
            tex.Apply();
            return tex;
        }

        public static void SaveTextureAsPNG(Texture2D tex, string fullPath)
        {
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);
#if UNITY_EDITOR
            Debug.Log($"[ProceduralGen] Saved to {fullPath}");
#endif
        }

        // ==================================================================================
        // NEW: Instrument Asset Generation (Silhouettes)
        // ==================================================================================

        public static Texture2D CreateInstrumentTexture(string type)
        {
            int w = 512;
            int h = 512;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // CRITICAL: Initialize with Color.clear to ensure transparency
            Color[] colors = new Color[w * h];
            for(int i=0; i<colors.Length; i++) colors[i] = Color.clear; 

            Color neonColor = Color.white;
            switch(type)
            {
                case "Drum": neonColor = new Color(0f, 1f, 1f); break;    // Cyan
                case "Piano": neonColor = new Color(1f, 0f, 0.8f); break; // Magenta
                case "Guitar": neonColor = new Color(1f, 1f, 0f); break;  // Yellow
                case "Notes": neonColor = Color.white; break;             // Mixed later
                // Performers
                case "Drummer": neonColor = new Color(0f, 1f, 1f); break;
                case "Pianist": neonColor = new Color(1f, 0f, 0.8f); break;
                case "Guitarist": neonColor = new Color(1f, 1f, 0f); break;
                case "DJ": neonColor = new Color(0.6f, 0f, 1f); break; // Purple
            }

            if (type == "Drum") DrawDrumSilhouette(colors, w, h, neonColor);
            else if (type == "Piano") DrawPianoSilhouette(colors, w, h, neonColor);
            else if (type == "Guitar") DrawGuitarSilhouette(colors, w, h, neonColor);
            else if (type == "Notes") DrawMusicNotes(colors, w, h);
            // Performers
            else if (type == "Drummer") DrawDrummerSilhouette(colors, w, h, neonColor);
            else if (type == "Pianist") DrawPianistSilhouette(colors, w, h, neonColor);
            else if (type == "Guitarist") DrawGuitaristSilhouette(colors, w, h, neonColor);
            else if (type == "DJ") DrawDJSilhouette(colors, w, h, neonColor);

            tex.SetPixels(colors);

            tex.Apply();
            return tex;
        }

        private static void DrawDrumSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Simple Drum Set Silhouette: Bass drum (center), Snare, Toms, Cymbals
            // Bass Drum (Circle)
            DrawCircleOutline(pixels, w, w/2, h/3, 80, color);
            // Tom 1 (Circle)
            DrawCircleOutline(pixels, w, w/2 - 60, h/2 + 20, 40, color);
            // Tom 2 (Circle)
            DrawCircleOutline(pixels, w, w/2 + 60, h/2 + 20, 40, color);
            // Snare (Ovular?)
            DrawCircleOutline(pixels, w, w/2 - 90, h/3 + 10, 45, color);
            // Floor Tom
            DrawCircleOutline(pixels, w, w/2 + 90, h/3 - 20, 55, color);
            // Cymbal Left (Line + flat oval)
            DrawLine(pixels, w, w/2 - 120, h/2, w/2 - 150, h/2 + 100, color); // Stand
            DrawCircleOutline(pixels, w, w/2 - 150, h/2 + 100, 50, color);
            // Cymbal Right
            DrawLine(pixels, w, w/2 + 120, h/2, w/2 + 150, h/2 + 120, color); // Stand
            DrawCircleOutline(pixels, w, w/2 + 150, h/2 + 120, 50, color);
        }

        private static void DrawPianoSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Grand Piano Side View shape
            // Body
            int startX = 100;
            int startY = 150;
            // Draw Outline
            // Top curve
             for (int x = startX; x < w - 100; x++)
            {
                // Top Lid line
                int y = startY + (int)(Mathf.Sin((float)x/w * 3f) * 20) + 150;
                 if (x < w/2) y = startY + 150; // Flat part
                
                SetPixelSafe(pixels, w, x, y, color);
                // Bottom Body line
                int yBot = startY + 50;
                SetPixelSafe(pixels, w, x, yBot, color);
            }
             
             // Legs
             DrawLine(pixels, w, startX + 20, startY + 50, startX + 20, startY - 50, color);
             DrawLine(pixels, w, w - 150, startY + 50, w - 150, startY - 50, color);
             DrawLine(pixels, w, w/2, startY + 50, w/2, startY - 50, color); // Back leg
        }

        private static void DrawGuitarSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Electric Guitar Vertical
            int cx = w/2;
            int cy = h/2;
            
            // Body (Hourglass shape)
            for (int y = h/4; y < h/2 + 50; y++)
            {
                float normalizedY = (float)(y - h/4) / (h/4 + 50); // 0 to 1
                // Width varies
                float width = 60 + Mathf.Sin(normalizedY * Mathf.PI * 2) * 20; 
                // Outline only
                SetPixelSafe(pixels, w, cx - (int)width, y, color);
                SetPixelSafe(pixels, w, cx + (int)width, y, color);
            }
            // Neck
            for(int y = h/2 + 50; y < h - 50; y++)
            {
                SetPixelSafe(pixels, w, cx - 10, y, color);
                SetPixelSafe(pixels, w, cx + 10, y, color);
            }
            // Headstock
            DrawCircleOutline(pixels, w, cx, h - 30, 20, color);
        }

        private static void DrawMusicNotes(Color[] pixels, int w, int h)
        {
            // Random Notes
            DrawNote(pixels, w, 60, 60, new Color(0f, 1f, 1f)); // Cyan
            DrawNote(pixels, w, 180, 150, new Color(1f, 0f, 0.8f)); // Magenta
            DrawNote(pixels, w, 100, 200, new Color(1f, 1f, 0f)); // Yellow
        }

        private static void DrawNote(Color[] pixels, int w, int cx, int cy, Color c)
        {
            // Simple Eighth Note
            DrawCircleOutline(pixels, w, cx, cy, 15, c); // Head
            DrawLine(pixels, w, cx + 15, cy, cx + 15, cy + 60, c); // Stem
            DrawLine(pixels, w, cx + 15, cy + 60, cx + 35, cy + 40, c); // Flag
        }

        private static void DrawCircleOutline(Color[] pixels, int w, int cx, int cy, int r, Color c, int thickness = 1)
        {
            for (int x = cx - r - thickness; x <= cx + r + thickness; x++)
            {
                for (int y = cy - r - thickness; y <= cy + r + thickness; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    // Check outline thickness
                    if (Mathf.Abs(dist - r) < thickness)
                    {
                        SetPixelSafe(pixels, w, x, y, c);
                    }
                }
            }
        }
        
        private static void DrawLine(Color[] pixels, int w, int x0, int y0, int x1, int y1, Color c)
        {
             // Simple Bresenham or similar could be used, but for brevity simpler approach or assuming straight lines
             // Vertical/Horizontal easy
             if (x0 == x1)
             {
                 for(int y = Mathf.Min(y0,y1); y<=Mathf.Max(y0,y1); y++) SetPixelSafe(pixels, w, x0, y, c);
             }
             else if (y0 == y1)
             {
                 for(int x = Mathf.Min(x0,x1); x<=Mathf.Max(x0,x1); x++) SetPixelSafe(pixels, w, x, y0, c);
             }
             else
             {
                 // Diagonal simple implementation
                 float steps = Mathf.Max(Mathf.Abs(x1-x0), Mathf.Abs(y1-y0));
                 for(float t=0; t<=1; t+=1f/steps)
                 {
                     int x = (int)Mathf.Lerp(x0, x1, t);
                     int y = (int)Mathf.Lerp(y0, y1, t);
                     SetPixelSafe(pixels, w, x, y, c);
                 }
             }
        }

        private static void SetPixelSafe(Color[] pixels, int w, int x, int y, Color c)
        {
            if (x >= 0 && x < w && y >= 0 && y < pixels.Length / w)
                pixels[y * w + x] = c;
        }

        // ==================================================================================
        // NEW: Performer Silhouettes (Abstract/Geometric)
        // ==================================================================================

        private static void DrawDrummerSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Detailed drummer silhouette - energetic pose behind drum kit
            int cx = w / 2;
            int baseY = 50; // Ground level from bottom

            // === DRUMMER FIGURE (Upper portion) ===
            // Head - filled circle
            DrawFilledCircle(pixels, w, cx, h - 100, 28, color);
            // Hair/spiky top
            for (int i = -3; i <= 3; i++)
            {
                int spikeX = cx + i * 8;
                DrawThickLine(pixels, w, spikeX, h - 125, spikeX + i * 2, h - 140, color, 2);
            }

            // Neck
            DrawThickLine(pixels, w, cx, h - 128, cx, h - 145, color, 8);

            // Torso - trapezoid shape (shoulders wider)
            DrawFilledTrapezoid(pixels, w, cx - 45, h - 145, cx + 45, h - 145, cx - 35, h - 220, cx + 35, h - 220, color);

            // Left arm - raised with drumstick
            DrawThickLine(pixels, w, cx - 40, h - 150, cx - 75, h - 120, color, 6); // Upper arm
            DrawThickLine(pixels, w, cx - 75, h - 120, cx - 100, h - 80, color, 5); // Forearm
            DrawThickLine(pixels, w, cx - 100, h - 80, cx - 130, h - 50, color, 3); // Drumstick

            // Right arm - hitting pose
            DrawThickLine(pixels, w, cx + 40, h - 150, cx + 80, h - 130, color, 6); // Upper arm
            DrawThickLine(pixels, w, cx + 80, h - 130, cx + 110, h - 100, color, 5); // Forearm
            DrawThickLine(pixels, w, cx + 110, h - 100, cx + 140, h - 70, color, 3); // Drumstick

            // Stool/seat hint
            DrawFilledCircle(pixels, w, cx, h - 235, 25, color);

            // Legs (sitting)
            DrawThickLine(pixels, w, cx - 15, h - 235, cx - 50, h - 300, color, 8);
            DrawThickLine(pixels, w, cx + 15, h - 235, cx + 50, h - 300, color, 8);
            // Feet on pedals
            DrawFilledCircle(pixels, w, cx - 55, h - 305, 12, color);
            DrawFilledCircle(pixels, w, cx + 55, h - 305, 12, color);

            // === DRUM KIT ===
            // Bass drum (center, large)
            DrawFilledCircle(pixels, w, cx, h - 360, 55, color);
            DrawCircleOutline(pixels, w, cx, h - 360, 55, color, 3);
            DrawCircleOutline(pixels, w, cx, h - 360, 45, color, 2);

            // Snare drum (left front)
            DrawFilledEllipse(pixels, w, cx - 90, h - 320, 35, 15, color);
            DrawThickLine(pixels, w, cx - 90, h - 320, cx - 90, h - 280, color, 3); // Stand

            // Hi-hat (far left)
            DrawFilledEllipse(pixels, w, cx - 140, h - 280, 30, 10, color);
            DrawThickLine(pixels, w, cx - 140, h - 290, cx - 140, h - 330, color, 2);

            // Tom drums (above bass)
            DrawFilledEllipse(pixels, w, cx - 40, h - 410, 28, 12, color);
            DrawFilledEllipse(pixels, w, cx + 40, h - 410, 28, 12, color);

            // Floor tom (right)
            DrawFilledEllipse(pixels, w, cx + 100, h - 340, 38, 15, color);

            // Crash cymbal (right, high)
            DrawFilledEllipse(pixels, w, cx + 130, h - 440, 40, 8, color);
            DrawThickLine(pixels, w, cx + 130, h - 432, cx + 130, h - 360, color, 2);

            // Ride cymbal (far right)
            DrawFilledEllipse(pixels, w, cx + 160, h - 380, 35, 7, color);
        }

        private static void DrawPianistSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Detailed pianist - elegant pose at grand piano (side view)
            int cx = w / 2;

            // === PIANIST FIGURE ===
            // Head
            DrawFilledCircle(pixels, w, cx - 60, h - 110, 26, color);
            // Hair (slicked back)
            DrawFilledEllipse(pixels, w, cx - 65, h - 100, 30, 20, color);

            // Neck (tilted forward, focused)
            DrawThickLine(pixels, w, cx - 55, h - 136, cx - 40, h - 155, color, 7);

            // Torso (seated, leaning slightly forward)
            DrawFilledTrapezoid(pixels, w, cx - 65, h - 155, cx - 15, h - 155, cx - 55, h - 240, cx - 5, h - 240, color);

            // Left arm (reaching for bass keys)
            DrawThickLine(pixels, w, cx - 55, h - 165, cx - 100, h - 185, color, 6);
            DrawThickLine(pixels, w, cx - 100, h - 185, cx - 130, h - 220, color, 5);
            // Left hand
            DrawFilledCircle(pixels, w, cx - 135, h - 225, 10, color);

            // Right arm (treble keys)
            DrawThickLine(pixels, w, cx - 25, h - 165, cx + 30, h - 180, color, 6);
            DrawThickLine(pixels, w, cx + 30, h - 180, cx + 70, h - 220, color, 5);
            // Right hand
            DrawFilledCircle(pixels, w, cx + 75, h - 225, 10, color);

            // Fingers detail (both hands)
            for (int i = 0; i < 5; i++)
            {
                DrawThickLine(pixels, w, cx - 135 + i * 4, h - 225, cx - 140 + i * 5, h - 240, color, 2);
                DrawThickLine(pixels, w, cx + 70 + i * 4, h - 225, cx + 65 + i * 5, h - 240, color, 2);
            }

            // Bench
            DrawFilledRect(pixels, w, cx - 80, h - 260, cx + 10, h - 250, color);
            // Bench legs
            DrawThickLine(pixels, w, cx - 75, h - 260, cx - 75, h - 290, color, 4);
            DrawThickLine(pixels, w, cx + 5, h - 260, cx + 5, h - 290, color, 4);

            // Legs
            DrawThickLine(pixels, w, cx - 45, h - 250, cx - 60, h - 320, color, 7);
            DrawThickLine(pixels, w, cx - 15, h - 250, cx, h - 320, color, 7);
            // Feet
            DrawFilledEllipse(pixels, w, cx - 65, h - 325, 15, 8, color);
            DrawFilledEllipse(pixels, w, cx + 5, h - 325, 15, 8, color);

            // === GRAND PIANO ===
            // Piano body (curved top)
            for (int x = cx - 150; x <= cx + 150; x++)
            {
                float normalX = (float)(x - (cx - 150)) / 300f;
                int topY = h - 350 + (int)(Mathf.Sin(normalX * Mathf.PI * 0.5f) * 60);
                DrawThickLine(pixels, w, x, h - 240, x, topY, color, 2);
            }

            // Piano lid (propped open)
            DrawThickLine(pixels, w, cx + 140, h - 350, cx + 180, h - 450, color, 4);
            DrawThickLine(pixels, w, cx + 180, h - 450, cx - 50, h - 420, color, 3);

            // Keyboard (white keys)
            DrawFilledRect(pixels, w, cx - 140, h - 250, cx + 90, h - 240, color);
            // Black keys hint
            for (int i = 0; i < 12; i++)
            {
                int keyX = cx - 130 + i * 18;
                if (i % 7 != 2 && i % 7 != 6) // Skip E-F and B-C
                    DrawFilledRect(pixels, w, keyX, h - 250, keyX + 8, h - 242, color);
            }

            // Piano legs
            DrawThickLine(pixels, w, cx - 120, h - 240, cx - 130, h - 300, color, 6);
            DrawThickLine(pixels, w, cx + 100, h - 240, cx + 110, h - 300, color, 6);
            // Pedals
            DrawFilledRect(pixels, w, cx - 80, h - 310, cx - 40, h - 305, color);
        }

        private static void DrawGuitaristSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Detailed guitarist - rock star power stance
            int cx = w / 2;

            // === GUITARIST FIGURE ===
            // Head (tilted back, intense)
            DrawFilledCircle(pixels, w, cx + 20, h - 85, 26, color);
            // Long hair flowing
            for (int i = -4; i <= 4; i++)
            {
                int hairX = cx + 20 + i * 6;
                DrawThickLine(pixels, w, hairX, h - 100, hairX + i * 3, h - 140 - Mathf.Abs(i) * 5, color, 3);
            }

            // Neck
            DrawThickLine(pixels, w, cx + 15, h - 111, cx + 5, h - 130, color, 7);

            // Torso (angled, dynamic pose)
            DrawFilledTrapezoid(pixels, w, cx - 25, h - 130, cx + 35, h - 130, cx - 40, h - 220, cx + 20, h - 220, color);

            // Left arm (fretting hand on neck)
            DrawThickLine(pixels, w, cx - 20, h - 140, cx - 70, h - 160, color, 6);
            DrawThickLine(pixels, w, cx - 70, h - 160, cx - 100, h - 200, color, 5);
            // Fretting hand
            DrawFilledCircle(pixels, w, cx - 105, h - 205, 10, color);
            // Fingers on fretboard
            for (int i = 0; i < 4; i++)
                DrawThickLine(pixels, w, cx - 105, h - 200 + i * 5, cx - 115, h - 195 + i * 5, color, 2);

            // Right arm (strumming)
            DrawThickLine(pixels, w, cx + 25, h - 145, cx + 70, h - 180, color, 6);
            DrawThickLine(pixels, w, cx + 70, h - 180, cx + 50, h - 230, color, 5);
            // Strumming hand
            DrawFilledCircle(pixels, w, cx + 45, h - 235, 10, color);

            // Hips
            DrawFilledEllipse(pixels, w, cx - 10, h - 230, 35, 15, color);

            // Left leg (wide stance, bent)
            DrawThickLine(pixels, w, cx - 25, h - 235, cx - 70, h - 320, color, 9);
            DrawThickLine(pixels, w, cx - 70, h - 320, cx - 80, h - 410, color, 8);
            // Boot
            DrawFilledEllipse(pixels, w, cx - 85, h - 420, 20, 12, color);

            // Right leg (straight, power stance)
            DrawThickLine(pixels, w, cx + 10, h - 235, cx + 60, h - 330, color, 9);
            DrawThickLine(pixels, w, cx + 60, h - 330, cx + 70, h - 420, color, 8);
            // Boot
            DrawFilledEllipse(pixels, w, cx + 75, h - 428, 20, 12, color);

            // === ELECTRIC GUITAR ===
            // Guitar body (Les Paul style - two cutaways)
            DrawFilledCircle(pixels, w, cx + 30, h - 250, 45, color);
            DrawFilledCircle(pixels, w, cx + 20, h - 290, 35, color);
            // Cutaway
            DrawFilledCircle(pixels, w, cx + 50, h - 275, 20, Color.clear); // Negative space

            // Pickups
            DrawFilledRect(pixels, w, cx + 10, h - 260, cx + 50, h - 255, color);
            DrawFilledRect(pixels, w, cx + 10, h - 245, cx + 50, h - 240, color);

            // Bridge
            DrawFilledRect(pixels, w, cx + 15, h - 225, cx + 45, h - 220, color);

            // Guitar neck
            DrawThickLine(pixels, w, cx + 20, h - 295, cx - 90, h - 200, color, 8);
            // Frets
            for (int i = 0; i < 6; i++)
            {
                float t = i / 6f;
                int fretX = (int)Mathf.Lerp(cx + 15, cx - 80, t);
                int fretY = (int)Mathf.Lerp(h - 290, h - 205, t);
                DrawThickLine(pixels, w, fretX - 5, fretY - 3, fretX + 5, fretY + 3, color, 1);
            }

            // Headstock
            DrawFilledRect(pixels, w, cx - 105, h - 190, cx - 85, h - 160, color);
            // Tuning pegs
            for (int i = 0; i < 3; i++)
            {
                DrawFilledCircle(pixels, w, cx - 110, h - 170 + i * 10, 4, color);
                DrawFilledCircle(pixels, w, cx - 80, h - 170 + i * 10, 4, color);
            }

            // Strap
            DrawThickLine(pixels, w, cx - 5, h - 130, cx + 60, h - 210, color, 3);
            DrawThickLine(pixels, w, cx + 10, h - 310, cx - 30, h - 235, color, 3);
        }

        private static void DrawDJSilhouette(Color[] pixels, int w, int h, Color color)
        {
            // Detailed DJ - behind decks with headphones
            int cx = w / 2;

            // === 1. BOOTH LEGS (테이블 다리만) ===
            DrawThickLine(pixels, w, cx - 150, h - 275, cx - 150, h - 420, color, 6);
            DrawThickLine(pixels, w, cx + 150, h - 275, cx + 150, h - 420, color, 6);
            // Cross bar
            DrawThickLine(pixels, w, cx - 150, h - 400, cx + 150, h - 400, color, 4);

            // === 2. DJ BOOTH/DECKS (부스 위에 그림) ===
            // Laptop/controller behind
            DrawFilledRect(pixels, w, cx - 50, h - 400, cx + 50, h - 360, color);

            // Left turntable/CDJ
            DrawFilledCircle(pixels, w, cx - 90, h - 320, 50, color);
            DrawCircleOutline(pixels, w, cx - 90, h - 320, 50, color, 3);
            DrawCircleOutline(pixels, w, cx - 90, h - 320, 35, color, 2);
            DrawCircleOutline(pixels, w, cx - 90, h - 320, 15, color, 2);
            DrawFilledCircle(pixels, w, cx - 90, h - 320, 5, color);
            // Tonearm
            DrawThickLine(pixels, w, cx - 50, h - 360, cx - 70, h - 340, color, 3);
            DrawThickLine(pixels, w, cx - 70, h - 340, cx - 85, h - 330, color, 2);

            // Right turntable/CDJ
            DrawFilledCircle(pixels, w, cx + 90, h - 320, 50, color);
            DrawCircleOutline(pixels, w, cx + 90, h - 320, 50, color, 3);
            DrawCircleOutline(pixels, w, cx + 90, h - 320, 35, color, 2);
            DrawCircleOutline(pixels, w, cx + 90, h - 320, 15, color, 2);
            DrawFilledCircle(pixels, w, cx + 90, h - 320, 5, color);
            // Tonearm
            DrawThickLine(pixels, w, cx + 130, h - 360, cx + 110, h - 340, color, 3);
            DrawThickLine(pixels, w, cx + 110, h - 340, cx + 95, h - 330, color, 2);

            // Center mixer
            DrawFilledRect(pixels, w, cx - 35, h - 350, cx + 35, h - 280, color);
            // Mixer knobs (3x3 grid)
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int knobX = cx - 20 + col * 20;
                    int knobY = h - 340 + row * 20;
                    DrawFilledCircle(pixels, w, knobX, knobY, 5, color);
                }
            }
            // Crossfader
            DrawFilledRect(pixels, w, cx - 25, h - 285, cx + 25, h - 280, color);

            // Main table surface
            DrawFilledRect(pixels, w, cx - 160, h - 275, cx + 160, h - 265, color);

            // === 3. DJ FIGURE (맨 앞에 그림) ===
            // Head
            DrawFilledCircle(pixels, w, cx, h - 95, 28, color);

            // Headphones
            for (int x = cx - 35; x <= cx + 35; x++)
            {
                float t = (float)(x - (cx - 35)) / 70f;
                int bandY = h - 70 - (int)(Mathf.Sin(t * Mathf.PI) * 25);
                DrawThickLine(pixels, w, x, bandY, x, bandY + 3, color, 3);
            }
            DrawFilledCircle(pixels, w, cx - 35, h - 95, 14, color);
            DrawFilledCircle(pixels, w, cx + 35, h - 95, 14, color);

            // Sunglasses
            DrawThickLine(pixels, w, cx - 20, h - 90, cx + 20, h - 90, color, 4);

            // Neck
            DrawThickLine(pixels, w, cx, h - 123, cx, h - 145, color, 8);

            // Torso
            DrawFilledTrapezoid(pixels, w, cx - 40, h - 145, cx + 40, h - 145, cx - 35, h - 220, cx + 35, h - 220, color);

            // Left arm (raised, fist pump)
            DrawThickLine(pixels, w, cx - 35, h - 155, cx - 80, h - 120, color, 6);
            DrawThickLine(pixels, w, cx - 80, h - 120, cx - 100, h - 70, color, 5);
            DrawFilledCircle(pixels, w, cx - 100, h - 65, 12, color);

            // Right arm (on mixer)
            DrawThickLine(pixels, w, cx + 35, h - 160, cx + 70, h - 200, color, 6);
            DrawThickLine(pixels, w, cx + 70, h - 200, cx + 50, h - 250, color, 5);
            DrawFilledCircle(pixels, w, cx + 48, h - 255, 10, color);
        }

        // ==================================================================================
        // NEW: Advanced Drawing Helpers for Detailed Silhouettes
        // ==================================================================================

        private static void DrawFilledCircle(Color[] pixels, int w, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist <= radius)
                        SetPixelSafe(pixels, w, x, y, color);
                }
            }
        }

        private static void DrawFilledEllipse(Color[] pixels, int w, int cx, int cy, int radiusX, int radiusY, Color color)
        {
            for (int y = cy - radiusY; y <= cy + radiusY; y++)
            {
                for (int x = cx - radiusX; x <= cx + radiusX; x++)
                {
                    float dx = (float)(x - cx) / radiusX;
                    float dy = (float)(y - cy) / radiusY;
                    if (dx * dx + dy * dy <= 1f)
                        SetPixelSafe(pixels, w, x, y, color);
                }
            }
        }

        private static void DrawThickLine(Color[] pixels, int w, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            float steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;

            for (float t = 0; t <= 1; t += 1f / steps)
            {
                int x = (int)Mathf.Lerp(x0, x1, t);
                int y = (int)Mathf.Lerp(y0, y1, t);
                // Draw a filled circle at each point for thickness
                for (int dy = -thickness / 2; dy <= thickness / 2; dy++)
                {
                    for (int dx = -thickness / 2; dx <= thickness / 2; dx++)
                    {
                        if (dx * dx + dy * dy <= (thickness / 2) * (thickness / 2) + 1)
                            SetPixelSafe(pixels, w, x + dx, y + dy, color);
                    }
                }
            }
        }

        private static void DrawFilledRect(Color[] pixels, int w, int x0, int y0, int x1, int y1, Color color)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixelSafe(pixels, w, x, y, color);
                }
            }
        }

        private static void DrawFilledTrapezoid(Color[] pixels, int w, int topLeft, int topY, int topRight, int topY2, int botLeft, int botY, int botRight, int botY2, Color color)
        {
            int minY = Mathf.Min(topY, botY);
            int maxY = Mathf.Max(topY, botY);

            for (int y = minY; y <= maxY; y++)
            {
                float t = (float)(y - topY) / (botY - topY);
                int leftX = (int)Mathf.Lerp(topLeft, botLeft, t);
                int rightX = (int)Mathf.Lerp(topRight, botRight, t);

                for (int x = leftX; x <= rightX; x++)
                {
                    SetPixelSafe(pixels, w, x, y, color);
                }
            }
        }
    }
}


