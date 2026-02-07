using UnityEngine;
using TMPro;

namespace AIBeat.Core
{
    /// <summary>
    /// 한국어 TMP 폰트를 관리하는 유틸리티.
    /// Resources에서 SDF 폰트 에셋을 로드합니다.
    /// </summary>
    public static class KoreanFontManager
    {
        private static TMP_FontAsset _koreanFont;
        private static bool _initialized;

        public static TMP_FontAsset KoreanFont
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _koreanFont;
            }
        }

        private static void Initialize()
        {
            _initialized = true;

            // 여러 경로에서 SDF 에셋 로드 시도
            string[] paths = {
                "Fonts & Materials/MalgunGothicBold SDF",  // TMP Resources
                "Fonts/MalgunGothicBold SDF",              // Custom Resources
                "MalgunGothicBold SDF",                    // Root Resources
            };

            foreach (var path in paths)
            {
                _koreanFont = Resources.Load<TMP_FontAsset>(path);
                if (_koreanFont != null)
                {
                    Debug.Log($"[KoreanFontManager] 한국어 폰트 로드 성공: {path}");
                    return;
                }
            }

            // LiberationSans SDF에 Fallback으로 등록된 폰트 확인
            var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null && defaultFont.fallbackFontAssetTable != null)
            {
                foreach (var fallback in defaultFont.fallbackFontAssetTable)
                {
                    if (fallback != null && fallback.name.Contains("Malgun"))
                    {
                        _koreanFont = fallback;
                        Debug.Log("[KoreanFontManager] Fallback에서 한국어 폰트 발견");
                        return;
                    }
                }
            }

            // 마지막 수단: TMP_FontAsset을 런타임에 TTF로부터 생성
            CreateFromTTF();
        }

        private static void CreateFromTTF()
        {
            // Resources에서 TTF 폰트 로드 후 Dynamic SDF 에셋 생성
            var ttfFont = Resources.Load<Font>("Fonts/MalgunGothicBold");
            if (ttfFont == null)
            {
                // OS 폰트에서 시도
                string[] fontNames = { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Gulim", "Dotum" };
                foreach (var name in fontNames)
                {
                    ttfFont = Font.CreateDynamicFontFromOSFont(name, 36);
                    if (ttfFont != null)
                    {
                        Debug.Log("[KoreanFontManager] OS 폰트 발견: " + name);
                        break;
                    }
                }
            }
            else
            {
                Debug.Log("[KoreanFontManager] Resources에서 TTF 폰트 로드 성공");
            }

            if (ttfFont == null)
            {
                Debug.LogWarning("[KoreanFontManager] 한국어 폰트를 찾을 수 없습니다.");
                return;
            }

            // TMP_FontAsset.CreateFontAsset은 에디터/빌드 모두 사용 가능 (Unity 2021.2+)
            _koreanFont = TMP_FontAsset.CreateFontAsset(ttfFont);
            if (_koreanFont != null)
            {
                _koreanFont.name = "Korean Dynamic Font";
                _koreanFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                Debug.Log("[KoreanFontManager] Dynamic TMP 폰트 생성 완료");
            }
            else
            {
                Debug.LogWarning("[KoreanFontManager] TMP_FontAsset.CreateFontAsset 실패");
            }
        }

        public static void ApplyFont(TMP_Text textComponent)
        {
            if (textComponent == null) return;
            var font = KoreanFont;
            if (font != null)
                textComponent.font = font;
        }

        public static void ApplyFontToAll(GameObject root)
        {
            if (root == null) return;
            var font = KoreanFont;
            if (font == null) return;

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                t.font = font;
            }
        }
    }
}
