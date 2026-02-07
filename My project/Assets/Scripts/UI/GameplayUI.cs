using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Data;
using AIBeat.Core;
using AIBeat.Gameplay;

namespace AIBeat.UI
{
    /// <summary>
    /// 게임플레이 씬 UI 관리
    /// 상단 HUD: Score + 판정별 카운트 (PERFECT/GREAT/GOOD/BAD/MISS)
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text songTitleText;

        [Header("Judgement Display")]
        [SerializeField] private TMP_Text judgementText;
        [SerializeField] private float judgementDisplayTime = 0.5f;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private TMP_Text countdownText;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private TMP_Text resultComboText;
        [SerializeField] private TMP_Text resultAccuracyText;
        [SerializeField] private TMP_Text resultRankText;
        [SerializeField] private TMP_Text resultPerfectText;
        [SerializeField] private TMP_Text resultGreatText;
        [SerializeField] private TMP_Text resultGoodText;
        [SerializeField] private TMP_Text resultBadText;
        [SerializeField] private TMP_Text resultMissText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        // 결과 화면 추가 요소 (동적 생성)
        private TMP_Text resultSongInfoText;
        private TMP_Text resultNewRecordText;
        private TMP_Text resultBonusScoreText;

        // 보너스 점수 표시
        private TMP_Text bonusScoreText;
        private Coroutine bonusScoreCoroutine;

        [Header("Colors (Music Theme)")]
        [SerializeField] private Color perfectColor = new Color(1f, 0.85f, 0.1f);    // 골드 비트
        [SerializeField] private Color greatColor = new Color(0.2f, 0.9f, 1f);       // 네온 시안
        [SerializeField] private Color goodColor = new Color(0.4f, 1f, 0.6f);        // 민트
        [SerializeField] private Color badColor = new Color(0.8f, 0.3f, 0.6f);       // 핑크
        [SerializeField] private Color missColor = new Color(0.4f, 0.4f, 0.5f);      // 다크 그레이

        private Coroutine judgementCoroutine;
        private TMP_Text earlyLateText; // Early/Late 피드백 표시

        // 상단 판정 통계 HUD (동적 생성)
        private GameObject statsPanel;
        private TMP_Text statsPerfectText;
        private TMP_Text statsGreatText;
        private TMP_Text statsGoodText;
        private TMP_Text statsBadText;
        private TMP_Text statsMissText;
        private JudgementSystem judgementSystem;
        private GameplayController gameplayController;
        private SongData currentSongData; // 결과 화면에 곡 정보 표시용
        private Button pauseButton; // 모바일용 일시정지 버튼

        private void Awake()
        {
            AutoSetupReferences();
            CreateStatsHUD();
            CreateEarlyLateText();
            CreateBonusScoreText();
            CreatePauseButton();
            RepositionHUD();

            // 패널은 Awake에서 즉시 숨기기
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (judgementText != null) judgementText.gameObject.SetActive(false);

            // Combo 초기 텍스트 비우기 (콤보 0일때 표시 안함)
            if (comboText != null) comboText.text = "";

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(gameObject);
        }

        /// <summary>
        /// Inspector에서 참조가 없으면 자동으로 찾기
        /// </summary>
        private void AutoSetupReferences()
        {
            // HUD
            if (scoreText == null)
                scoreText = transform.Find("ScorePanel/ScoreText")?.GetComponent<TMP_Text>();
            if (comboText == null)
                comboText = transform.Find("ComboText")?.GetComponent<TMP_Text>();
            if (songTitleText == null)
                songTitleText = transform.Find("SongTitleText")?.GetComponent<TMP_Text>();
            if (judgementText == null)
                judgementText = transform.Find("JudgementText")?.GetComponent<TMP_Text>();

            // Countdown
            if (countdownPanel == null)
                countdownPanel = transform.Find("CountdownPanel")?.gameObject;
            if (countdownText == null && countdownPanel != null)
                countdownText = countdownPanel.transform.Find("CountdownText")?.GetComponent<TMP_Text>();

            // Pause Panel
            if (pausePanel == null)
                pausePanel = transform.Find("PausePanel")?.gameObject;
            if (pausePanel != null)
            {
                if (resumeButton == null)
                    resumeButton = pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
                if (restartButton == null)
                    restartButton = pausePanel.transform.Find("RestartButton")?.GetComponent<Button>();
                if (quitButton == null)
                    quitButton = pausePanel.transform.Find("QuitButton")?.GetComponent<Button>();
            }

            // Result Panel
            if (resultPanel == null)
                resultPanel = transform.Find("ResultPanel")?.gameObject;
            if (resultPanel != null)
            {
                var rp = resultPanel.transform;
                if (resultScoreText == null)
                    resultScoreText = rp.Find("ResultScoreText")?.GetComponent<TMP_Text>();
                if (resultComboText == null)
                    resultComboText = rp.Find("ResultComboText")?.GetComponent<TMP_Text>();
                if (resultAccuracyText == null)
                    resultAccuracyText = rp.Find("ResultAccuracyText")?.GetComponent<TMP_Text>();
                if (resultRankText == null)
                    resultRankText = rp.Find("ResultRankText")?.GetComponent<TMP_Text>();
                if (resultPerfectText == null)
                    resultPerfectText = rp.Find("PerfectText")?.GetComponent<TMP_Text>();
                if (resultGreatText == null)
                    resultGreatText = rp.Find("GreatText")?.GetComponent<TMP_Text>();
                if (resultGoodText == null)
                    resultGoodText = rp.Find("GoodText")?.GetComponent<TMP_Text>();
                if (resultBadText == null)
                    resultBadText = rp.Find("BadText")?.GetComponent<TMP_Text>();
                if (resultMissText == null)
                    resultMissText = rp.Find("MissText")?.GetComponent<TMP_Text>();
                if (retryButton == null)
                    retryButton = rp.Find("RetryButton")?.GetComponent<Button>();
                if (menuButton == null)
                    menuButton = rp.Find("MenuButton")?.GetComponent<Button>();
            }

            // JudgementSystem, GameplayController 참조
            judgementSystem = FindFirstObjectByType<JudgementSystem>();
            gameplayController = FindFirstObjectByType<GameplayController>();

#if UNITY_EDITOR
            Debug.Log($"[GameplayUI] AutoSetup - Score:{scoreText != null}, Combo:{comboText != null}, Judgement:{judgementText != null}, ResultPanel:{resultPanel != null}");
#endif
        }

        /// <summary>
        /// HUD 요소 위치 재배치 (스코어 상단 중앙, 콤보 하단)
        /// </summary>
        private void RepositionHUD()
        {
            // ScorePanel → 상단 중앙 (DJ 콘솔 스타일)
            if (scoreText != null)
            {
                var scorePanel = scoreText.transform.parent;
                if (scorePanel != null)
                {
                    var panelRect = scorePanel.GetComponent<RectTransform>();
                    if (panelRect != null)
                    {
                        panelRect.anchorMin = new Vector2(0.5f, 1f);
                        panelRect.anchorMax = new Vector2(0.5f, 1f);
                        panelRect.pivot = new Vector2(0.5f, 1f);
                        panelRect.anchoredPosition = new Vector2(0, -10);
                        panelRect.sizeDelta = new Vector2(380, 56);
                    }

                    // Score 패널 배경 (다크 그라데이션)
                    var panelImage = scorePanel.GetComponent<Image>();
                    if (panelImage != null)
                    {
                        panelImage.color = new Color(0.03f, 0.02f, 0.1f, 0.8f);
                    }

                    // Outline (이퀄라이저 블루-퍼플 테두리)
                    var outline = scorePanel.GetComponent<Outline>();
                    if (outline == null)
                        outline = scorePanel.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.3f, 0.4f, 1f, 0.5f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);

                    // ScoreLabel 숨기기 (숫자만 표시)
                    var labelTransform = scorePanel.Find("ScoreLabel");
                    if (labelTransform != null)
                        labelTransform.gameObject.SetActive(false);

                    // ScoreText 스타일 (LED 디스플레이 느낌)
                    scoreText.fontSize = 38;
                    scoreText.alignment = TextAlignmentOptions.Center;
                    scoreText.color = new Color(0.9f, 0.95f, 1f);
                    scoreText.fontStyle = FontStyles.Bold;
                    scoreText.outlineWidth = 0.1f;
                    scoreText.outlineColor = new Color32(60, 80, 255, 100);
                }
            }

            // ComboText → 좌상단 (노트 영역 회피)
            if (comboText != null)
            {
                var comboRect = comboText.GetComponent<RectTransform>();
                if (comboRect != null)
                {
                    comboRect.anchorMin = new Vector2(0, 1);
                    comboRect.anchorMax = new Vector2(0, 1);
                    comboRect.pivot = new Vector2(0, 1);
                    comboRect.anchoredPosition = new Vector2(15, -60);
                    comboRect.sizeDelta = new Vector2(250, 40);
                }
                comboText.fontSize = 34;
                comboText.alignment = TextAlignmentOptions.Left;
                comboText.color = new Color(1f, 1f, 1f, 0.9f);
                comboText.fontStyle = FontStyles.Bold;
                comboText.outlineWidth = 0.15f;
                comboText.outlineColor = new Color32(0, 0, 0, 180);
                comboText.text = "";
            }

            // JudgementText → 판정선 약간 위 (하단 30% 지점)
            if (judgementText != null)
            {
                var judgeRect = judgementText.GetComponent<RectTransform>();
                if (judgeRect != null)
                {
                    judgeRect.anchorMin = new Vector2(0.5f, 0.30f);
                    judgeRect.anchorMax = new Vector2(0.5f, 0.30f);
                    judgeRect.anchoredPosition = new Vector2(0, 0);
                    judgeRect.sizeDelta = new Vector2(500, 60);
                }
                judgementText.fontSize = 48;
                judgementText.fontStyle = FontStyles.Bold;
                judgementText.outlineWidth = 0.2f;
                judgementText.outlineColor = new Color32(0, 0, 0, 200);
            }
        }

        /// <summary>
        /// 모바일용 일시정지 버튼 (우상단)
        /// </summary>
        private void CreatePauseButton()
        {
            var pauseBtnGo = new GameObject("PauseButton");
            pauseBtnGo.transform.SetParent(transform, false);

            var btnRect = pauseBtnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-10, -10);
            btnRect.sizeDelta = new Vector2(50, 50);

            var btnImage = pauseBtnGo.AddComponent<Image>();
            btnImage.color = new Color(0.03f, 0.02f, 0.1f, 0.6f);

            var btnOutline = pauseBtnGo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.3f, 0.4f, 1f, 0.35f);
            btnOutline.effectDistance = new Vector2(1, -1);

            // 일시정지 아이콘 (음악 정지)
            var textGo = new GameObject("PauseIcon");
            textGo.transform.SetParent(pauseBtnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var iconText = textGo.AddComponent<TextMeshProUGUI>();
            iconText.text = "\u23F8";
            iconText.fontSize = 22;
            iconText.color = new Color(0.6f, 0.7f, 1f, 0.9f);
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.fontStyle = FontStyles.Bold;

            pauseButton = pauseBtnGo.AddComponent<Button>();
            pauseButton.targetGraphic = btnImage;
            pauseButton.onClick.AddListener(() => gameplayController?.PauseGame());
        }

        /// <summary>
        /// Early/Late 피드백 텍스트 동적 생성 (판정 텍스트 아래)
        /// </summary>
        private void CreateEarlyLateText()
        {
            var go = new GameObject("EarlyLateText");
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f); // 판정 텍스트 아래
            rect.sizeDelta = new Vector2(300f, 40f);

            earlyLateText = go.AddComponent<TextMeshProUGUI>();
            earlyLateText.fontSize = 22;
            earlyLateText.alignment = TextAlignmentOptions.Center;
            earlyLateText.fontStyle = FontStyles.Bold;
            earlyLateText.text = "";
            go.SetActive(false);
        }

        /// <summary>
        /// 보너스 점수 팝업 텍스트 동적 생성 (콤보 텍스트 아래)
        /// </summary>
        private void CreateBonusScoreText()
        {
            var go = new GameObject("BonusScoreText");
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(15, -100);
            rect.sizeDelta = new Vector2(250f, 30f);

            bonusScoreText = go.AddComponent<TextMeshProUGUI>();
            bonusScoreText.fontSize = 20;
            bonusScoreText.alignment = TextAlignmentOptions.Left;
            bonusScoreText.fontStyle = FontStyles.Bold;
            bonusScoreText.color = new Color(1f, 0.85f, 0.2f, 1f); // 골드
            bonusScoreText.outlineWidth = 0.15f;
            bonusScoreText.outlineColor = new Color32(0, 0, 0, 180);
            bonusScoreText.text = "";
            go.SetActive(false);
        }

        /// <summary>
        /// 보너스 점수 표시 (홀드 중 틱마다 호출)
        /// </summary>
        public void ShowBonusScore(int tickAmount, int totalBonus)
        {
            if (bonusScoreText == null) return;

            bonusScoreText.gameObject.SetActive(true);
            bonusScoreText.text = $"BONUS +{totalBonus}";
            bonusScoreText.color = new Color(1f, 0.85f, 0.2f, 1f);

            // 작은 팝업 효과
            bonusScoreText.transform.localScale = Vector3.one * 1.15f;
            UIAnimator.ScaleTo(this, bonusScoreText.gameObject, Vector3.one, 0.1f);

            // 기존 숨기기 코루틴 취소 후 재시작
            if (bonusScoreCoroutine != null)
                StopCoroutine(bonusScoreCoroutine);
            bonusScoreCoroutine = StartCoroutine(HideBonusScore());
        }

        private System.Collections.IEnumerator HideBonusScore()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            if (bonusScoreText != null)
                bonusScoreText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 상단 판정 통계 HUD 동적 생성
        /// SCORE + PERFECT/GREAT/GOOD/BAD/MISS 행별 표시
        /// </summary>
        private void CreateStatsHUD()
        {
            // StatsPanel 컨테이너 (우상단)
            statsPanel = new GameObject("StatsPanel");
            statsPanel.transform.SetParent(transform, false);

            var statsRect = statsPanel.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(1, 1);  // 우상단
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.pivot = new Vector2(1, 1);
            statsRect.anchoredPosition = new Vector2(-8, -60);
            statsRect.sizeDelta = new Vector2(140, 115);

            // 반투명 DJ 콘솔 배경
            var bgImage = statsPanel.AddComponent<Image>();
            bgImage.color = new Color(0.03f, 0.02f, 0.1f, 0.4f);

            // 이퀄라이저 테두리
            var statsOutline = statsPanel.AddComponent<Outline>();
            statsOutline.effectColor = new Color(0.3f, 0.4f, 1f, 0.3f);
            statsOutline.effectDistance = new Vector2(1, -1);

            // VerticalLayoutGroup으로 행 정렬
            var layout = statsPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 1;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 판정별 텍스트 생성
            statsPerfectText = CreateStatsRow(statsPanel.transform, "PERFECT", "0", perfectColor);
            statsGreatText = CreateStatsRow(statsPanel.transform, "GREAT", "0", greatColor);
            statsGoodText = CreateStatsRow(statsPanel.transform, "GOOD", "0", goodColor);
            statsBadText = CreateStatsRow(statsPanel.transform, "BAD", "0", badColor);
            statsMissText = CreateStatsRow(statsPanel.transform, "MISS", "0", missColor);
        }

        /// <summary>
        /// 판정 통계 행 생성 (라벨 + 카운트)
        /// </summary>
        private TMP_Text CreateStatsRow(Transform parent, string label, string value, Color color)
        {
            var row = new GameObject($"Stats_{label}");
            row.transform.SetParent(parent, false);

            var rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(124, 18);

            // HorizontalLayoutGroup
            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 5;
            hLayout.childAlignment = TextAnchor.MiddleRight;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // 라벨
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = color;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.fontStyle = FontStyles.Bold;

            // 카운트
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(row.transform, false);
            var valueRect = valueGo.AddComponent<RectTransform>();
            var valueLayout = valueGo.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 45;
            var valueText = valueGo.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 14;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.fontStyle = FontStyles.Bold;

            return valueText;
        }

        /// <summary>
        /// 판정 통계 HUD 업데이트
        /// </summary>
        private void UpdateStatsHUD()
        {
            if (judgementSystem == null) return;

            if (statsPerfectText != null)
                statsPerfectText.text = judgementSystem.PerfectCount.ToString();
            if (statsGreatText != null)
                statsGreatText.text = judgementSystem.GreatCount.ToString();
            if (statsGoodText != null)
                statsGoodText.text = judgementSystem.GoodCount.ToString();
            if (statsBadText != null)
                statsBadText.text = judgementSystem.BadCount.ToString();
            if (statsMissText != null)
                statsMissText.text = judgementSystem.MissCount.ToString();
        }

        public void Initialize(SongData songData)
        {
            currentSongData = songData;

            if (songTitleText != null)
                songTitleText.text = songData.Title;

            UpdateScore(0);
            UpdateCombo(0);

            // 버튼 이벤트 연결 (중복 등록 방지: RemoveAllListeners 후 AddListener)
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(OnResumeClicked);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRestartClicked);
            }
            if (menuButton != null)
            {
                menuButton.onClick.RemoveAllListeners();
                menuButton.onClick.AddListener(OnQuitClicked);
            }

            // 패널 초기화
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (judgementText != null) judgementText.gameObject.SetActive(false);

            // JudgementSystem 참조 갱신
            if (judgementSystem == null)
                judgementSystem = FindFirstObjectByType<JudgementSystem>();

            // 모바일 버튼 최소 크기 보장
            EnsureMinButtonSize(resumeButton);
            EnsureMinButtonSize(restartButton);
            EnsureMinButtonSize(quitButton);
            EnsureMinButtonSize(retryButton);
            EnsureMinButtonSize(menuButton);

            // 한국어 버튼 텍스트 + 스타일 적용
            ApplyButtonStyle(resumeButton, "이어하기");
            ApplyButtonStyle(restartButton, "다시하기");
            ApplyButtonStyle(quitButton, "나가기");
            ApplyButtonStyle(retryButton, "재시도");
            ApplyButtonStyle(menuButton, "메뉴");
        }

        /// <summary>
        /// 버튼에 한국어 텍스트 + 네온 스타일 적용
        /// </summary>
        private void ApplyButtonStyle(Button btn, string koreanText)
        {
            if (btn == null) return;

            // 배경색
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

            // 네온 테두리
            var outline = btn.GetComponent<Outline>();
            if (outline == null)
                outline = btn.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.9f, 1f, 0.5f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 텍스트
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = koreanText;
                tmp.fontSize = 30;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.4f, 0.95f, 1f, 1f);
            }
        }

        /// <summary>
        /// 버튼 최소 높이 보장 (모바일 터치 최적화)
        /// </summary>
        private void EnsureMinButtonSize(Button btn, float minHeight = 56f)
        {
            if (btn == null) return;
            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                var size = rect.sizeDelta;
                if (size.y < minHeight)
                {
                    size.y = minHeight;
                    rect.sizeDelta = size;
                }
            }
            var layout = btn.GetComponent<LayoutElement>();
            if (layout == null) layout = btn.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = minHeight;
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString("N0");
        }

        public void UpdateCombo(int combo)
        {
            if (comboText != null)
            {
                comboText.text = combo > 0 ? $"{combo} COMBO" : "";

                // 콤보 색상 단계 (이퀄라이저 바 그라데이션)
                if (combo >= 100)
                    comboText.color = new Color(1f, 0.2f, 0.8f, 1f); // 핫 핑크 (MAX)
                else if (combo >= 50)
                    comboText.color = new Color(1f, 0.85f, 0.1f, 1f); // 골드 비트
                else if (combo >= 25)
                    comboText.color = new Color(0.2f, 0.9f, 1f, 1f); // 네온 시안
                else if (combo >= 10)
                    comboText.color = new Color(0.4f, 1f, 0.6f, 1f); // 민트
                else
                    comboText.color = new Color(0.8f, 0.85f, 1f, 0.9f); // 라이트 블루

                // 10콤보 단위 마일스톤 펄스
                if (combo > 0 && combo % 10 == 0)
                {
                    comboText.transform.localScale = Vector3.one * 1.2f;
                    UIAnimator.ScaleTo(this, comboText.gameObject, Vector3.one, 0.2f);
                }
            }

            // 판정 통계 HUD 업데이트 (콤보 변경 = 판정 발생)
            UpdateStatsHUD();
        }

        public void ShowJudgement(JudgementResult result)
        {
            ShowJudgementDetailed(result, 0f);
        }

        /// <summary>
        /// Early/Late 피드백 포함 판정 표시
        /// rawDiff: 양수=LATE, 음수=EARLY
        /// </summary>
        public void ShowJudgementDetailed(JudgementResult result, float rawDiff)
        {
            if (judgementText == null) return;

            // 기존 코루틴 중지
            if (judgementCoroutine != null)
                StopCoroutine(judgementCoroutine);

            judgementText.gameObject.SetActive(true);

            (string text, Color color) = result switch
            {
                JudgementResult.Perfect => ("\u266B PERFECT!", perfectColor),
                JudgementResult.Great => ("\u266A GREAT!", greatColor),
                JudgementResult.Good => ("GOOD", goodColor),
                JudgementResult.Bad => ("BAD", badColor),
                _ => ("MISS", missColor)
            };

            judgementText.text = text;
            judgementText.color = color;

            // Early/Late 피드백 (Perfect 이외 + Miss 이외에서 표시)
            if (earlyLateText != null)
            {
                if (result != JudgementResult.Perfect && result != JudgementResult.Miss && Mathf.Abs(rawDiff) > 0.01f)
                {
                    earlyLateText.gameObject.SetActive(true);
                    bool isLate = rawDiff > 0;
                    earlyLateText.text = isLate ? "LATE" : "EARLY";
                    earlyLateText.color = isLate
                        ? new Color(1f, 0.5f, 0.2f, 0.8f)  // 주황 (느림)
                        : new Color(0.3f, 0.7f, 1f, 0.8f);  // 하늘 (빠름)
                }
                else
                {
                    earlyLateText.gameObject.SetActive(false);
                }
            }

            // 판정별 스케일 차별화 팝업
            float startScale = result switch
            {
                JudgementResult.Perfect => 1.4f,
                JudgementResult.Great => 1.3f,
                JudgementResult.Good => 1.1f,
                JudgementResult.Bad => 1.1f,
                _ => 0.8f // Miss: 축소 등장
            };
            judgementText.transform.localScale = Vector3.one * startScale;
            UIAnimator.ScaleTo(this, judgementText.gameObject, Vector3.one, 0.15f);

            judgementCoroutine = StartCoroutine(HideJudgement());

            // 판정 통계 HUD 업데이트
            UpdateStatsHUD();
        }

        private System.Collections.IEnumerator HideJudgement()
        {
            yield return new WaitForSecondsRealtime(judgementDisplayTime);
            judgementText.gameObject.SetActive(false);
            if (earlyLateText != null) earlyLateText.gameObject.SetActive(false);
        }

        public void ShowCountdown(bool show)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(show);
        }

        public void UpdateCountdown(string text)
        {
            if (countdownText != null)
            {
                countdownText.text = text;

                // 숫자 팝업 애니메이션
                countdownText.transform.localScale = Vector3.one * 2f;
                UIAnimator.ScaleTo(this, countdownText.gameObject, Vector3.one, 0.3f);
            }
        }

        public void ShowPauseMenu(bool show)
        {
            if (pausePanel != null)
                pausePanel.SetActive(show);
            // 일시정지 중에는 버튼 숨김
            if (pauseButton != null)
                pauseButton.gameObject.SetActive(!show);
        }

        public void ShowResult(GameResult result)
        {
#if UNITY_EDITOR
            Debug.Log($"[GameplayUI] ShowResult called - resultPanel:{resultPanel != null}, Score:{result.Score}, Rank:{result.Rank}, Miss:{result.MissCount}");
#endif

            // 통계 HUD + 일시정지 버튼 숨기기
            if (statsPanel != null) statsPanel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);

            if (resultPanel == null)
            {
                Debug.LogWarning("[GameplayUI] ResultPanel is NULL! Result screen cannot be shown.");
                return;
            }

            // 결과 화면 표시 시 다른 패널 숨기기
            if (pausePanel != null) pausePanel.SetActive(false);
            if (judgementText != null) judgementText.gameObject.SetActive(false);

            // ResultPanel에 배경 Image (DJ 부스 / 스테이지 느낌)
            var bgImage2 = resultPanel.GetComponent<Image>();
            if (bgImage2 == null)
            {
                bgImage2 = resultPanel.AddComponent<Image>();
            }
            bgImage2.color = new Color(0.04f, 0.03f, 0.12f, 0.96f);

            resultPanel.SetActive(true);

            // 곡 정보 표시 (동적 생성)
            CreateResultSongInfo(result);

            if (resultScoreText != null)
                resultScoreText.text = result.Score.ToString("N0");

            // 보너스 점수가 있으면 별도 표시
            CreateResultBonusScore(result);

            if (resultComboText != null)
                resultComboText.text = $"최대 콤보: {result.MaxCombo}";

            if (resultAccuracyText != null)
                resultAccuracyText.text = $"정확도: {result.Accuracy:F1}%";

            if (resultRankText != null)
            {
                resultRankText.text = result.Rank;
                resultRankText.color = GetRankColor(result.Rank);
            }

            if (resultPerfectText != null)
                resultPerfectText.text = $"PERFECT: {result.PerfectCount}";

            if (resultGreatText != null)
                resultGreatText.text = $"GREAT: {result.GreatCount}";

            if (resultGoodText != null)
                resultGoodText.text = $"GOOD: {result.GoodCount}";

            if (resultBadText != null)
                resultBadText.text = $"BAD: {result.BadCount}";

            if (resultMissText != null)
                resultMissText.text = $"MISS: {result.MissCount}";

            // NEW RECORD 체크 및 표시
            CheckAndShowNewRecord(result);

            // 결과 패널 애니메이션
            resultPanel.transform.localScale = Vector3.zero;
            UIAnimator.ScaleTo(this, resultPanel, Vector3.one, 0.5f);
        }

        /// <summary>
        /// 결과 화면에 곡 정보(제목/장르/BPM) 동적 생성
        /// </summary>
        private void CreateResultSongInfo(GameResult result)
        {
            if (resultPanel == null || currentSongData == null) return;

            // 이미 생성되어 있으면 업데이트만
            if (resultSongInfoText != null)
            {
                resultSongInfoText.text = $"\u266B {currentSongData.Title}  |  {currentSongData.Genre}  |  BPM {currentSongData.BPM:F0}";
                return;
            }

            var songInfoGo = new GameObject("ResultSongInfo");
            songInfoGo.transform.SetParent(resultPanel.transform, false);
            var songInfoRect = songInfoGo.AddComponent<RectTransform>();
            songInfoRect.anchorMin = new Vector2(0.05f, 0.92f);
            songInfoRect.anchorMax = new Vector2(0.95f, 1f);
            songInfoRect.offsetMin = Vector2.zero;
            songInfoRect.offsetMax = Vector2.zero;

            resultSongInfoText = songInfoGo.AddComponent<TextMeshProUGUI>();
            resultSongInfoText.text = $"\u266B {currentSongData.Title}  |  {currentSongData.Genre}  |  BPM {currentSongData.BPM:F0}";
            resultSongInfoText.fontSize = 18;
            resultSongInfoText.color = new Color(0.5f, 0.6f, 1f, 0.75f);
            resultSongInfoText.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// 결과 화면에 보너스 점수 표시 (동적 생성)
        /// </summary>
        private void CreateResultBonusScore(GameResult result)
        {
            if (resultPanel == null || result.BonusScore <= 0) return;

            if (resultBonusScoreText == null)
            {
                var bonusGo = new GameObject("ResultBonusScore");
                bonusGo.transform.SetParent(resultPanel.transform, false);

                // ResultScoreText 아래에 배치
                var bonusRect = bonusGo.AddComponent<RectTransform>();
                // resultScoreText의 위치를 기준으로 아래에 배치
                if (resultScoreText != null)
                {
                    var scoreRect = resultScoreText.GetComponent<RectTransform>();
                    bonusRect.anchorMin = scoreRect.anchorMin;
                    bonusRect.anchorMax = scoreRect.anchorMax;
                    bonusRect.anchoredPosition = scoreRect.anchoredPosition + new Vector2(0, -30);
                    bonusRect.sizeDelta = scoreRect.sizeDelta;
                }
                else
                {
                    bonusRect.anchorMin = new Vector2(0.1f, 0.7f);
                    bonusRect.anchorMax = new Vector2(0.9f, 0.76f);
                    bonusRect.offsetMin = Vector2.zero;
                    bonusRect.offsetMax = Vector2.zero;
                }

                resultBonusScoreText = bonusGo.AddComponent<TextMeshProUGUI>();
                resultBonusScoreText.fontSize = 18;
                resultBonusScoreText.alignment = TextAlignmentOptions.Center;
                resultBonusScoreText.fontStyle = FontStyles.Bold;
            }

            resultBonusScoreText.text = $"(BONUS +{result.BonusScore:N0})";
            resultBonusScoreText.color = new Color(1f, 0.85f, 0.2f, 0.9f); // 골드
            resultBonusScoreText.gameObject.SetActive(true);
        }

        /// <summary>
        /// NEW RECORD 확인 및 표시
        /// PlayerPrefs에 곡별 최고 점수를 저장/비교
        /// </summary>
        private void CheckAndShowNewRecord(GameResult result)
        {
            if (resultPanel == null) return;

            string songId = currentSongData != null ? currentSongData.Id : "unknown";
            string prefsKey = $"HighScore_{songId}";
            int previousBest = PlayerPrefs.GetInt(prefsKey, 0);

            bool isNewRecord = result.Score > previousBest;

            if (isNewRecord)
            {
                // 최고 점수 저장
                PlayerPrefs.SetInt(prefsKey, result.Score);
                PlayerPrefs.Save();
            }

            // NEW RECORD 텍스트 (동적 생성 또는 업데이트)
            if (resultNewRecordText == null)
            {
                var newRecordGo = new GameObject("NewRecordText");
                newRecordGo.transform.SetParent(resultPanel.transform, false);
                var nrRect = newRecordGo.AddComponent<RectTransform>();
                nrRect.anchorMin = new Vector2(0.1f, 0.82f);
                nrRect.anchorMax = new Vector2(0.9f, 0.92f);
                nrRect.offsetMin = Vector2.zero;
                nrRect.offsetMax = Vector2.zero;

                resultNewRecordText = newRecordGo.AddComponent<TextMeshProUGUI>();
                resultNewRecordText.fontSize = 28;
                resultNewRecordText.alignment = TextAlignmentOptions.Center;
                resultNewRecordText.fontStyle = FontStyles.Bold;
            }

            if (isNewRecord)
            {
                resultNewRecordText.text = "\u2605 신기록! \u2605";
                resultNewRecordText.color = new Color(1f, 1f, 0.2f, 1f); // 노란색
                resultNewRecordText.gameObject.SetActive(true);

                // 깜빡이는 애니메이션 효과 (스케일)
                resultNewRecordText.transform.localScale = Vector3.one * 1.3f;
                UIAnimator.ScaleTo(this, resultNewRecordText.gameObject, Vector3.one, 0.4f);
            }
            else
            {
                resultNewRecordText.gameObject.SetActive(false);
            }
        }

        private Color GetRankColor(string rank)
        {
            return rank switch
            {
                "S+" => new Color(1f, 0.85f, 0.1f),    // 골드 비트
                "S" => new Color(1f, 0.75f, 0.2f),      // 오렌지 골드
                "A" => new Color(0.2f, 0.9f, 1f),       // 네온 시안
                "B" => new Color(0.4f, 1f, 0.6f),       // 민트
                "C" => new Color(0.7f, 0.75f, 0.9f),    // 라벤더
                _ => new Color(0.4f, 0.4f, 0.5f)        // 다크 그레이
            };
        }

        private void OnResumeClicked()
        {
            ShowPauseMenu(false);
            gameplayController?.ResumeGame();
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.ReturnToMenu();
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (quitButton != null) quitButton.onClick.RemoveAllListeners();
            if (retryButton != null) retryButton.onClick.RemoveAllListeners();
            if (menuButton != null) menuButton.onClick.RemoveAllListeners();
        }
    }
}
