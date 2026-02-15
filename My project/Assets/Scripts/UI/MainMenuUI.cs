using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using AIBeat.Core;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.UI
{
    /// <summary>
    /// 메인 메뉴 UI - 센세이셔널 네온 사이버펑크 디자인
    /// 서브타이틀 + 캐치프레이즈 + 아이콘 버튼 + 듀얼 이퀄라이저 + 배경 펄스
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button libraryButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        // 이퀄라이저 바 참조
        private List<Image> eqBarsBottom = new List<Image>();
        private List<Image> eqBarsTop = new List<Image>();
        private Coroutine eqAnimCoroutine;
        private Coroutine breatheCoroutine;

        // 서브타이틀/캐치프레이즈 참조
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI catchphraseText;

        // 연주자 애니메이션 참조
        private Dictionary<string, RectTransform> musicianTransforms = new Dictionary<string, RectTransform>();
        private Coroutine musicianAnimCoroutine;

        private void Start()
        {
            Debug.Log("[MainMenuUI] Start() 호출됨");

            // TMP_Text 생성 전에 한국어 폰트를 글로벌 기본값으로 설정
            var _ = KoreanFontManager.KoreanFont;

            // 오래된 레거시 UI 요소 정리 (씬에 남아있는 기존 버튼들)
            CleanupLegacyUI();

            EnsureEventSystem(); // 터치/클릭 입력을 위한 EventSystem 보장
            EnsureCanvasScaler();
            CreateBackgroundImage();
            AutoSetupReferences();
            Initialize();
            CreateEqualizerBars();
            EnsureSafeArea();
            SetupMusicianSprites();
        }

        /// <summary>
        /// 씬에 남아있는 오래된 레거시 UI 요소 제거
        /// </summary>
        private void CleanupLegacyUI()
        {
            // 오래된 ButtonPanel 제거 (SELECT SONG, QUIT 버튼 포함)
            var legacyNames = new string[] { "ButtonPanel", "SongSelect", "Quit", "Logo" };
            foreach (var name in legacyNames)
            {
                var legacy = transform.Find(name);
                if (legacy != null)
                {
                    Debug.Log($"[MainMenuUI] Removing legacy UI element: {name}");
                    Destroy(legacy.gameObject);
                }
                else
                {
                    Debug.Log($"[MainMenuUI] Legacy element not found: {name}");
                }
            }
        }

        /// <summary>
        /// EventSystem이 씬에 없으면 자동 생성 (터치/클릭 입력 필수)
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                Debug.Log("[MainMenuUI] EventSystem 자동 생성됨");
            }

            // Canvas에 GraphicRaycaster 확인
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[MainMenuUI] GraphicRaycaster 자동 추가됨");
            }
        }

        /// <summary>
        /// MusicianBackground 패널의 자식 Image들에 스프라이트를 런타임에 할당
        /// </summary>
        private void SetupMusicianSprites()
        {
            // MusicianBackground는 SafeAreaPanel 아래에 있을 수 있음
            var musicianBg = transform.Find("MusicianBackground");
            if (musicianBg == null)
                musicianBg = transform.Find("SafeAreaPanel/MusicianBackground");
            if (musicianBg == null)
                musicianBg = FindDeepChild(transform, "MusicianBackground");
            if (musicianBg == null)
            {
                Debug.Log("[MainMenuUI] MusicianBackground not found, skipping sprite setup");
                return;
            }

            // MusicianBackground 자체의 raycastTarget도 비활성화
            var bgImage = musicianBg.GetComponent<Image>();
            if (bgImage != null)
                bgImage.raycastTarget = false;

            // 새 고품질 AI 생성 이미지 사용 (Illustrations 폴더)
            var spriteMap = new (string childName, string spritePath)[]
            {
                ("Drummer", "AIBeat_Design/Illustrations/Cyberpunk_guitarist_female_4k_202602151641"),  // 역동적 점프 포즈
                ("Pianist", "AIBeat_Design/Illustrations/Cyberpunk_keyboardist_anime_4k_202602151645"), // 키보디스트
                ("Guitarist", "AIBeat_Design/Illustrations/Cyberpunk_guitarist_female_4k_202602151642"), // 남성 기타리스트
                ("DJ", "AIBeat_Design/Illustrations/Cyberpunk_dj_male_4k_202602151643")                 // DJ
            };

            musicianTransforms.Clear();
            foreach (var (childName, spritePath) in spriteMap)
            {
                var child = musicianBg.Find(childName);
                if (child == null) continue;

                var image = child.GetComponent<Image>();
                if (image == null) continue;

                var sprite = Resources.Load<Sprite>(spritePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;  // 버튼 클릭 방해 방지
                    Debug.Log($"[MainMenuUI] Loaded sprite for '{childName}'");
                }

                // 애니메이션을 위해 RectTransform 저장
                var rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    musicianTransforms[childName] = rectTransform;
                }
            }

            // 연주자 애니메이션 코루틴 시작
            if (musicianTransforms.Count > 0)
            {
                musicianAnimCoroutine = StartCoroutine(AnimateMusicians());
            }
        }

        /// <summary>
        /// 각 연주자별 개별 애니메이션
        /// - 드러머: 빠른 위아래 진동 (드럼 치는 느낌)
        /// - 피아니스트: 부드러운 좌우 흔들림
        /// - 기타리스트: 기울어지는 회전
        /// - DJ: 펄스 (크기 변화)
        /// </summary>
        private IEnumerator AnimateMusicians()
        {
            // 각 연주자의 초기 위치/회전/크기 저장
            var initialPositions = new Dictionary<string, Vector2>();
            var initialRotations = new Dictionary<string, float>();
            var initialScales = new Dictionary<string, Vector3>();

            foreach (var kvp in musicianTransforms)
            {
                initialPositions[kvp.Key] = kvp.Value.anchoredPosition;
                initialRotations[kvp.Key] = kvp.Value.localEulerAngles.z;
                initialScales[kvp.Key] = kvp.Value.localScale;
            }

            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;

                // === 드러머: 빠른 위아래 진동 (8Hz, ±8px) ===
                if (musicianTransforms.TryGetValue("Drummer", out var drummer))
                {
                    float bobY = Mathf.Sin(time * 16f) * 8f;
                    drummer.anchoredPosition = initialPositions["Drummer"] + new Vector2(0, bobY);
                }

                // === 피아니스트: 부드러운 좌우 흔들림 (1Hz, ±12px) ===
                if (musicianTransforms.TryGetValue("Pianist", out var pianist))
                {
                    float swayX = Mathf.Sin(time * 2f) * 12f;
                    pianist.anchoredPosition = initialPositions["Pianist"] + new Vector2(swayX, 0);
                }

                // === 기타리스트: 기울어지는 회전 (0.8Hz, ±8도) ===
                if (musicianTransforms.TryGetValue("Guitarist", out var guitarist))
                {
                    float rotZ = Mathf.Sin(time * 1.6f) * 8f;
                    guitarist.localEulerAngles = new Vector3(0, 0, initialRotations["Guitarist"] + rotZ);
                }

                // === DJ: 펄스 크기 변화 (2Hz, 0.95~1.05) ===
                if (musicianTransforms.TryGetValue("DJ", out var dj))
                {
                    float scale = 1f + Mathf.Sin(time * 4f) * 0.05f;
                    dj.localScale = initialScales["DJ"] * scale;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 배경 이미지 생성 (Resources/UI/MainMenuBG.jpg 사용, 없으면 Procedural)
        /// </summary>
        private void CreateBackgroundImage()
        {
            var existing = transform.Find("BIT_Background");
            if (existing != null) return;

            var bgGo = new GameObject("BIT_Background");
            bgGo.transform.SetParent(transform, false);
            bgGo.transform.SetAsFirstSibling();

            var rect = bgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = bgGo.AddComponent<Image>();
            img.raycastTarget = false;
            
            // Resources에서 배경 이미지 로드 시도 (BIT.jpg 사용)
            Sprite bgSprite = Resources.Load<Sprite>("UI/BIT");
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                Debug.Log("[MainMenuUI] Loaded BIT.jpg as background");
            }
            else
            {
                // Procedural Generation 호출 (fallback)
                img.sprite = ProceduralImageGenerator.CreateCyberpunkBackground();
                img.type = Image.Type.Sliced;
                Debug.Log("[MainMenuUI] Using procedural background (BIT.jpg not found)");
            }
            
            // 어두운 오버레이 (숨쉬기 효과 대상)
            var overlayGo = new GameObject("DarkOverlay");
            overlayGo.transform.SetParent(transform, false);
            overlayGo.transform.SetSiblingIndex(1);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.raycastTarget = false;
            overlayImg.color = UIColorPalette.BG_DEEP.WithAlpha(0.6f);

            // 배경 숨쉬기(Breathe) 효과 코루틴
            breatheCoroutine = StartCoroutine(AnimateBreathe(overlayImg));
        }

        /// <summary>
        /// 배경 오버레이 투명도를 천천히 순환 (3초 주기)
        /// </summary>
        private IEnumerator AnimateBreathe(Image overlay)
        {
            float phase = 0f;
            while (true)
            {
                phase += Time.unscaledDeltaTime / 3f * Mathf.PI * 2f;
                float alpha = 0.5f + 0.1f * Mathf.Sin(phase);
                if (overlay != null)
                    overlay.color = UIColorPalette.BG_DEEP.WithAlpha(alpha);
                yield return null;
            }
        }

        /// <summary>
        /// 상단 + 하단 듀얼 이퀄라이저 바 (시안→마젠타 그라데이션)
        /// </summary>
        private void CreateEqualizerBars()
        {
            // === 하단 이퀄라이저 (120px, 위로 솟아오름) ===
            CreateSingleEqualizer("EqualizerBottom", eqBarsBottom,
                anchorY: 0f, pivotY: 0f, height: 120f, barCount: 30, flipY: false);

            // === 상단 이퀄라이저 (70px, 아래로 내려옴) ===
            CreateSingleEqualizer("EqualizerTop", eqBarsTop,
                anchorY: 1f, pivotY: 1f, height: 70f, barCount: 20, flipY: true);

            eqAnimCoroutine = StartCoroutine(AnimateEqualizer());
        }

        private void CreateSingleEqualizer(string name, List<Image> barList,
            float anchorY, float pivotY, float height, int barCount, bool flipY)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform, false);

            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, anchorY);
            containerRect.anchorMax = new Vector2(1, anchorY);
            containerRect.pivot = new Vector2(0.5f, pivotY);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, height);

            float barWidth = 1f / barCount;
            for (int i = 0; i < barCount; i++)
            {
                var barGo = new GameObject($"EqBar_{i}");
                barGo.transform.SetParent(container.transform, false);

                var barRect = barGo.AddComponent<RectTransform>();
                if (flipY)
                {
                    // 상단: 위에서 아래로 내려옴
                    barRect.anchorMin = new Vector2(i * barWidth + 0.002f, 0.5f);
                    barRect.anchorMax = new Vector2((i + 1) * barWidth - 0.002f, 1f);
                }
                else
                {
                    barRect.anchorMin = new Vector2(i * barWidth + 0.002f, 0);
                    barRect.anchorMax = new Vector2((i + 1) * barWidth - 0.002f, 0.5f);
                }
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;

                var barImg = barGo.AddComponent<Image>();
                barImg.raycastTarget = false;
                // 시안→마젠타 그라데이션 (사이버펑크)
                float t = (float)i / barCount;
                barImg.color = Color.Lerp(UIColorPalette.NEON_CYAN, UIColorPalette.NEON_MAGENTA, t);
                barList.Add(barImg);
            }
        }

        private IEnumerator AnimateEqualizer()
        {
            int totalBars = eqBarsBottom.Count + eqBarsTop.Count;
            float[] phases = new float[totalBars];
            float[] speeds = new float[totalBars];
            for (int i = 0; i < totalBars; i++)
            {
                phases[i] = Random.Range(0f, Mathf.PI * 2f);
                speeds[i] = Random.Range(1.5f, 4.5f);
            }

            while (true)
            {
                // 하단 바
                for (int i = 0; i < eqBarsBottom.Count; i++)
                {
                    if (eqBarsBottom[i] == null) continue;
                    float h = 0.12f + 0.88f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * speeds[i] + phases[i]));
                    var rect = eqBarsBottom[i].GetComponent<RectTransform>();
                    rect.anchorMax = new Vector2(rect.anchorMax.x, h);
                }
                // 상단 바
                int offset = eqBarsBottom.Count;
                for (int i = 0; i < eqBarsTop.Count; i++)
                {
                    if (eqBarsTop[i] == null) continue;
                    float h = 0.12f + 0.88f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * speeds[offset + i] + phases[offset + i]));
                    var rect = eqBarsTop[i].GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(rect.anchorMin.x, 1f - h);
                }
                yield return null;
            }
        }

        private void AutoSetupReferences()
        {
            // 버튼이 없으면 자동 생성
            playButton = EnsureButton("PlayButton");
            libraryButton = EnsureButton("LibraryButton");
            settingsButton = EnsureButton("SettingsButton");
            exitButton = EnsureButton("ExitButton");

            // 타이틀 텍스트 자동 생성
            if (titleText == null)
            {
                var existingTitle = transform.Find("TitleText");
                if (existingTitle != null)
                {
                    titleText = existingTitle.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var titleGo = new GameObject("TitleText");
                    titleGo.transform.SetParent(transform, false);
                    var titleRect = titleGo.AddComponent<RectTransform>();
                    titleRect.anchorMin = new Vector2(0, 0.65f);
                    titleRect.anchorMax = new Vector2(1, 0.78f);
                    titleRect.offsetMin = Vector2.zero;
                    titleRect.offsetMax = Vector2.zero;
                    titleText = titleGo.AddComponent<TextMeshProUGUI>();
                    titleText.text = "A.I. BEAT";
                    titleText.raycastTarget = false;
                    Debug.Log("[MainMenuUI] TitleText 자동 생성됨");
                }
            }

            // 버전 텍스트 자동 생성
            if (versionText == null)
            {
                var existingVersion = transform.Find("VersionText");
                if (existingVersion != null)
                {
                    versionText = existingVersion.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var versionGo = new GameObject("VersionText");
                    versionGo.transform.SetParent(transform, false);
                    var versionRect = versionGo.AddComponent<RectTransform>();
                    versionRect.anchorMin = new Vector2(0, 0);
                    versionRect.anchorMax = new Vector2(1, 0.04f);
                    versionRect.offsetMin = Vector2.zero;
                    versionRect.offsetMax = Vector2.zero;
                    versionText = versionGo.AddComponent<TextMeshProUGUI>();
                    versionText.raycastTarget = false;
                    Debug.Log("[MainMenuUI] VersionText 자동 생성됨");
                }
            }

            if (settingsPanel == null)
            {
                var existing = transform.Find("SettingsPanel");
                if (existing != null)
                {
                    settingsPanel = existing.gameObject;
                }
                else
                {
                    settingsPanel = new GameObject("SettingsPanel");
                    settingsPanel.transform.SetParent(transform, false);
                    var rect = settingsPanel.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    settingsPanel.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 버튼이 없으면 자동 생성
        /// </summary>
        private Button EnsureButton(string name)
        {
            var existing = transform.Find(name);
            if (existing != null)
                return existing.GetComponent<Button>();

            // 새 버튼 생성
            var btnGO = new GameObject(name, typeof(RectTransform));
            btnGO.transform.SetParent(transform, false);
            btnGO.layer = LayerMask.NameToLayer("UI");

            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.1f, 0.05f, 0.2f, 0.85f);
            img.raycastTarget = true;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            Debug.Log($"[MainMenuUI] 버튼 자동 생성: {name}");
            return btn;
        }

        private void Initialize()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnPlayClicked);
            }
            if (libraryButton != null)
            {
                libraryButton.onClick.RemoveAllListeners();
                libraryButton.onClick.AddListener(OnLibraryClicked);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
            }

            if (versionText != null)
                versionText.text = $"v{Application.version}";

            GameManager.Instance?.ChangeState(GameManager.GameState.MainMenu);

            if (settingsPanel != null)
            {
                if (settingsPanel.GetComponent<SettingsUI>() == null)
                    settingsPanel.AddComponent<SettingsUI>();
                settingsPanel.SetActive(false);
            }

            KoreanFontManager.ApplyFontToAll(gameObject);
            EnsureButtonMobileSize();
            AnimateTitle();
        }

        /// <summary>
        /// 메인 메뉴 버튼 v5 — 대형 고가시성 카드 디자인
        /// - 버튼 높이 증가 (90px)
        /// - 악센트 바 10px 두께
        /// - 메인 텍스트 36pt / 서브 텍스트 14pt
        /// - 큰 화살표 "▶"
        /// </summary>
        private void EnsureButtonMobileSize()
        {
            // 기존 ButtonContainer가 있으면 중복 생성 방지
            var existingContainer = transform.Find("ButtonContainer");
            if (existingContainer != null)
            {
                Debug.Log("[MainMenuUI] ButtonContainer already exists, skipping recreation");
                return;
            }

            var buttonConfigs = new (Button btn, string text, string subText, Color accentColor)[]
            {
                (playButton, "플레이", "PLAY", UIColorPalette.NEON_MAGENTA),
                (libraryButton, "라이브러리", "LIBRARY", UIColorPalette.NEON_CYAN),
                (settingsButton, "설정", "SETTINGS", UIColorPalette.NEON_PURPLE),
                (exitButton, "종료", "EXIT", new Color(1f, 0.35f, 0.35f, 1f))
            };

            // 버튼 컨테이너: 화면 중앙 하단 (좌우 여백 10%)
            var btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(transform, false);
            btnContainer.transform.SetAsLastSibling();
            var btnContainerRect = btnContainer.AddComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.10f, 0.06f);
            btnContainerRect.anchorMax = new Vector2(0.90f, 0.50f);
            btnContainerRect.offsetMin = Vector2.zero;
            btnContainerRect.offsetMax = Vector2.zero;

            var vLayout = btnContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 16;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(0, 0, 0, 0);

            foreach (var cfg in buttonConfigs)
            {
                if (cfg.btn == null) continue;

                cfg.btn.transform.SetParent(btnContainer.transform, false);

                // 버튼 높이 증가 (90px)
                var rect = cfg.btn.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(0, 90f);

                var le = cfg.btn.gameObject.GetComponent<LayoutElement>();
                if (le == null) le = cfg.btn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 90f;
                le.minHeight = 90f;

                // 버튼 배경 (더 밝은 글래스)
                var img = cfg.btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.20f, 0.15f, 0.30f, 0.92f);
                    img.sprite = CreateRoundedRectSprite(16);
                    img.type = Image.Type.Sliced;
                }

                // 기존 컴포넌트 정리
                var existingOutline = cfg.btn.GetComponent<Outline>();
                if (existingOutline != null) Destroy(existingOutline);
                var existingShadows = cfg.btn.GetComponents<Shadow>();
                foreach (var s in existingShadows) Destroy(s);
                var existingHLayout = cfg.btn.GetComponent<HorizontalLayoutGroup>();
                if (existingHLayout != null) Destroy(existingHLayout);

                // 버튼 테두리 (더 밝게)
                var outline = cfg.btn.gameObject.AddComponent<Outline>();
                outline.effectColor = cfg.accentColor.WithAlpha(0.6f);
                outline.effectDistance = new Vector2(2f, -2f);

                // 버튼 상태 색상
                var colors = cfg.btn.colors;
                colors.normalColor = new Color(0.20f, 0.15f, 0.30f, 0.92f);
                colors.highlightedColor = new Color(0.28f, 0.22f, 0.40f, 0.95f);
                colors.pressedColor = cfg.accentColor.WithAlpha(0.6f);
                colors.selectedColor = colors.highlightedColor;
                cfg.btn.colors = colors;

                // 기존 자식 제거
                for (int i = cfg.btn.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(cfg.btn.transform.GetChild(i).gameObject);
                }

                // === 좌측 악센트 바 (10px 두께) ===
                var accentBar = new GameObject("AccentBar");
                accentBar.transform.SetParent(cfg.btn.transform, false);
                var accentRect = accentBar.AddComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0, 0.1f);
                accentRect.anchorMax = new Vector2(0, 0.9f);
                accentRect.pivot = new Vector2(0, 0.5f);
                accentRect.anchoredPosition = new Vector2(8, 0);
                accentRect.sizeDelta = new Vector2(10, 0);  // 10px 두께
                var accentImg = accentBar.AddComponent<Image>();
                accentImg.color = cfg.accentColor;
                accentImg.raycastTarget = false;
                accentImg.sprite = CreateRoundedRectSprite(5);
                accentImg.type = Image.Type.Sliced;

                // === 메인 텍스트 (한국어) - 36pt ===
                var mainTextGo = new GameObject("MainText");
                mainTextGo.transform.SetParent(cfg.btn.transform, false);
                var mainRect = mainTextGo.AddComponent<RectTransform>();
                mainRect.anchorMin = new Vector2(0, 0);
                mainRect.anchorMax = new Vector2(0.8f, 1);
                mainRect.pivot = new Vector2(0, 0.5f);
                mainRect.offsetMin = new Vector2(30, 22);
                mainRect.offsetMax = new Vector2(0, -8);
                var mainTmp = mainTextGo.AddComponent<TextMeshProUGUI>();
                mainTmp.text = cfg.text;
                mainTmp.fontSize = 36;
                mainTmp.fontStyle = FontStyles.Bold;
                mainTmp.color = Color.white;
                mainTmp.alignment = TextAlignmentOptions.BottomLeft;
                mainTmp.overflowMode = TextOverflowModes.Ellipsis;
                mainTmp.raycastTarget = false;

                // === 서브 텍스트 (영어) - 14pt ===
                var subTextGo = new GameObject("SubText");
                subTextGo.transform.SetParent(cfg.btn.transform, false);
                var subRect = subTextGo.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0, 0);
                subRect.anchorMax = new Vector2(0.8f, 1);
                subRect.pivot = new Vector2(0, 0.5f);
                subRect.offsetMin = new Vector2(30, 8);
                subRect.offsetMax = new Vector2(0, -50);
                var subTmp = subTextGo.AddComponent<TextMeshProUGUI>();
                subTmp.text = cfg.subText;
                subTmp.fontSize = 14;
                subTmp.color = cfg.accentColor;
                subTmp.characterSpacing = 6f;
                subTmp.alignment = TextAlignmentOptions.TopLeft;
                subTmp.raycastTarget = false;

                // === 우측 화살표 (▶ 아이콘, 32pt) ===
                var arrowGo = new GameObject("Arrow");
                arrowGo.transform.SetParent(cfg.btn.transform, false);
                var arrowRect = arrowGo.AddComponent<RectTransform>();
                arrowRect.anchorMin = new Vector2(1, 0);
                arrowRect.anchorMax = new Vector2(1, 1);
                arrowRect.pivot = new Vector2(1, 0.5f);
                arrowRect.offsetMin = new Vector2(-60, 0);
                arrowRect.offsetMax = new Vector2(-12, 0);
                var arrowTmp = arrowGo.AddComponent<TextMeshProUGUI>();
                arrowTmp.text = "▶";
                arrowTmp.fontSize = 32;
                arrowTmp.fontStyle = FontStyles.Bold;
                arrowTmp.color = cfg.accentColor;
                arrowTmp.alignment = TextAlignmentOptions.Center;
                arrowTmp.raycastTarget = false;
            }

            Debug.Log("[MainMenuUI] 버튼 UI 리디자인 완료 (v5 - 대형 고가시성)");
        }

        /// <summary>
        /// 라운드 사각형 스프라이트 생성 (런타임)
        /// </summary>
        private Sprite CreateRoundedRectSprite(int cornerRadius)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color fill = Color.white;
            Color clear = Color.clear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 코너 체크
                    bool inCorner = false;
                    float dist = 0;

                    // 좌하단
                    if (x < cornerRadius && y < cornerRadius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
                        inCorner = true;
                    }
                    // 우하단
                    else if (x >= size - cornerRadius && y < cornerRadius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, cornerRadius));
                        inCorner = true;
                    }
                    // 좌상단
                    else if (x < cornerRadius && y >= size - cornerRadius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, size - cornerRadius - 1));
                        inCorner = true;
                    }
                    // 우상단
                    else if (x >= size - cornerRadius && y >= size - cornerRadius)
                    {
                        dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, size - cornerRadius - 1));
                        inCorner = true;
                    }

                    if (inCorner)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist - cornerRadius + 1f));
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, fill);
                    }
                }
            }

            tex.Apply();
            int border = cornerRadius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>
        /// 원형 스프라이트 생성 (아이콘 배지용)
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist - radius + 1.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 타이틀 애니메이션 — "A.I. BEAT" + "INFINITE MIX" + 캐치프레이즈
        /// </summary>
        private void AnimateTitle()
        {
            if (titleText == null) return;

            // 타이틀 위치: 화면 중앙 (캐릭터와 버튼 사이)
            var titleRect = titleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0, 0.56f);
                titleRect.anchorMax = new Vector2(1, 0.68f);
                titleRect.offsetMin = new Vector2(15, 0);
                titleRect.offsetMax = new Vector2(-15, 0);
            }

            titleText.fontSize = 64;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = UIColorPalette.NEON_CYAN_BRIGHT;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.enableAutoSizing = false;

            // 네온 글로우 (가볍게) - 폰트가 있어야 outlineWidth 설정 가능
            var korFontTitle = KoreanFontManager.KoreanFont;
            if (korFontTitle != null) titleText.font = korFontTitle;
            // titleText.outlineWidth = 0.15f; // Dynamic SDF에서 outline 비활성화
            // titleText.outlineColor = new Color32(0, 140, 255, 180);

            var outline = titleText.GetComponent<Outline>();
            if (outline == null)
                outline = titleText.gameObject.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.6f);
            outline.effectDistance = new Vector2(2, -2);

            titleText.transform.localScale = Vector3.zero;
            UIAnimator.ScaleTo(this, titleText.transform, Vector3.one, 0.5f);

            // === 서브타이틀: "INFINITE MIX" ===
            var subGo = new GameObject("SubtitleText");
            subGo.transform.SetParent(transform, false);
            var subRect = subGo.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0.51f);
            subRect.anchorMax = new Vector2(1, 0.56f);
            subRect.offsetMin = new Vector2(20, 0);
            subRect.offsetMax = new Vector2(-20, 0);

            subtitleText = subGo.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "- I N F I N I T E   M I X -";
            subtitleText.fontSize = 22;
            subtitleText.color = UIColorPalette.NEON_MAGENTA;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.characterSpacing = 6f;
            subtitleText.raycastTarget = false;

            // 서브타이틀 글로우 - 폰트가 있어야 outlineWidth 설정 가능
            var korFontSub = KoreanFontManager.KoreanFont;
            if (korFontSub != null) subtitleText.font = korFontSub;
            // subtitleText.outlineWidth = 0.12f; // Dynamic SDF에서 outline 비활성화
            // subtitleText.outlineColor = new Color32(255, 40, 160, 160);

            subGo.transform.localScale = Vector3.zero;
            UIAnimator.ScaleTo(this, subGo.transform, Vector3.one, 0.6f);

            // === 캐치프레이즈: "AI가 만드는 무한 리듬" ===
            var catchGo = new GameObject("CatchphraseText");
            catchGo.transform.SetParent(transform, false);
            var catchRect = catchGo.AddComponent<RectTransform>();
            catchRect.anchorMin = new Vector2(0, 0.465f);
            catchRect.anchorMax = new Vector2(1, 0.51f);
            catchRect.offsetMin = new Vector2(20, 0);
            catchRect.offsetMax = new Vector2(-20, 0);

            catchphraseText = catchGo.AddComponent<TextMeshProUGUI>();
            catchphraseText.text = "AI\uAC00 \uB9CC\uB4DC\uB294 \uBB34\uD55C \uB9AC\uB4EC";
            catchphraseText.fontSize = 20;
            catchphraseText.color = new Color(0.6f, 0.65f, 0.8f, 0f); // 투명에서 시작
            catchphraseText.alignment = TextAlignmentOptions.Center;
            catchphraseText.raycastTarget = false;

            // 페이드인 애니메이션 (1초 딜레이 후)
            StartCoroutine(FadeInCatchphrase());

            // 버전 텍스트 위치 조정 (화면 최하단)
            if (versionText != null)
            {
                var verRect = versionText.GetComponent<RectTransform>();
                if (verRect != null)
                {
                    verRect.anchorMin = new Vector2(0, 0);
                    verRect.anchorMax = new Vector2(1, 0.04f);
                    verRect.offsetMin = Vector2.zero;
                    verRect.offsetMax = Vector2.zero;
                }
                versionText.fontSize = 14;
                versionText.alignment = TextAlignmentOptions.Center;
                versionText.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
            }

            // 한국어 폰트 적용
            var korFont = KoreanFontManager.KoreanFont;
            if (korFont != null)
            {
                subtitleText.font = korFont;
                catchphraseText.font = korFont;
            }
        }

        private IEnumerator FadeInCatchphrase()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            float elapsed = 0f;
            float duration = 1.2f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                if (catchphraseText != null)
                    catchphraseText.color = new Color(0.6f, 0.65f, 0.8f, alpha * 0.8f);
                yield return null;
            }
        }

        private void OnPlayClicked()
        {
            GameManager.Instance?.LoadScene("SongSelectScene");
        }

        private void OnLibraryClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenLibraryOnSongSelect = true;
                GameManager.Instance.LoadScene("SongSelectScene");
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                settingsPanel.transform.localScale = Vector3.zero;
                UIAnimator.ScaleTo(this, settingsPanel, Vector3.one, 0.3f);
            }
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                UIAnimator.ScaleTo(this, settingsPanel, Vector3.zero, 0.2f, () => settingsPanel.SetActive(false));
            }
        }

        private void EnsureCanvasScaler()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void EnsureSafeArea()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<SafeAreaApplier>() == null)
                canvas.gameObject.AddComponent<SafeAreaApplier>();
        }

        /// <summary>
        /// 재귀적으로 자식 Transform을 검색
        /// </summary>
        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void OnDestroy()
        {
            if (eqAnimCoroutine != null) StopCoroutine(eqAnimCoroutine);
            if (breatheCoroutine != null) StopCoroutine(breatheCoroutine);
            if (musicianAnimCoroutine != null) StopCoroutine(musicianAnimCoroutine);
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (libraryButton != null) libraryButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
