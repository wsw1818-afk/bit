using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Core;
using System.Collections;
using System.Collections.Generic;

namespace AIBeat.UI
{
    /// <summary>
    /// 메인 메뉴 UI - BIT.jpg 네온 사이버펑크 디자인
    /// 배경 이미지 + 이퀄라이저 바 + 네온 버튼
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
        private List<Image> eqBars = new List<Image>();
        private Coroutine eqAnimCoroutine;

        private void Start()
        {
            EnsureCanvasScaler();
            CreateBackgroundImage();
            AutoSetupReferences();
            Initialize();
            CreateEqualizerBar();
            EnsureSafeArea();
        }

        /// <summary>
        /// BIT.jpg 배경 이미지 설정
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
            var tex = Resources.Load<Texture2D>("UI/BIT");
            if (tex != null)
            {
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = new Color(1f, 1f, 1f, 0.5f); // 반투명 (UI 가독성)
            }
            else
            {
                img.color = UIColorPalette.BG_DEEP;
            }

            // 배경 위에 어두운 오버레이 (텍스트 가독성)
            var overlayGo = new GameObject("DarkOverlay");
            overlayGo.transform.SetParent(transform, false);
            overlayGo.transform.SetSiblingIndex(1);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0.01f, 0.005f, 0.04f, 0.55f);
        }

        /// <summary>
        /// 하단 이퀄라이저 바 애니메이션 (BIT.jpg 하단 스타일)
        /// </summary>
        private void CreateEqualizerBar()
        {
            var eqContainer = new GameObject("EqualizerBar");
            eqContainer.transform.SetParent(transform, false);

            var containerRect = eqContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, 140);

            int barCount = 30;
            float barWidth = 1f / barCount;

            for (int i = 0; i < barCount; i++)
            {
                var barGo = new GameObject($"EqBar_{i}");
                barGo.transform.SetParent(eqContainer.transform, false);

                var barRect = barGo.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(i * barWidth + 0.002f, 0);
                barRect.anchorMax = new Vector2((i + 1) * barWidth - 0.002f, 0.5f);
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;

                var barImg = barGo.AddComponent<Image>();
                // 오렌지→옐로우 그라데이션 (BIT.jpg 이퀄라이저 색상)
                float t = (float)i / barCount;
                barImg.color = Color.Lerp(UIColorPalette.EQ_ORANGE, UIColorPalette.EQ_YELLOW, t);
                eqBars.Add(barImg);
            }

            // 이퀄라이저 애니메이션 시작
            eqAnimCoroutine = StartCoroutine(AnimateEqualizer());
        }

        private IEnumerator AnimateEqualizer()
        {
            float[] phases = new float[eqBars.Count];
            float[] speeds = new float[eqBars.Count];
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = Random.Range(0f, Mathf.PI * 2f);
                speeds[i] = Random.Range(1.5f, 4f);
            }

            while (true)
            {
                for (int i = 0; i < eqBars.Count; i++)
                {
                    if (eqBars[i] == null) continue;
                    float barRect_anchorMaxY = 0.15f + 0.85f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * speeds[i] + phases[i]));
                    var rect = eqBars[i].GetComponent<RectTransform>();
                    rect.anchorMax = new Vector2(rect.anchorMax.x, barRect_anchorMaxY);
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
        /// BIT.jpg 스타일 네온 버튼 (마젠타/시안/퍼플 그라데이션)
        /// </summary>
        private void EnsureButtonMobileSize()
        {
            var buttonConfigs = new (Button btn, string text, Color glowColor)[]
            {
                (playButton, "플레이", UIColorPalette.NEON_MAGENTA),
                (libraryButton, "라이브러리", UIColorPalette.NEON_CYAN),
                (settingsButton, "설정", UIColorPalette.NEON_PURPLE),
                (exitButton, "종료", UIColorPalette.NEON_ORANGE)
            };

            foreach (var cfg in buttonConfigs)
            {
                if (cfg.btn == null) continue;

                var rect = cfg.btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var size = rect.sizeDelta;
                    size.y = 120f;
                    size.x = Mathf.Max(size.x, 500f);
                    rect.sizeDelta = size;
                }

                // 어두운 반투명 배경
                var img = cfg.btn.GetComponent<Image>();
                if (img != null)
                    img.color = UIColorPalette.BG_BUTTON;

                // 네온 테두리 (각 버튼별 고유 글로우 색상)
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

                // 텍스트 스타일
                var tmp = cfg.btn.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = cfg.text;
                    tmp.fontSize = 48;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = cfg.glowColor;
                }
            }
        }

        private void AnimateTitle()
        {
            if (titleText == null) return;

            titleText.fontSize = 100;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = UIColorPalette.NEON_CYAN_BRIGHT;

            // 강한 네온 글로우
            titleText.outlineWidth = 0.15f;
            titleText.outlineColor = new Color32(0, 140, 255, 200);

            var outline = titleText.GetComponent<Outline>();
            if (outline == null)
                outline = titleText.gameObject.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.8f);
            outline.effectDistance = new Vector2(3, -3);

            titleText.transform.localScale = Vector3.zero;
            UIAnimator.ScaleTo(this, titleText.transform, Vector3.one, 0.5f);
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
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (libraryButton != null) libraryButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
