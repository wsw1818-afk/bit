using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace AIBeat.Editor
{
    /// <summary>
    /// 프로시저럴 텍스처 생성 (노트, 레인, 배경, UI 등)
    /// 에디터 전용 - Assets/Textures/Generated/ 에 PNG로 저장
    /// </summary>
    public class TextureGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
#pragma warning disable 0414
        private static readonly string OUTPUT_DIR = "Assets/Textures/Generated";
#pragma warning restore 0414

        [MenuItem("Tools/A.I. BEAT/Generate Assets")]
        public static void GenerateAllAssets()
        {
            EnsureDirectoryExists();

            // Background & UI
            GenerateCyberpunkBackground();
            GeneratePanelBackground();
            GenerateLaneSeparator();
            GenerateGlowParticle();

            // Note textures
            GenerateNoteTexture("TapNote.png", new Color(0f, 0.9f, 1f), false);
            GenerateNoteTexture("LongNoteBody.png", new Color(0.4f, 0.2f, 1f), true);
            GenerateNoteTexture("ScratchNote.png", new Color(1f, 0.2f, 0.4f), false);
            GenerateLongNoteCap("LongNoteCap.png", new Color(0.5f, 0.3f, 1f));

            // Lane backgrounds
            GenerateLaneBackground("LaneKeyBg.png", new Color(0.1f, 0.1f, 0.2f, 0.5f));
            GenerateLaneBackground("LaneScratchBg.png", new Color(0.15f, 0.05f, 0.1f, 0.5f));

            // Judgement line
            GenerateJudgementLine();

            // Hit effect
            GenerateHitEffect();

            AssetDatabase.Refresh();
            Debug.Log("[TextureGenerator] All assets generated in Assets/Textures/Generated/");
        }

        [MenuItem("Tools/A.I. BEAT/Generate Note Textures Only")]
        public static void GenerateNoteTexturesOnly()
        {
            EnsureDirectoryExists();
            GenerateNoteTexture("TapNote.png", new Color(0f, 0.9f, 1f), false);
            GenerateNoteTexture("LongNoteBody.png", new Color(0.4f, 0.2f, 1f), true);
            GenerateNoteTexture("ScratchNote.png", new Color(1f, 0.2f, 0.4f), false);
            GenerateLongNoteCap("LongNoteCap.png", new Color(0.5f, 0.3f, 1f));
            AssetDatabase.Refresh();
            Debug.Log("[TextureGenerator] Note textures generated");
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

        // ===== Background & UI =====

        private static void GenerateCyberpunkBackground()
        {
            int width = 1080;
            int height = 1920;
            Texture2D tex = new Texture2D(width, height);

            Color topColor = new Color(0.05f, 0.0f, 0.1f);
            Color bottomColor = new Color(0.2f, 0.0f, 0.4f);
            Color gridColor = new Color(0.0f, 1.0f, 1.0f, 0.3f);

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                Color bgColor = Color.Lerp(bottomColor, topColor, v);

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    bool isGridLine = false;
                    if (Mathf.Abs(x - width / 2) % 180 < 2) isGridLine = true;
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
            Color borderColor = new Color(0f, 1f, 1f, 1f);
            int borderSize = 8;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < borderSize || x > size - borderSize ||
                                     y < borderSize || y > size - borderSize);
                    tex.SetPixel(x, y, isBorder ? borderColor : contentColor);
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

            for (int y = 0; y < height; y++)
            {
                float alpha = 0.3f * Mathf.Sin((float)y / height * Mathf.PI);
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, "LaneSeparator.png");
        }

        private static void GenerateGlowParticle()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normDist = dist / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - normDist);
                    alpha = Mathf.Pow(alpha, 2);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, "GlowParticle.png");
        }

        // ===== Note Textures =====

        /// <summary>
        /// 노트 텍스처 생성 (Tap/Scratch: 128x32, LongBody: 128x128 tiling)
        /// </summary>
        private static void GenerateNoteTexture(string fileName, Color baseColor, bool isTiling)
        {
            int width = 128;
            int height = isTiling ? 128 : 32;
            Texture2D tex = new Texture2D(width, height);

            Color dark = baseColor * 0.4f;
            dark.a = 1f;
            Color bright = baseColor;
            bright.a = 1f;
            Color glow = Color.Lerp(baseColor, Color.white, 0.5f);
            glow.a = 1f;

            for (int y = 0; y < height; y++)
            {
                float vy = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float vx = (float)x / width;

                    // Edge fade (left/right)
                    float edgeFade = 1f;
                    float edgeDist = Mathf.Min(vx, 1f - vx) * 2f; // 0 at edges, 1 at center
                    if (edgeDist < 0.15f)
                        edgeFade = edgeDist / 0.15f;

                    Color pixel;
                    if (isTiling)
                    {
                        // Long note body: vertical gradient with pulse lines
                        float grad = Mathf.Lerp(0.5f, 1f, vy);
                        pixel = Color.Lerp(dark, bright, grad);

                        // Pulse lines every 16px
                        if (y % 16 < 2)
                            pixel = Color.Lerp(pixel, glow, 0.4f);
                    }
                    else
                    {
                        // Tap/Scratch: horizontal gradient center-bright
                        float centerDist = Mathf.Abs(vx - 0.5f) * 2f;
                        pixel = Color.Lerp(glow, bright, centerDist);

                        // Top/bottom edge highlight
                        if (y < 3 || y > height - 4)
                            pixel = Color.Lerp(pixel, Color.white, 0.3f);
                    }

                    pixel.a = edgeFade;
                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            SaveTexture(tex, fileName);
        }

        /// <summary>
        /// 롱노트 양 끝 캡 (32x16, rounded)
        /// </summary>
        private static void GenerateLongNoteCap(string fileName, Color baseColor)
        {
            int width = 128;
            int height = 16;
            Texture2D tex = new Texture2D(width, height);

            Color glow = Color.Lerp(baseColor, Color.white, 0.6f);
            glow.a = 1f;

            for (int y = 0; y < height; y++)
            {
                float vy = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float vx = (float)x / width;

                    // Rounded rectangle shape
                    float edgeX = Mathf.Min(vx, 1f - vx) * width;
                    float edgeY = Mathf.Min(vy, 1f - vy) * height;
                    float edge = Mathf.Min(edgeX, edgeY);
                    float alpha = Mathf.Clamp01(edge / 4f);

                    // Center glow
                    float centerDist = Mathf.Abs(vx - 0.5f) * 2f;
                    Color pixel = Color.Lerp(glow, baseColor, centerDist);
                    pixel.a = alpha;

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            SaveTexture(tex, fileName);
        }

        // ===== Lane Textures =====

        /// <summary>
        /// 레인 배경 텍스처 (세로 그라디언트, 반투명)
        /// </summary>
        private static void GenerateLaneBackground(string fileName, Color baseColor)
        {
            int width = 64;
            int height = 512;
            Texture2D tex = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                float vy = (float)y / height;
                // Bottom brighter, top darker
                float brightness = Mathf.Lerp(1.2f, 0.6f, vy);
                float alpha = baseColor.a * Mathf.Lerp(1f, 0.3f, vy);

                for (int x = 0; x < width; x++)
                {
                    float vx = (float)x / width;
                    // Slight horizontal gradient (center brighter)
                    float centerBoost = 1f - Mathf.Abs(vx - 0.5f) * 0.4f;

                    Color pixel = new Color(
                        baseColor.r * brightness * centerBoost,
                        baseColor.g * brightness * centerBoost,
                        baseColor.b * brightness * centerBoost,
                        alpha
                    );
                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            SaveTexture(tex, fileName);
        }

        // ===== Judgement Line =====

        /// <summary>
        /// 판정선 텍스처 (가로 그라디언트, 중앙 밝음)
        /// </summary>
        private static void GenerateJudgementLine()
        {
            int width = 512;
            int height = 16;
            Texture2D tex = new Texture2D(width, height);

            Color lineColor = new Color(1f, 1f, 1f, 0.9f);
            Color glowColor = new Color(0f, 1f, 1f, 0.6f);

            for (int y = 0; y < height; y++)
            {
                float vy = (float)y / height;
                // Vertical fade from center
                float vertFade = 1f - Mathf.Abs(vy - 0.5f) * 2f;
                vertFade = Mathf.Pow(vertFade, 0.5f);

                for (int x = 0; x < width; x++)
                {
                    float vx = (float)x / width;
                    // Horizontal fade at edges
                    float horzFade = Mathf.Min(vx, 1f - vx) * 2f;
                    horzFade = Mathf.Clamp01(horzFade * 3f);

                    float fade = vertFade * horzFade;

                    // Core line (bright white) + glow (cyan)
                    Color pixel;
                    if (Mathf.Abs(vy - 0.5f) < 0.15f)
                        pixel = Color.Lerp(glowColor, lineColor, fade);
                    else
                        pixel = glowColor;

                    pixel.a *= fade;
                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            SaveTexture(tex, "JudgementLine.png");
        }

        // ===== Hit Effect =====

        /// <summary>
        /// 히트 이펙트 텍스처 (방사형 그라디언트, 파티클용)
        /// </summary>
        private static void GenerateHitEffect()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normDist = dist / (size / 2f);

                    // Ring shape: bright at ~0.6 radius, fade inside and outside
                    float ring = 1f - Mathf.Abs(normDist - 0.6f) * 4f;
                    ring = Mathf.Clamp01(ring);

                    // Inner glow
                    float inner = Mathf.Clamp01(1f - normDist * 1.5f);
                    inner = Mathf.Pow(inner, 3);

                    float alpha = Mathf.Max(ring * 0.8f, inner);

                    // Color: white core, cyan edge
                    Color pixel = Color.Lerp(new Color(0f, 1f, 1f), Color.white, inner);
                    pixel.a = alpha;

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            SaveTexture(tex, "HitEffect.png");
        }
#endif
    }
}
