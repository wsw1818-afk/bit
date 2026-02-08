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

#if UNITY_EDITOR
            // 에디터: 기존 Dynamic SDF 에셋 시도 (에디터는 런타임 글리프 생성 가능)
            string[] paths = {
                "Fonts & Materials/MalgunGothicBold SDF",
                "Fonts/MalgunGothicBold SDF",
                "MalgunGothicBold SDF",
            };

            foreach (var path in paths)
            {
                var loaded = Resources.Load<TMP_FontAsset>(path);
                if (loaded != null && loaded.atlasPopulationMode == AtlasPopulationMode.Dynamic
                    && loaded.sourceFontFile != null)
                {
                    if (loaded.TryAddCharacters("가나다라마바사"))
                    {
                        _koreanFont = loaded;
                        Debug.Log($"[KoreanFontManager] Editor: Dynamic SDF 사용: {path}");
                        return;
                    }
                }
            }
#endif

            // APK + 에디터 폴백: TTF에서 런타임 Dynamic SDF 생성
            CreateFromTTF();
        }

        private static void CreateFromTTF()
        {
            // 1단계: Resources에서 TTF 폰트 로드
            var ttfFont = Resources.Load<Font>("Fonts/MalgunGothicBold");
            if (ttfFont != null)
            {
                Debug.Log("[KoreanFontManager] Resources TTF 로드 성공");
                var created = TMP_FontAsset.CreateFontAsset(ttfFont);
                if (created != null)
                {
                    created.name = "Korean Dynamic Font (Resources)";
                    created.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                    // 실제 글리프 렌더링 가능한지 검증
                    if (created.TryAddCharacters("가나다"))
                    {
                        _koreanFont = created;
                        Debug.Log("[KoreanFontManager] Resources TTF → Dynamic SDF 성공");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("[KoreanFontManager] Resources TTF → TryAddCharacters 실패");
                    }
                }
                else
                {
                    Debug.LogWarning("[KoreanFontManager] Resources TTF → CreateFontAsset 실패");
                }
            }
            else
            {
                Debug.LogWarning("[KoreanFontManager] Resources TTF 로드 실패");
            }

            // 2단계: Android OS 폰트 파일에서 직접 로드
#if UNITY_ANDROID && !UNITY_EDITOR
            string[] androidFontPaths = {
                "/system/fonts/NotoSansCJK-Regular.ttc",
                "/system/fonts/NotoSansKR-Regular.otf",
                "/system/fonts/DroidSansFallback.ttf",
                "/system/fonts/DroidSans.ttf",
            };

            foreach (var fontPath in androidFontPaths)
            {
                if (System.IO.File.Exists(fontPath))
                {
                    Debug.Log($"[KoreanFontManager] Android OS 폰트 발견: {fontPath}");
                    var osFont = new Font(fontPath);
                    if (osFont != null)
                    {
                        var created = TMP_FontAsset.CreateFontAsset(osFont);
                        if (created != null)
                        {
                            created.name = "Korean Dynamic Font (OS)";
                            created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                            if (created.TryAddCharacters("가나다"))
                            {
                                _koreanFont = created;
                                Debug.Log($"[KoreanFontManager] Android OS 폰트 → Dynamic SDF 성공: {fontPath}");
                                return;
                            }
                        }
                    }
                }
            }
#endif

            // 3단계: OS 폰트 이름으로 시도
            string[] fontNames = { "NotoSansCJK", "Malgun Gothic", "NanumGothic", "Gulim", "Dotum", "sans-serif" };
            foreach (var name in fontNames)
            {
                var osFont = Font.CreateDynamicFontFromOSFont(name, 44);
                if (osFont != null)
                {
                    var created = TMP_FontAsset.CreateFontAsset(osFont);
                    if (created != null)
                    {
                        created.name = $"Korean Dynamic Font ({name})";
                        created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                        if (created.TryAddCharacters("가나다"))
                        {
                            _koreanFont = created;
                            Debug.Log($"[KoreanFontManager] OS 폰트 이름 → Dynamic SDF 성공: {name}");
                            return;
                        }
                    }
                }
            }

            // 최종 폴백: 검증 없이라도 Resources TTF로 생성
            ttfFont = Resources.Load<Font>("Fonts/MalgunGothicBold");
            if (ttfFont != null)
            {
                _koreanFont = TMP_FontAsset.CreateFontAsset(ttfFont);
                if (_koreanFont != null)
                {
                    _koreanFont.name = "Korean Dynamic Font (Fallback)";
                    _koreanFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    Debug.LogWarning("[KoreanFontManager] 최종 폴백: 검증 없이 생성 (글리프 렌더링 불확실)");
                }
            }

            if (_koreanFont == null)
                Debug.LogError("[KoreanFontManager] 한국어 폰트를 생성할 수 없습니다!");
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
