using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using AIBeat.Core;

namespace AIBeat.UI
{
    /// <summary>
    /// 씬 전환 시 표시되는 로딩 화면
    /// 게임 팁 랜덤 표시 + 진행률 바
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        // 게임 팁 배열
        private static readonly string[] TIPS = new string[]
        {
            "PERFECT judgement is within \u00b150ms",
            "Maintain combo for score bonus",
            "Note speed can be adjusted in Settings",
            "Hold long notes until the end",
            "Use S or L key for scratch notes"
        };

        // UI 요소
        private GameObject rootPanel;
        private Image progressBar;
        private Image progressBg;
        private TextMeshProUGUI tipText;
        private TextMeshProUGUI loadingText;
        private TextMeshProUGUI percentText;

        // 색상
        private static readonly Color BG_COLOR = new Color(0.01f, 0.01f, 0.05f, 1f);
        private static readonly Color CYAN = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color CYAN_DIM = new Color(0f, 0.5f, 0.7f, 0.4f);
        private static readonly Color TEXT_DIM = new Color(0.6f, 0.6f, 0.7f, 0.8f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildUI();
                rootPanel.SetActive(false);
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
            // Canvas가 없으면 생성
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999; // 최상위
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

            // 루트 패널 (배경)
            rootPanel = new GameObject("LoadingPanel");
            rootPanel.transform.SetParent(transform, false);
            var rootRect = rootPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var bgImage = rootPanel.AddComponent<Image>();
            bgImage.color = BG_COLOR;

            // "LOADING" 텍스트 (중앙)
            var loadingGo = new GameObject("LoadingText");
            loadingGo.transform.SetParent(rootPanel.transform, false);
            var loadingRect = loadingGo.AddComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.1f, 0.48f);
            loadingRect.anchorMax = new Vector2(0.9f, 0.58f);
            loadingRect.offsetMin = Vector2.zero;
            loadingRect.offsetMax = Vector2.zero;

            loadingText = loadingGo.AddComponent<TextMeshProUGUI>();
            loadingText.text = "LOADING...";
            loadingText.fontSize = 32;
            loadingText.color = CYAN;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.fontStyle = FontStyles.Bold;

            // 진행률 바 배경
            var progressBgGo = new GameObject("ProgressBg");
            progressBgGo.transform.SetParent(rootPanel.transform, false);
            var progressBgRect = progressBgGo.AddComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.15f, 0.42f);
            progressBgRect.anchorMax = new Vector2(0.85f, 0.44f);
            progressBgRect.offsetMin = Vector2.zero;
            progressBgRect.offsetMax = Vector2.zero;

            progressBg = progressBgGo.AddComponent<Image>();
            progressBg.color = CYAN_DIM;

            // 진행률 바 (채우기)
            var progressGo = new GameObject("ProgressBar");
            progressGo.transform.SetParent(progressBgGo.transform, false);
            var progressRect = progressGo.AddComponent<RectTransform>();
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = new Vector2(0f, 1f); // 초기값 0%
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;

            progressBar = progressGo.AddComponent<Image>();
            progressBar.color = CYAN;

            // 퍼센트 텍스트
            var percentGo = new GameObject("PercentText");
            percentGo.transform.SetParent(rootPanel.transform, false);
            var percentRect = percentGo.AddComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0.3f, 0.36f);
            percentRect.anchorMax = new Vector2(0.7f, 0.42f);
            percentRect.offsetMin = Vector2.zero;
            percentRect.offsetMax = Vector2.zero;

            percentText = percentGo.AddComponent<TextMeshProUGUI>();
            percentText.text = "0%";
            percentText.fontSize = 20;
            percentText.color = CYAN;
            percentText.alignment = TextAlignmentOptions.Center;

            // 팁 텍스트 (하단)
            var tipGo = new GameObject("TipText");
            tipGo.transform.SetParent(rootPanel.transform, false);
            var tipRect = tipGo.AddComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.1f, 0.22f);
            tipRect.anchorMax = new Vector2(0.9f, 0.32f);
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;

            tipText = tipGo.AddComponent<TextMeshProUGUI>();
            tipText.text = "";
            tipText.fontSize = 20;
            tipText.color = TEXT_DIM;
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.fontStyle = FontStyles.Italic;

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(rootPanel);
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
            // 로딩 화면 표시
            rootPanel.SetActive(true);

            // 랜덤 팁 표시
            if (tipText != null)
            {
                string tip = TIPS[Random.Range(0, TIPS.Length)];
                tipText.text = $"TIP: {tip}";
            }

            // 진행률 초기화
            UpdateProgress(0f);

            // 비동기 씬 로드
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            if (asyncLoad == null)
            {
                Debug.LogError($"[LoadingScreen] Scene load failed: {sceneName}");
                rootPanel.SetActive(false);
                yield break;
            }

            asyncLoad.allowSceneActivation = false;

            // 진행률 업데이트
            while (!asyncLoad.isDone)
            {
                // asyncLoad.progress는 0~0.9 범위 (0.9에서 대기)
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateProgress(progress);

                // 씬 로드 완료 (progress >= 0.9)
                if (asyncLoad.progress >= 0.9f)
                {
                    UpdateProgress(1f);
                    yield return new WaitForSeconds(0.3f); // 잠시 대기 (100% 표시)
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // 로딩 화면 숨기기
            rootPanel.SetActive(false);
        }

        /// <summary>
        /// 진행률 바 업데이트
        /// </summary>
        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                var rect = progressBar.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMax = new Vector2(progress, 1f);
                }
            }

            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
