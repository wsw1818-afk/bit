using UnityEngine;

namespace AIBeat.Utils
{
    /// <summary>
    /// 런타임 텍스처 생성 유틸리티
    /// Music/Rhythm 스타일 비주얼을 위한 프로시저럴 텍스처 생성
    /// </summary>
    public static class TextureGenerator
    {
        // Music Theme 컬러 팔레트
        public static readonly Color DeepNavy = new Color(0.05f, 0.08f, 0.15f, 1f);      // 짙은 남색 (배경)
        public static readonly Color MusicGold = new Color(1f, 0.84f, 0f, 1f);           // 골드 (강조)
        public static readonly Color MusicPurple = new Color(0.58f, 0.29f, 0.98f, 1f);   // 보라 (액센트)
        public static readonly Color MusicTeal = new Color(0f, 0.8f, 0.82f, 1f);         // 틸 (보조)
        public static readonly Color MusicOrange = new Color(1f, 0.55f, 0f, 1f);         // 오렌지 (활력)

        /// <summary>
        /// 배경 텍스처 생성 (Deep Navy Gradient - 음악 테마, 그리드 제거)
        /// </summary>
        public static Texture2D CreateBackgroundTexture(int width = 512, int height = 1024)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float normalizedY = (float)y / height;

                // 수직 그라데이션: 아래쪽 어둡게, 위쪽 약간 밝게 (음악 테마)
                Color darkNavy = new Color(0.03f, 0.05f, 0.1f, 1f);
                Color baseColor = Color.Lerp(darkNavy, DeepNavy, normalizedY * 0.6f);

                for (int x = 0; x < width; x++)
                {
                    float normalizedX = (float)x / width;
                    Color pixelColor = baseColor;

                    // 중앙 하이라이트 (비네트 역효과) - 골드 색조
                    float centerDist = Mathf.Abs(normalizedX - 0.5f) * 2f;
                    Color highlight = Color.Lerp(pixelColor, new Color(0.1f, 0.09f, 0.05f, 1f), (1f - centerDist) * 0.15f);
                    pixelColor = highlight;

                    pixels[y * width + x] = pixelColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 레인 구분선 텍스처 생성 (Gold Line - 음악 테마)
        /// </summary>
        public static Texture2D CreateLaneSeparatorTexture(int width = 4, int height = 512)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float normalizedY = (float)y / height;

                // 아래쪽 밝고 위쪽 어둡게 (원근감) - 골드 색상
                float intensity = Mathf.Lerp(1f, 0.3f, normalizedY);
                Color lineColor = MusicGold * intensity;
                lineColor.a = Mathf.Lerp(0.6f, 0.15f, normalizedY);

                for (int x = 0; x < width; x++)
                {
                    // 중앙이 가장 밝음 (글로우 효과)
                    float centerDist = Mathf.Abs((float)x / width - 0.5f) * 2f;
                    float glowIntensity = 1f - (centerDist * centerDist);

                    Color pixelColor = lineColor * glowIntensity;
                    pixelColor.a = lineColor.a * glowIntensity;

                    pixels[y * width + x] = pixelColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 패널 배경 텍스처 생성 (Glassmorphism - 음악 테마)
        /// </summary>
        public static Texture2D CreatePanelBackgroundTexture(int width = 256, int height = 256)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];

            int borderWidth = 2;
            Color borderColor = MusicGold;
            borderColor.a = 0.6f;

            Color fillColor = new Color(0.05f, 0.08f, 0.15f, 0.85f); // 반투명 짙은 남색

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < borderWidth || x >= width - borderWidth ||
                                   y < borderWidth || y >= height - borderWidth;

                    if (isBorder)
                    {
                        // 테두리: 네온 글로우
                        pixels[y * width + x] = borderColor;
                    }
                    else
                    {
                        // 내부: 그라데이션 + 노이즈
                        float normalizedY = (float)y / height;
                        Color innerColor = Color.Lerp(fillColor, fillColor * 0.8f, normalizedY);

                        // 미세한 노이즈 추가
                        float noise = Random.Range(-0.02f, 0.02f);
                        innerColor.r += noise;
                        innerColor.g += noise;
                        innerColor.b += noise;

                        pixels[y * width + x] = innerColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 노트 텍스처 생성 (글로우 효과)
        /// </summary>
        public static Texture2D CreateNoteTexture(int width = 64, int height = 32, Color baseColor = default)
        {
            if (baseColor == default) baseColor = Color.white;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float normalizedY = (float)y / height;
                float yDist = Mathf.Abs(normalizedY - 0.5f) * 2f;

                for (int x = 0; x < width; x++)
                {
                    float normalizedX = (float)x / width;
                    float xDist = Mathf.Abs(normalizedX - 0.5f) * 2f;

                    // 둥근 모서리 + 글로우
                    float dist = Mathf.Sqrt(xDist * xDist * 0.3f + yDist * yDist);
                    float intensity = 1f - Mathf.Clamp01(dist);
                    intensity = intensity * intensity; // 더 강한 중앙 집중

                    Color pixelColor = baseColor * intensity;
                    pixelColor.a = intensity;

                    // 상단 하이라이트
                    if (normalizedY < 0.3f)
                    {
                        pixelColor = Color.Lerp(pixelColor, Color.white, (0.3f - normalizedY) * 0.5f);
                    }

                    pixels[y * width + x] = pixelColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 판정선 텍스처 생성 (골드 글로우 라인 - 음악 테마)
        /// </summary>
        public static Texture2D CreateJudgementLineTexture(int width = 512, int height = 16)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float normalizedY = (float)y / height;
                float yDist = Mathf.Abs(normalizedY - 0.5f) * 2f;
                float glowY = 1f - (yDist * yDist);

                for (int x = 0; x < width; x++)
                {
                    // 수평 그라데이션 (중앙 밝음)
                    float normalizedX = (float)x / width;
                    float xDist = Mathf.Abs(normalizedX - 0.5f) * 2f;
                    float glowX = 1f - (xDist * xDist * 0.5f);

                    float intensity = glowY * glowX;

                    // 코어 라인 (중앙 2픽셀) - 골드
                    if (yDist < 0.3f)
                    {
                        Color coreColor = Color.Lerp(MusicGold, Color.white, 0.4f);
                        coreColor.a = intensity;
                        pixels[y * width + x] = coreColor;
                    }
                    else
                    {
                        // 글로우 - 골드
                        Color glowColor = MusicGold * intensity * 0.6f;
                        glowColor.a = intensity * 0.5f;
                        pixels[y * width + x] = glowColor;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 레인 플래시 텍스처 생성 (입력 피드백용 - 음악 테마)
        /// </summary>
        public static Texture2D CreateLaneFlashTexture(int width = 64, int height = 256)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float normalizedY = (float)y / height;
                // 아래쪽 밝고 위로 갈수록 페이드
                float fadeY = 1f - (normalizedY * normalizedY);

                for (int x = 0; x < width; x++)
                {
                    float normalizedX = (float)x / width;
                    float xDist = Mathf.Abs(normalizedX - 0.5f) * 2f;
                    // 중앙 밝고 가장자리 어둡게
                    float fadeX = 1f - (xDist * xDist);

                    float intensity = fadeY * fadeX;

                    Color pixelColor = MusicTeal * intensity;
                    pixelColor.a = intensity * 0.8f;

                    pixels[y * width + x] = pixelColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
