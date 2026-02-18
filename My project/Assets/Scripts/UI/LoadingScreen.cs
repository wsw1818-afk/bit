using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using AIBeat.Core;
using AIBeat.Data;
using AIBeat.Utils;

namespace AIBeat.UI
{
    /// <summary>
    /// 씬 전환 시 표시되는 로딩 화면
    /// - 배경 이미지 (Gameplay_BG)
    /// - 곡 정보 (제목, 아티스트, BPM, 난이도)
    /// - 애니메이션 이퀄라이저 바
    /// - 글로우 프로그레스 바 + 퍼센트
    /// - 게임 팁 랜덤 표시
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        // 게임 팁 배열 (한국어)
        private static readonly string[] TIPS = new string[]
        {
            "PERFECT 판정은 \u00b150ms 이내입니다",
            "콤보를 유지하면 점수 보너스!",
            "노트가 판정선에 닿을 때 터치하세요",
            "롱노트를 끝까지 누르고 있으세요",
            "리듬에 집중하면 높은 점수를 얻을 수 있어요",
            "각 레인에 떨어지는 노트를 놓치지 마세요",
            "BPM이 높을수록 노트가 빨라집니다",
            "MISS 없이 플레이하면 S+ 랭크!"
        };

        // UI 요소
        private GameObject rootPanel;
        private Image backgroundImage;
        private Image progressBar;
        private Image progressGlow;
        private TextMeshProUGUI songTitleText;
        private TextMeshProUGUI songArtistText;
        private TextMeshProUGUI songInfoText;
        private TextMeshProUGUI loadingText;
        private TextMeshProUGUI percentText;
        private TextMeshProUGUI tipText;

        // 이퀄라이저 바
        private Image[] eqBars;
        private const int EQ_BAR_COUNT = 20;
        private float[] eqPhases;
        private float[] eqSpeeds;

        // 애니메이션 코루틴
        private Coroutine animCoroutine;
        private Coroutine eqCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildUI();
                rootPanel.SetActive(false);
                Debug.Log("[LoadingScreen] 초기화 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 로딩 화면 UI 구조 빌드
        /// </summary>
        private void BuildUI()
        {
            // Canvas 설정
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 998; // FadeOverlay(9999) 바로 아래
            }

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // === 루트 패널 ===
            rootPanel = new GameObject("LoadingPanel");
            rootPanel.transform.SetParent(transform, false);
            var rootRect = rootPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // === 배경 이미지 ===
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(rootPanel.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            backgroundImage = bgGo.AddComponent<Image>();
            backgroundImage.raycastTarget = false;

            // Gameplay_BG 로드 시도
            Sprite bgSprite = ResourceHelper.LoadSpriteFromResources("AIBeat_Design/UI/Backgrounds/Gameplay_BG");
            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.preserveAspect = false;
                backgroundImage.color = new Color(0.3f, 0.3f, 0.35f, 1f); // 어둡게
            }
            else
            {
                backgroundImage.color = UIColorPalette.BG_DEEP;
            }

            // === 어두운 오버레이 (텍스트 가독성 향상) ===
            var overlayGo = new GameObject("DarkOverlay");
            overlayGo.transform.SetParent(rootPanel.transform, false);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0.05f, 0.6f);
            overlayImg.raycastTarget = false;

            // === 상단 이퀄라이저 바 ===
            BuildEqualizer();

            // === 곡 정보 영역 (화면 중앙 상단) ===
            BuildSongInfo();

            // === "LOADING" + 프로그레스 바 (화면 중앙 하단) ===
            BuildProgressSection();

            // === 팁 텍스트 (하단) ===
            BuildTipSection();

            // 한국어 폰트 적용
            KoreanFontManager.ApplyFontToAll(rootPanel);
        }

        /// <summary>
        /// 상단 이퀄라이저 바 (로딩 애니메이션)
        /// </summary>
        private void BuildEqualizer()
        {
            var eqContainer = new GameObject("Equalizer");
            eqContainer.transform.SetParent(rootPanel.transform, false);
            var containerRect = eqContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.78f);
            containerRect.anchorMax = new Vector2(0.95f, 0.88f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            eqBars = new Image[EQ_BAR_COUNT];
            eqPhases = new float[EQ_BAR_COUNT];
            eqSpeeds = new float[EQ_BAR_COUNT];
            float barWidth = 1f / EQ_BAR_COUNT;

            for (int i = 0; i < EQ_BAR_COUNT; i++)
            {
                var barGo = new GameObject($"EqBar_{i}");
                barGo.transform.SetParent(eqContainer.transform, false);
                var barRect = barGo.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(i * barWidth + 0.004f, 0f);
                barRect.anchorMax = new Vector2((i + 1) * barWidth - 0.004f, 0.3f);
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;

                var barImg = barGo.AddComponent<Image>();
                barImg.raycastTarget = false;

                // 그라데이션 색상: 보라 → 시안 → 골드
                float t = (float)i / EQ_BAR_COUNT;
                Color barColor;
                if (t < 0.5f)
                    barColor = Color.Lerp(UIColorPalette.NEON_PURPLE, UIColorPalette.NEON_CYAN, t * 2f);
                else
                    barColor = Color.Lerp(UIColorPalette.NEON_CYAN, UIColorPalette.NEON_GOLD, (t - 0.5f) * 2f);
                barImg.color = barColor;

                eqBars[i] = barImg;
                eqPhases[i] = Random.Range(0f, Mathf.PI * 2f);
                eqSpeeds[i] = Random.Range(2f, 5f);
            }
        }

        /// <summary>
        /// 곡 정보 (제목, 아티스트, BPM/난이도)
        /// </summary>
        private void BuildSongInfo()
        {
            // 곡 제목 (큰 글씨, 골드)
            var titleGo = new GameObject("SongTitle");
            titleGo.transform.SetParent(rootPanel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, 0.58f);
            titleRect.anchorMax = new Vector2(0.92f, 0.68f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            songTitleText = titleGo.AddComponent<TextMeshProUGUI>();
            songTitleText.text = "";
            songTitleText.fontSize = 48;
            songTitleText.color = UIColorPalette.NEON_GOLD;
            songTitleText.alignment = TextAlignmentOptions.Center;
            songTitleText.fontStyle = FontStyles.Bold;
            songTitleText.textWrappingMode = TextWrappingModes.Normal;
            songTitleText.overflowMode = TextOverflowModes.Ellipsis;

            // 아티스트
            var artistGo = new GameObject("SongArtist");
            artistGo.transform.SetParent(rootPanel.transform, false);
            var artistRect = artistGo.AddComponent<RectTransform>();
            artistRect.anchorMin = new Vector2(0.1f, 0.53f);
            artistRect.anchorMax = new Vector2(0.9f, 0.59f);
            artistRect.offsetMin = Vector2.zero;
            artistRect.offsetMax = Vector2.zero;

            songArtistText = artistGo.AddComponent<TextMeshProUGUI>();
            songArtistText.text = "";
            songArtistText.fontSize = 28;
            songArtistText.color = UIColorPalette.TEXT_GRAY;
            songArtistText.alignment = TextAlignmentOptions.Center;

            // BPM / 난이도 정보
            var infoGo = new GameObject("SongInfo");
            infoGo.transform.SetParent(rootPanel.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.15f, 0.47f);
            infoRect.anchorMax = new Vector2(0.85f, 0.53f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            songInfoText = infoGo.AddComponent<TextMeshProUGUI>();
            songInfoText.text = "";
            songInfoText.fontSize = 24;
            songInfoText.color = UIColorPalette.NEON_CYAN;
            songInfoText.alignment = TextAlignmentOptions.Center;
            songInfoText.richText = true;
        }

        /// <summary>
        /// 프로그레스 바 + 로딩 텍스트
        /// </summary>
        private void BuildProgressSection()
        {
            // "LOADING..." 텍스트
            var loadingGo = new GameObject("LoadingText");
            loadingGo.transform.SetParent(rootPanel.transform, false);
            var loadingRect = loadingGo.AddComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.1f, 0.35f);
            loadingRect.anchorMax = new Vector2(0.9f, 0.42f);
            loadingRect.offsetMin = Vector2.zero;
            loadingRect.offsetMax = Vector2.zero;

            loadingText = loadingGo.AddComponent<TextMeshProUGUI>();
            loadingText.text = "LOADING";
            loadingText.fontSize = 26;
            loadingText.color = UIColorPalette.NEON_CYAN;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.fontStyle = FontStyles.Bold;
            loadingText.characterSpacing = 8f;

            // 프로그레스 바 배경 (둥근 느낌)
            var progressBgGo = new GameObject("ProgressBg");
            progressBgGo.transform.SetParent(rootPanel.transform, false);
            var progressBgRect = progressBgGo.AddComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.1f, 0.30f);
            progressBgRect.anchorMax = new Vector2(0.9f, 0.33f);
            progressBgRect.offsetMin = Vector2.zero;
            progressBgRect.offsetMax = Vector2.zero;

            var bgImg = progressBgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);
            bgImg.raycastTarget = false;

            // 프로그레스 바 테두리 (시안)
            var borderOutline = progressBgGo.AddComponent<Outline>();
            borderOutline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.5f);
            borderOutline.effectDistance = new Vector2(1, -1);

            // 프로그레스 바 (채우기)
            var progressGo = new GameObject("ProgressBar");
            progressGo.transform.SetParent(progressBgGo.transform, false);
            var progressRect = progressGo.AddComponent<RectTransform>();
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = new Vector2(0f, 1f);
            progressRect.offsetMin = new Vector2(2, 2);
            progressRect.offsetMax = new Vector2(0, -2);

            progressBar = progressGo.AddComponent<Image>();
            progressBar.color = UIColorPalette.NEON_CYAN;
            progressBar.raycastTarget = false;

            // 프로그레스 글로우 (바 끝에 밝은 효과)
            var glowGo = new GameObject("ProgressGlow");
            glowGo.transform.SetParent(progressGo.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(1f, 0f);
            glowRect.anchorMax = new Vector2(1f, 1f);
            glowRect.pivot = new Vector2(1f, 0.5f);
            glowRect.sizeDelta = new Vector2(30, 0);
            glowRect.anchoredPosition = Vector2.zero;

            progressGlow = glowGo.AddComponent<Image>();
            progressGlow.color = new Color(0.5f, 1f, 1f, 0.6f);
            progressGlow.raycastTarget = false;

            // 퍼센트 텍스트
            var percentGo = new GameObject("PercentText");
            percentGo.transform.SetParent(rootPanel.transform, false);
            var percentRect = percentGo.AddComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0.3f, 0.25f);
            percentRect.anchorMax = new Vector2(0.7f, 0.30f);
            percentRect.offsetMin = Vector2.zero;
            percentRect.offsetMax = Vector2.zero;

            percentText = percentGo.AddComponent<TextMeshProUGUI>();
            percentText.text = "0%";
            percentText.fontSize = 22;
            percentText.color = UIColorPalette.NEON_GOLD;
            percentText.alignment = TextAlignmentOptions.Center;
            percentText.fontStyle = FontStyles.Bold;
        }

        /// <summary>
        /// 팁 텍스트 (하단)
        /// </summary>
        private void BuildTipSection()
        {
            // 구분선
            var lineGo = new GameObject("TipLine");
            lineGo.transform.SetParent(rootPanel.transform, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.2f, 0.18f);
            lineRect.anchorMax = new Vector2(0.8f, 0.182f);
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;

            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = UIColorPalette.NEON_PURPLE.WithAlpha(0.4f);
            lineImg.raycastTarget = false;

            // 팁 텍스트
            var tipGo = new GameObject("TipText");
            tipGo.transform.SetParent(rootPanel.transform, false);
            var tipRect = tipGo.AddComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.08f, 0.10f);
            tipRect.anchorMax = new Vector2(0.92f, 0.18f);
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;

            tipText = tipGo.AddComponent<TextMeshProUGUI>();
            tipText.text = "";
            tipText.fontSize = 22;
            tipText.color = UIColorPalette.TEXT_GRAY;
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.fontStyle = FontStyles.Italic;
            tipText.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>
        /// 곡 정보를 설정하고 로딩 화면 표시
        /// </summary>
        public void SetSongInfo(SongData songData)
        {
            if (songData == null) return;
            Debug.Log($"[LoadingScreen] SetSongInfo: {songData.Title}");

            if (songTitleText != null)
                songTitleText.text = songData.Title ?? "Unknown";

            if (songArtistText != null)
            {
                string artist = !string.IsNullOrEmpty(songData.Artist) && songData.Artist != "Unknown"
                    ? songData.Artist : "";
                songArtistText.text = artist;
                songArtistText.gameObject.SetActive(!string.IsNullOrEmpty(artist));
            }

            if (songInfoText != null)
            {
                string info = "";
                if (songData.BPM > 0)
                    info += $"<color=#{ColorUtility.ToHtmlStringRGB(UIColorPalette.NEON_GOLD)}>{songData.BPM} BPM</color>";
                if (songData.Difficulty > 0)
                {
                    if (!string.IsNullOrEmpty(info)) info += "  ·  ";
                    info += $"Lv.{songData.Difficulty}";
                }
                if (!string.IsNullOrEmpty(songData.Genre))
                {
                    if (!string.IsNullOrEmpty(info)) info += "  ·  ";
                    info += songData.Genre;
                }
                songInfoText.text = info;
            }
        }

        /// <summary>
        /// 로딩 화면 표시 + 애니메이션 시작
        /// </summary>
        public void Show()
        {
            if (rootPanel == null) return;
            rootPanel.SetActive(true);
            Debug.Log("[LoadingScreen] Show() - 로딩 화면 표시");

            // 랜덤 팁
            if (tipText != null)
            {
                string tip = TIPS[Random.Range(0, TIPS.Length)];
                tipText.text = $"TIP: {tip}";
            }

            UpdateProgress(0f);

            // 애니메이션 시작
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(LoadingAnimation());

            if (eqCoroutine != null) StopCoroutine(eqCoroutine);
            eqCoroutine = StartCoroutine(EqualizerAnimation());
        }

        /// <summary>
        /// 로딩 화면 숨기기
        /// </summary>
        public void Hide()
        {
            Debug.Log("[LoadingScreen] Hide() - 로딩 화면 숨김");
            if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
            if (eqCoroutine != null) { StopCoroutine(eqCoroutine); eqCoroutine = null; }
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        /// <summary>
        /// 씬을 로딩 화면과 함께 비동기로 로드
        /// </summary>
        public void LoadSceneWithScreen(string sceneName)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            Show();
            yield return null; // 1프레임 대기 (UI 표시)

            // 비동기 씬 로드
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            if (asyncLoad == null)
            {
                Debug.LogError($"[LoadingScreen] Scene load failed: {sceneName}");
                Hide();
                yield break;
            }

            asyncLoad.allowSceneActivation = false;

            // 최소 표시 시간 (너무 빨리 사라지면 깜빡임 느낌)
            float minDisplayTime = 0.8f;
            float elapsed = 0f;

            while (!asyncLoad.isDone)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateProgress(progress);

                if (asyncLoad.progress >= 0.9f && elapsed >= minDisplayTime)
                {
                    UpdateProgress(1f);
                    yield return new WaitForSecondsRealtime(0.3f);
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // 씬 활성화 후 잠시 대기
            yield return new WaitForSecondsRealtime(0.1f);
            Hide();
        }

        /// <summary>
        /// 진행률 업데이트
        /// </summary>
        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                var rect = progressBar.GetComponent<RectTransform>();
                if (rect != null)
                    rect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            }

            if (percentText != null)
                percentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        /// <summary>
        /// "LOADING" 텍스트 점 애니메이션 + 글로우 펄스
        /// </summary>
        private IEnumerator LoadingAnimation()
        {
            int dotCount = 0;
            float dotTimer = 0f;
            float glowTimer = 0f;

            while (true)
            {
                dotTimer += Time.unscaledDeltaTime;
                glowTimer += Time.unscaledDeltaTime;

                // 점 애니메이션 (0.4초마다)
                if (dotTimer >= 0.4f)
                {
                    dotTimer = 0f;
                    dotCount = (dotCount + 1) % 4;
                    string dots = new string('.', dotCount);
                    if (loadingText != null)
                        loadingText.text = $"LOADING{dots}";
                }

                // 글로우 펄스 (프로그레스 바 끝)
                if (progressGlow != null)
                {
                    float alpha = 0.3f + 0.4f * Mathf.Abs(Mathf.Sin(glowTimer * 3f));
                    progressGlow.color = new Color(0.5f, 1f, 1f, alpha);
                }

                // 배경 미세 색상 변화
                if (backgroundImage != null && backgroundImage.sprite != null)
                {
                    float r = 0.28f + 0.04f * Mathf.Sin(glowTimer * 0.5f);
                    float g = 0.28f + 0.04f * Mathf.Sin(glowTimer * 0.5f + 0.5f);
                    float b = 0.33f + 0.04f * Mathf.Sin(glowTimer * 0.5f + 1.0f);
                    backgroundImage.color = new Color(r, g, b, 1f);
                }

                yield return null;
            }
        }

        /// <summary>
        /// 이퀄라이저 바 애니메이션
        /// </summary>
        private IEnumerator EqualizerAnimation()
        {
            while (true)
            {
                for (int i = 0; i < EQ_BAR_COUNT; i++)
                {
                    if (eqBars[i] == null) continue;
                    float h = 0.1f + 0.9f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * eqSpeeds[i] + eqPhases[i]));
                    var r = eqBars[i].GetComponent<RectTransform>();
                    if (r != null)
                        r.anchorMax = new Vector2(r.anchorMax.x, h);
                }
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
