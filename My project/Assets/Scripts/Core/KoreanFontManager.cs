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

            // 에디터/런타임 공통: 항상 TTF에서 새로 생성 (기존 SDF 에셋의 글리프 메트릭 문제 방지)

            // APK + 에디터 폴백: TTF에서 런타임 Dynamic SDF 생성
            CreateFromTTF();

            // 글로벌 기본 폰트로 설정 (이후 생성되는 모든 TMP_Text에 자동 적용)
            if (_koreanFont != null)
                SetAsGlobalDefault(_koreanFont);
        }

        /// <summary>
        /// 한국어 폰트를 TMP 글로벌 기본 폰트 + fallback으로 설정.
        /// 이렇게 하면 이후 생성되는 모든 TMP_Text가 한국어를 렌더링 가능.
        /// </summary>
        private static void SetAsGlobalDefault(TMP_FontAsset koreanFont)
        {
            // 1) TMP_Settings의 기본 폰트를 한국어 폰트로 교체
            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                // TMP_Settings.defaultFontAsset은 읽기전용 프로퍼티이므로
                // 리플렉션으로 내부 필드를 직접 설정
                var field = typeof(TMP_Settings).GetField("m_defaultFontAsset",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(settings, koreanFont);
                    Debug.Log("[KoreanFontManager] TMP_Settings.defaultFontAsset → 한국어 폰트로 변경");
                }
            }

            // 2) 기존 LiberationSans SDF의 fallback 리스트에 한국어 폰트 추가
            //    (이미 LiberationSans SDF를 쓰고 있는 TMP_Text도 한국어 렌더링 가능)
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null && defaultFont != koreanFont)
            {
                if (defaultFont.fallbackFontAssetTable == null)
                    defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

                if (!defaultFont.fallbackFontAssetTable.Contains(koreanFont))
                {
                    defaultFont.fallbackFontAssetTable.Insert(0, koreanFont);
                    Debug.Log($"[KoreanFontManager] '{defaultFont.name}'의 fallback에 한국어 폰트 추가");
                }
            }

            // 3) TMP_Settings의 글로벌 fallback 리스트에도 추가
            var globalFallbacks = TMP_Settings.fallbackFontAssets;
            if (globalFallbacks != null)
            {
                if (!globalFallbacks.Contains(koreanFont))
                {
                    globalFallbacks.Insert(0, koreanFont);
                    Debug.Log("[KoreanFontManager] TMP_Settings 글로벌 fallback에 한국어 폰트 추가");
                }
            }
        }

        private static void CreateFromTTF()
        {
            // 1단계: Resources에서 TTF 폰트 로드
            var ttfFont = Resources.Load<Font>("Fonts/MalgunGothicBold");
            if (ttfFont != null)
            {
                Debug.Log("[KoreanFontManager] Resources TTF 로드 성공");
                // 충분한 아틀라스 크기로 생성 (글리프 겹침/깨짐 방지)
                var created = TMP_FontAsset.CreateFontAsset(
                    ttfFont, 44, 5,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    2048, 2048);
                if (created != null)
                {
                    created.name = "Korean Dynamic Font (Resources)";
                    // Static 모드: 글리프 미리 생성 후 고정 → TMP가 자체 Material 재생성 안 함
                    created.atlasPopulationMode = AtlasPopulationMode.Static;

                    // 한국어 + 영문/숫자 글리프 미리 포함
                    created.TryAddCharacters("가나다라마바사아자차카타파하걸고곡곧공과관광교구국권귀기길김나남너노녹높뇨눈늘다달담당대더도독동두든등디딩따때려로록롭롯료루룹류름릇리릭링마막많말맞매맵먹메명모목못무문물므미믹밀바박밖반발밝밟방배백번벌범별보복본볼봄봐부북분불붙비빠빨사산상새색생서석선설성세소속손솔송수순술쉬스슬슴습시식신실심싱쓰아악안않알앞야약양어없에여역연열영예오온완왜요우운울움워원위음이인일임입자장재저적전절점정제조족존종주줄중지직진짜째초촉총최추출충취치카타터파팔패편평포폭표하학한할합해핵행향허현호혹홀홈화확환활황회효후힘");
                    created.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
                    created.TryAddCharacters("0123456789 .·,!?%()+-:/·★♪▶◀■□▼▲←→↑↓_@#$&'\"~^`…·•");

                    // 아틀라스 텍스처 GPU 업로드 + Material._MainTex 직접 연결
                    int texCount = created.atlasTextures?.Length ?? 0;
                    if (texCount > 0 && created.atlasTextures[0] != null)
                    {
                        var atlasTex = created.atlasTextures[0];
                        atlasTex.Apply(false, false);
                        var mat = created.material;
                        if (mat != null)
                            mat.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
                    }

                    Debug.Log($"[KoreanFontManager] atlasTextures={texCount}, glyphTable={created.glyphTable.Count}, charTable={created.characterTable.Count}");

                    if (created.characterTable.Count > 0)
                    {
                        _koreanFont = created;
                        Debug.Log($"[KoreanFontManager] Resources TTF → Static SDF 성공 (글리프 수: {created.characterTable.Count})");
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
                        var created = TMP_FontAsset.CreateFontAsset(
                            osFont, 44, 5,
                            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                            2048, 2048);
                        if (created != null)
                        {
                            created.name = "Korean Dynamic Font (OS)";
                            created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                            created.TryAddCharacters("가나다라마바사아자차카타파하");
                            created.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
                            created.TryAddCharacters("0123456789 .·,!?%()+-:/★♪▶◀■□");
                            if (created.characterTable.Count > 0)
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
                    var created = TMP_FontAsset.CreateFontAsset(
                        osFont, 44, 5,
                        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                        2048, 2048);
                    if (created != null)
                    {
                        created.name = $"Korean Dynamic Font ({name})";
                        created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                        created.TryAddCharacters("가나다라마바사아자차카타파하");
                        created.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
                        created.TryAddCharacters("0123456789 .·,!?%()+-:/★♪▶◀■□");
                        if (created.characterTable.Count > 0)
                        {
                            _koreanFont = created;
                            Debug.Log($"[KoreanFontManager] OS 폰트 이름 → Dynamic SDF 성공: {name}");
                            return;
                        }
                    }
                }
            }

            // 최종 폴백: Resources TTF로 생성 후 Dynamic으로 글리프 요청
            ttfFont = Resources.Load<Font>("Fonts/MalgunGothicBold");
            if (ttfFont != null)
            {
                _koreanFont = TMP_FontAsset.CreateFontAsset(
                    ttfFont, 44, 5,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    4096, 4096);
                if (_koreanFont != null)
                {
                    _koreanFont.name = "Korean Dynamic Font (Fallback)";
                    _koreanFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                    // Dynamic 모드에서 TryAddCharacters로 글리프 채우기
                    _koreanFont.TryAddCharacters("가나다라마바사아자차카타파하걸고곡곧공과관광교구국권귀기길김나남너노녹높뇨눈늘다달담당대더도독동두든등디딩따때려로록롭롯료루룹류름릇리릭링마막많말맞매맵먹메명모목못무문물므미믹밀바박밖반발밝밟방배백번벌범별보복본볼봄봐부북분불붙비빠빨사산상새색생서석선설성세소속손솔송수순술쉬스슬슴습시식신실심싱쓰아악안않알앞야약양어없에여역연열영예오온완왜요우운울움워원위음이인일임입자장재저적전절점정제조족존종주줄중지직진짜째초촉총최추출충취치카타터파팔패편평포폭표하학한할합해핵행향허현호혹홀홈화확환활황회효후힘");
                    _koreanFont.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
                    _koreanFont.TryAddCharacters("0123456789 .·,!?%()+-:/·★♪▶◀■□▼▲←→↑↓_@#$&'\"~^`…·•");

                    int texCount = _koreanFont.atlasTextures?.Length ?? 0;
                    if (texCount > 0 && _koreanFont.atlasTextures[0] != null)
                    {
                        var atlasTex = _koreanFont.atlasTextures[0];
                        atlasTex.Apply(false, false);
                        var mat = _koreanFont.material;
                        if (mat != null)
                            mat.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
                    }

                    Debug.LogWarning($"[KoreanFontManager] 최종 폴백 생성 완료 (charCount={_koreanFont.characterTable?.Count})");
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
