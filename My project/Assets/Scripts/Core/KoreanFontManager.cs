using UnityEngine;
using TMPro;

namespace AIBeat.Core
{
    /// <summary>
    /// 한국어 TMP 폰트를 관리하는 유틸리티.
    /// TTF로부터 Dynamic SDF 폰트를 런타임 생성하여 확실한 한국어 렌더링 보장.
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

            // APK 빌드에서는 기존 SDF 에셋의 글리프가 m_ClearDynamicDataOnBuild=1로 삭제됨
            // → 항상 TTF에서 런타임 Dynamic SDF를 새로 생성하는 것이 가장 확실
#if UNITY_EDITOR
            // 에디터에서만 기존 SDF 에셋 시도 (에디터는 동적 글리프 생성 가능)
            string[] paths = {
                "Fonts & Materials/MalgunGothicBold SDF",
                "Fonts/MalgunGothicBold SDF",
                "MalgunGothicBold SDF",
            };

            foreach (var path in paths)
            {
                var loaded = Resources.Load<TMP_FontAsset>(path);
                if (loaded != null)
                {
                    if (loaded.atlasPopulationMode == AtlasPopulationMode.Static
                        && loaded.characterTable != null
                        && loaded.characterTable.Count > 0)
                    {
                        _koreanFont = loaded;
                        Debug.Log($"[KoreanFontManager] Static SDF 폰트 로드 성공: {path} (글리프 {loaded.characterTable.Count}개)");
                        return;
                    }

                    if (loaded.atlasPopulationMode == AtlasPopulationMode.Dynamic
                        && loaded.sourceFontFile != null)
                    {
                        bool canAddChars = loaded.TryAddCharacters("가나다라마바사");
                        if (canAddChars)
                        {
                            _koreanFont = loaded;
                            Debug.Log($"[KoreanFontManager] Dynamic SDF 폰트 사용 (소스 폰트 연결됨): {path}");
                            return;
                        }
                    }
                }
            }
#endif

            // TTF로부터 Dynamic SDF 생성 (APK에서 가장 확실한 방법)
            CreateFromTTF();
        }

        private static void CreateFromTTF()
        {
            // Resources에서 TTF 폰트 로드
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
