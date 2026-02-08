using UnityEngine;
using UnityEngine.UI;
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

        private void Start()
        {
            EnsureCanvasScaler();
            CreateBackgroundImage();
            AutoSetupReferences();
            Initialize();
            CreateEqualizerBars();
            EnsureSafeArea();
        }

        /// <summary>
        /// 배경 이미지 생성 (Procedural Cyberpunk)
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
            
            // Procedural Generation 호출
            img.sprite = ProceduralImageGenerator.CreateCyberpunkBackground();
            img.type = Image.Type.Sliced; // 또는 Simple, 텍스처에 따라
            
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
            if (playButton == null)
                playButton = transform.Find("PlayButton")?.GetComponent<Button>();
            if (libraryButton == null)
                libraryButton = transform.Find("LibraryButton")?.GetComponent<Button>();
            if (settingsButton == null)
                settingsButton = transform.Find("SettingsButton")?.GetComponent<Button>();
            if (exitButton == null)
                exitButton = transform.Find("ExitButton")?.GetComponent<Button>();
            if (titleText == null)
                titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            if (versionText == null)
                versionText = transform.Find("VersionText")?.GetComponent<TextMeshProUGUI>();

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
                }
            }
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
        /// 센세이셔널 버튼 리디자인 — 아이콘 + 서브텍스트 + 네온 글래스모피즘
        /// </summary>
        private void EnsureButtonMobileSize()
        {
            var buttonConfigs = new (Button btn, string icon, string text, Color glowColor)[]
            {
                (playButton, ">", "플레이", UIColorPalette.NEON_MAGENTA),
                (libraryButton, "#", "라이브러리", UIColorPalette.NEON_CYAN),
                (settingsButton, "@", "설정", UIColorPalette.NEON_PURPLE),
                (exitButton, "X", "종료", UIColorPalette.NEON_ORANGE)
            };

            // 버튼 컨테이너: 세로 중앙 배치
            var btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(transform, false);
            var btnContainerRect = btnContainer.AddComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.075f, 0.12f);
            btnContainerRect.anchorMax = new Vector2(0.925f, 0.52f);
            btnContainerRect.offsetMin = Vector2.zero;
            btnContainerRect.offsetMax = Vector2.zero;

            var vLayout = btnContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 14;
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

                var rect = cfg.btn.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(0, 80f);

                var le = cfg.btn.gameObject.GetComponent<LayoutElement>();
                if (le == null) le = cfg.btn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 80f;

                // 어두운 반투명 배경
                var img = cfg.btn.GetComponent<Image>();
                if (img != null)
                    img.color = UIColorPalette.BG_BUTTON;

                // 네온 테두리
                var outline = cfg.btn.GetComponent<Outline>();
                if (outline == null)
                    outline = cfg.btn.gameObject.AddComponent<Outline>();
                outline.effectColor = cfg.glowColor.WithAlpha(0.6f);
                outline.effectDistance = new Vector2(2, -2);

                // 버튼 색상 전환
                var colors = cfg.btn.colors;
                colors.normalColor = UIColorPalette.BG_BUTTON;
                colors.highlightedColor = UIColorPalette.STATE_HOVER;
                colors.pressedColor = cfg.glowColor.WithAlpha(0.6f);
                colors.disabledColor = UIColorPalette.STATE_DISABLED;
                cfg.btn.colors = colors;

                // 기존 자식 모두 제거 후 새로 구성
                foreach (Transform child in cfg.btn.transform)
                {
                    Destroy(child.gameObject);
                }

                var hLayout = cfg.btn.gameObject.AddComponent<HorizontalLayoutGroup>();
                hLayout.padding = new RectOffset(20, 20, 0, 0);
                hLayout.spacing = 16;
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childControlWidth = false;
                hLayout.childControlHeight = true;
                hLayout.childForceExpandWidth = false;
                hLayout.childForceExpandHeight = true;

                // 아이콘 텍스트
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(cfg.btn.transform, false);
                var iconLE = iconGo.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 50;
                var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
                iconTmp.text = cfg.icon;
                iconTmp.fontSize = 32;
                iconTmp.color = cfg.glowColor;
                iconTmp.alignment = TextAlignmentOptions.Center;
                iconTmp.fontStyle = FontStyles.Bold;
                iconTmp.raycastTarget = false;

                // 메인 텍스트 (단일 행)
                var mainTextGo = new GameObject("MainText");
                mainTextGo.transform.SetParent(cfg.btn.transform, false);
                var mainLE = mainTextGo.AddComponent<LayoutElement>();
                mainLE.flexibleWidth = 1;
                var mainTmp = mainTextGo.AddComponent<TextMeshProUGUI>();
                mainTmp.text = cfg.text;
                mainTmp.fontSize = 32;
                mainTmp.fontStyle = FontStyles.Bold;
                mainTmp.color = cfg.glowColor;
                mainTmp.alignment = TextAlignmentOptions.MidlineLeft;
                mainTmp.raycastTarget = false;
            }
        }

        /// <summary>
        /// 타이틀 애니메이션 — "A.I. BEAT" + "INFINITE MIX" + 캐치프레이즈
        /// </summary>
        private void AnimateTitle()
        {
            if (titleText == null) return;

            // 타이틀 위치 조정 (화면 상단 65~78% 영역)
            var titleRect = titleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0, 0.65f);
                titleRect.anchorMax = new Vector2(1, 0.78f);
                titleRect.offsetMin = new Vector2(10, 0);
                titleRect.offsetMax = new Vector2(-10, 0);
            }

            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = UIColorPalette.NEON_CYAN_BRIGHT;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.enableAutoSizing = false;

            // 네온 글로우 (가볍게)
            titleText.outlineWidth = 0.15f;
            titleText.outlineColor = new Color32(0, 140, 255, 180);

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
            subRect.anchorMin = new Vector2(0, 0.59f);
            subRect.anchorMax = new Vector2(1, 0.65f);
            subRect.offsetMin = new Vector2(20, 0);
            subRect.offsetMax = new Vector2(-20, 0);

            subtitleText = subGo.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "- I N F I N I T E   M I X -";
            subtitleText.fontSize = 28;
            subtitleText.color = UIColorPalette.NEON_MAGENTA;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.characterSpacing = 6f;
            subtitleText.raycastTarget = false;

            // 서브타이틀 글로우
            subtitleText.outlineWidth = 0.12f;
            subtitleText.outlineColor = new Color32(255, 40, 160, 160);

            subGo.transform.localScale = Vector3.zero;
            UIAnimator.ScaleTo(this, subGo.transform, Vector3.one, 0.6f);

            // === 캐치프레이즈: "AI가 만드는 무한 리듬" ===
            var catchGo = new GameObject("CatchphraseText");
            catchGo.transform.SetParent(transform, false);
            var catchRect = catchGo.AddComponent<RectTransform>();
            catchRect.anchorMin = new Vector2(0, 0.54f);
            catchRect.anchorMax = new Vector2(1, 0.59f);
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
            GameManager.Instance?.LoadScene("SongSelect");
        }

        private void OnLibraryClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenLibraryOnSongSelect = true;
                GameManager.Instance.LoadScene("SongSelect");
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

        private void OnDestroy()
        {
            if (eqAnimCoroutine != null) StopCoroutine(eqAnimCoroutine);
            if (breatheCoroutine != null) StopCoroutine(breatheCoroutine);
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (libraryButton != null) libraryButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
