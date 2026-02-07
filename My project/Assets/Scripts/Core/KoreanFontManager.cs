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

            // Resources에서 SDF 에셋 로드 시도
            _koreanFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/MalgunGothicBold SDF");
            if (_koreanFont != null)
            {
                Debug.Log("[KoreanFontManager] 한국어 폰트 에셋 로드 성공");
                return;
            }

            // Fallback: TMP 기본 Resources 경로에서 시도
            _koreanFont = Resources.Load<TMP_FontAsset>("Fonts/MalgunGothicBold SDF");
            if (_koreanFont != null)
            {
                Debug.Log("[KoreanFontManager] 한국어 폰트 에셋 로드 성공 (Fonts/)");
                return;
            }

            // OS 폰트에서 동적 생성 시도
            CreateFromOSFont();
        }

        private static void CreateFromOSFont()
        {
            // Font.CreateDynamicFontFromOSFont은 런타임에서 사용 가능
            string[] fontNames = { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Gulim", "Dotum" };
            Font osFont = null;

            foreach (var name in fontNames)
            {
                osFont = Font.CreateDynamicFontFromOSFont(name, 36);
                if (osFont != null)
                {
                    Debug.Log("[KoreanFontManager] OS 폰트 발견: " + name);
                    break;
                }
            }

            if (osFont == null)
            {
                Debug.LogWarning("[KoreanFontManager] 한국어 OS 폰트를 찾을 수 없습니다.");
                return;
            }

#if UNITY_EDITOR
            // 에디터에서만 CreateFontAsset 사용 가능
            _koreanFont = TMP_FontAsset.CreateFontAsset(osFont);
            if (_koreanFont != null)
            {
                _koreanFont.name = "Korean Dynamic Font";
                _koreanFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                Debug.Log("[KoreanFontManager] 에디터에서 Dynamic TMP 폰트 생성 완료");
            }
#else
            Debug.LogWarning("[KoreanFontManager] 빌드 환경에서는 미리 생성된 SDF 에셋이 필요합니다.");
#endif
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
