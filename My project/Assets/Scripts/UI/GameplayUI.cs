using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Data;
using AIBeat.Core;
using AIBeat.Gameplay;
using AIBeat.Utils;

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
        [SerializeField] private Color perfectColor = new Color(1f, 0.84f, 0f);       // Gold
        [SerializeField] private Color greatColor = new Color(0f, 0.8f, 0.82f);       // Teal
        [SerializeField] private Color goodColor = new Color(0.4f, 0.8f, 0.2f);       // Warm Green
        [SerializeField] private Color badColor = new Color(1f, 0.55f, 0f);            // Orange
        [SerializeField] private Color missColor = new Color(0.8f, 0.1f, 0.1f);       // Red

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

        // 로딩 영상
        private GameObject loadingVideoPanel;
        private VideoPlayer videoPlayer;
        private RawImage videoDisplay;

        // 분석 오버레이 (전용)
        private GameObject analysisPanel;
        private Image analysisProgressBar;
        private Image analysisProgressGlow;
        private TMP_Text analysisStatusText;
        private TMP_Text analysisPercentText;
        private TMP_Text analysisSongText;
        private TMP_Text analysisTipText;
        private Image[] analysisEqBars;
        private Coroutine analysisAnimCoroutine;
        private Coroutine analysisEqCoroutine;
        private RenderTexture videoRenderTexture;

        // Visual Effects
        private JudgementEffectController effectControllerPrefab;
        private List<JudgementEffectController> effectPool = new List<JudgementEffectController>();


        private void Awake()
        {
            // TMP_Text 생성 전에 한국어 폰트를 글로벌 기본값으로 설정
            // (AddComponent<TextMeshProUGUI>() 시점에 LiberationSans SDF 경고 방지)
            var _ = KoreanFontManager.KoreanFont;

            EnsureCanvasScaler();
            AutoSetupReferences();
            CreateStatsHUD();
            CreateEarlyLateText();
            CreateBonusScoreText();
            CreatePauseButton();
            CreatePausePanel();
            CreateCountdownPanel();
            CreateAnalysisOverlay();
            // CreateLoadingVideoPanel(); // 필요할 때만 생성 (ShowLoadingVideo(true) 호출 시)
            CreateGameplayBackground(); // Cyberpunk 배경 활성화
            CreateLaneDividers();       // 레인 구분선 (3D)
            StartBackgroundAnimation(); // 배경 회전/펄스 애니메이션
            CreateHUDFrameOverlay();    // HUD 프레임 오버레이 (배경 위, HUD 아래)
            RepositionHUD();

            // 패널은 Awake에서 즉시 숨기기 (loadingVideoPanel은 더 이상 Awake에서 생성 안 함)
            if (analysisPanel != null) analysisPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (judgementText != null) judgementText.gameObject.SetActive(false);

            // Combo 초기 텍스트 비우기 (콤보 0일때 표시 안함)
            if (comboText != null) comboText.text = "";

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(gameObject);

            // HUD 프레임 오버레이 sibling 순서 조정: 배경(0) 바로 위, TopBar/StatsBar 아래
            var hudFrame = transform.Find("HUD_Frame_Overlay");
            if (hudFrame != null)
                hudFrame.SetSiblingIndex(1); // 0=배경, 1=프레임, 2+=HUD 요소들

            EnsureSafeArea(); // 모든 UI 셋업 후 마지막에 SafeArea 적용

            // Z-order 보장: SafeAreaApplier 이후에 설정해야 유효함
            // SafeAreaApplier가 모든 자식을 SafeAreaPanel로 이동시킴
            if (pauseButton != null) pauseButton.transform.SetAsLastSibling();
            if (pausePanel != null) pausePanel.transform.SetAsLastSibling();

            Debug.Log($"[GameplayUI] Awake complete - pauseBtn:{pauseButton != null}, pausePanel:{pausePanel != null}, gameplayCtrl:{gameplayController != null}");
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
            if (resultPanel == null)
            {
                resultPanel = CreateResultPanel();
            }
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

            Debug.Log($"[GameplayUI] AutoSetup - Score:{scoreText != null}, Combo:{comboText != null}, Judgement:{judgementText != null}, ResultPanel:{resultPanel != null}");
            
            // Setup Effect Controller Prefab (UI Image 기반 — Canvas 내부)
            if (effectControllerPrefab == null)
            {
                var go = new GameObject("JudgementEffect_Prefab");
                go.AddComponent<RectTransform>();
                effectControllerPrefab = go.AddComponent<JudgementEffectController>();
                go.SetActive(false);
                go.transform.SetParent(transform, false); // Canvas 내부에 배치
            }
        }


        /// <summary>
        /// 배경 이미지 — 3D SpriteRenderer로 노트 뒤에 배치 (UI Canvas 아래가 아님)
        /// Screen Space Overlay 캔버스의 UI Image는 3D 노트를 완전히 가리므로
        /// 3D 월드 공간에 배경을 배치해야 노트가 보임
        /// </summary>
        private void CreateGameplayBackground()
        {
            // 기존 UI 배경 제거 (있다면)
            var existingUI = transform.Find("Gameplay_Background");
            if (existingUI != null) Destroy(existingUI.gameObject);

            // 3D 배경 이미 존재하면 스킵
            var existing3D = GameObject.Find("Gameplay_Background_3D");
            if (existing3D != null) return;

            var cam = Camera.main;
            if (cam == null) return;

            Sprite bgSprite = ResourceHelper.LoadSpriteFromResources("AIBeat_Design/UI/Backgrounds/Gameplay_BG");

            var bgGo = new GameObject("Gameplay_Background_3D");
            // 노트(Z=-1)보다 뒤에 배치 (카메라에서 더 먼 쪽)
            bgGo.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f);

            var sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -100; // 최후방 렌더링

            if (bgSprite != null)
            {
                sr.sprite = bgSprite;
                Debug.Log("[GameplayUI] Loaded Gameplay_BG as 3D background");
            }
            else
            {
                sr.sprite = ProceduralImageGenerator.CreateCyberpunkBackground();
                sr.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Debug.Log("[GameplayUI] Using procedural 3D background (fallback)");
            }

            // 카메라 뷰를 꽉 채우도록 스케일 (Cover 방식)
            if (sr.sprite != null)
            {
                float worldHeight = cam.orthographicSize * 2f;
                float aspect = (float)Screen.width / Screen.height;
                float worldWidth = worldHeight * aspect;

                float spriteW = sr.sprite.bounds.size.x;
                float spriteH = sr.sprite.bounds.size.y;
                float scale = Mathf.Max(worldWidth / spriteW, worldHeight / spriteH) * 1.2f; // 20% 여유
                bgGo.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        /// <summary>
        /// 레인 시각화 — 반투명 스트립(교대 색조) + 엣지 라인 + 하단 글로우
        /// whiteSprite 기본 크기 = 4px / 100PPU = 0.04 world units
        /// → 원하는 월드 크기로 변환: scale = worldSize / 0.04 = worldSize * 25
        /// </summary>
        private void CreateLaneDividers()
        {
            if (GameObject.Find("LaneDividers") != null) return;

            var cam = Camera.main;
            if (cam == null) return;

            var parent = new GameObject("LaneDividers");
            parent.transform.position = Vector3.zero;

            const float laneWidth = 1.4f;
            const int laneCount = 4;
            const float S = 25f; // sprite→world 스케일 팩터 (1 / 0.04)
            float camY = cam.transform.position.y;
            float viewHeight = cam.orthographicSize * 2.5f; // 화면보다 약간 넓게
            float stripZ = 0f;   // 배경(Z=5)보다 앞, 노트(Z=-1)보다 뒤
            float lineZ = -0.5f; // 엣지 라인은 노트에 가깝게

            // 레인별 색상 (사이버펑크 네온)
            Color[] laneColors = {
                new Color(0.6f, 0.2f, 1f, 1f),   // Lane 0: 퍼플
                new Color(0f, 0.9f, 1f, 1f),      // Lane 1: 시안
                new Color(1f, 1f, 0f, 1f),         // Lane 2: 옐로우
                new Color(1f, 0.4f, 0f, 1f)        // Lane 3: 오렌지
            };

            var whiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f), 100f
            );

            // --- 1) 반투명 레인 스트립 (교대 명암) ---
            for (int i = 0; i < laneCount; i++)
            {
                float x = (i - 1.5f) * laneWidth;
                bool isDark = (i % 2 == 0);

                var stripGo = new GameObject($"LaneStrip_{i}");
                stripGo.transform.SetParent(parent.transform);
                stripGo.transform.position = new Vector3(x, camY, stripZ);
                stripGo.transform.localScale = new Vector3(laneWidth * S, viewHeight * S, 1f);

                var sr = stripGo.AddComponent<SpriteRenderer>();
                sr.sprite = whiteSprite;
                sr.sortingOrder = -90;
                sr.color = isDark
                    ? new Color(0f, 0f, 0.08f, 0.3f)
                    : new Color(0.06f, 0.06f, 0.15f, 0.18f);
            }

            // --- 2) 엣지 라인 (레인 경계 5개) ---
            for (int i = 0; i <= laneCount; i++)
            {
                float x = (i - laneCount / 2f) * laneWidth;

                var lineGo = new GameObject($"LaneLine_{i}");
                lineGo.transform.SetParent(parent.transform);
                lineGo.transform.position = new Vector3(x, camY, lineZ);
                lineGo.transform.localScale = new Vector3(0.05f * S, viewHeight * S, 1f);

                var sr = lineGo.AddComponent<SpriteRenderer>();
                sr.sprite = whiteSprite;
                sr.sortingOrder = -50;
                sr.color = new Color(0f, 0.8f, 1f, 0.35f);
            }

        }

        /// <summary>
        /// 배경 애니메이션 — 느린 회전 + 펄스 스케일 + 색상 사이클
        /// </summary>
        private void StartBackgroundAnimation()
        {
            var bg = GameObject.Find("Gameplay_Background_3D");
            if (bg == null) return;
            StartCoroutine(BackgroundAnimationLoop(bg.transform, bg.GetComponent<SpriteRenderer>()));
        }

        private IEnumerator BackgroundAnimationLoop(Transform bgTransform, SpriteRenderer bgSr)
        {
            // 카메라 배경색 검정 (빈 영역 방지)
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Color.black;

            float baseScale = bgTransform.localScale.x;
            Vector3 basePos = bgTransform.position;
            float time = 0f;

            while (bgTransform != null)
            {
                time += Time.deltaTime;

                // 1) 느린 수직 스크롤 (위로 천천히 흘러가는 느낌)
                float scrollY = Mathf.Sin(time * 0.3f) * 0.5f;
                bgTransform.position = basePos + new Vector3(0f, scrollY, 0f);

                // 2) 미세한 펄스 (±3%)
                float pulse = 1f + 0.03f * Mathf.Sin(time * 1.5f);
                float s = baseScale * pulse;
                bgTransform.localScale = new Vector3(s, s, 1f);

                // 3) 색상 사이클 (밝기 + 약간의 색조 변화)
                if (bgSr != null)
                {
                    float r = 0.85f + 0.10f * Mathf.Sin(time * 0.7f);
                    float g = 0.85f + 0.10f * Mathf.Sin(time * 0.7f + 0.5f);
                    float b = 0.90f + 0.10f * Mathf.Sin(time * 0.7f + 1.0f);
                    bgSr.color = new Color(r, g, b, 1f);
                }

                yield return null;
            }
        }

        /// <summary>
        /// HUD 네온 테두리 오버레이 — 프로그래밍으로 생성
        /// (Gameplay_BG.jpg는 JPG로 투명도 미지원 → 체크보드 문제 발생하므로 사용하지 않음)
        /// </summary>
        private void CreateHUDFrameOverlay()
        {
            var existing = transform.Find("HUD_Frame_Overlay");
            if (existing != null) return;

            var frameGo = new GameObject("HUD_Frame_Overlay");
            frameGo.transform.SetParent(transform, false);

            var rect = frameGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 상/하 네온 테두리만 (좌/우는 레인 구분선이 대체)
            Color neonCyan = new Color(0f, 0.85f, 0.9f, 0.4f);
            float borderWidth = 2f;

            CreateBorderLine(frameGo.transform, "TopBorder", neonCyan, borderWidth,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, borderWidth));
            CreateBorderLine(frameGo.transform, "BottomBorder", neonCyan, borderWidth,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, borderWidth));
        }

        private void CreateBorderLine(Transform parent, string name, Color color, float width,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot = pivot;
            r.anchoredPosition = anchoredPos;
            r.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void CreateCornerGlow(Transform parent, string name, Color color, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = pivot;
            r.sizeDelta = new Vector2(30, 30);
            r.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 0.3f);
            img.raycastTarget = false;
        }

        /// <summary>
        /// HUD 리디자인 — unnamed.jpg 레퍼런스 기반
        /// 상단 1행: [곡명(녹색)] [LED 점수] [COMBO] [||일시정지(마젠타)]
        /// 상단 2행: [P 0][G 0][OK 0][B 0][M 0] (StatsHUD에서 별도 처리)
        /// </summary>
        private void RepositionHUD()
        {
            // ============================================================
            // === 메인 TopBar (전체 너비, 높이 100px) ===
            // ============================================================
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(transform, false);
            var topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(0, 100); // 80 → 100 (더 크게)

            // 네온 그라데이션 배경 (위: 어두운 보라, 아래: 시안 힌트)
            var topBarBg = topBar.AddComponent<Image>();
            topBarBg.color = new Color(0.02f, 0.01f, 0.05f, 0.92f);
            topBarBg.raycastTarget = false;

            // 하단 네온 글로우 라인
            var glowLine = new GameObject("TopBarGlow");
            glowLine.transform.SetParent(topBar.transform, false);
            var glowRect = glowLine.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0, 0);
            glowRect.anchorMax = new Vector2(1, 0);
            glowRect.pivot = new Vector2(0.5f, 0);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(0, 3);
            var glowImg = glowLine.AddComponent<Image>();
            glowImg.color = new Color(0f, 0.85f, 0.9f, 0.7f); // 시안 글로우
            glowImg.raycastTarget = false;

            // HorizontalLayoutGroup
            var hLayout = topBar.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(6, 6, 4, 4);
            hLayout.spacing = 4;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // --- 곡명 (좌측, 시안, 크게) ---
            if (songTitleText != null)
            {
                songTitleText.transform.SetParent(topBar.transform, false);
                var titleLE = songTitleText.gameObject.AddComponent<LayoutElement>();
                titleLE.preferredWidth = 160;
                songTitleText.fontSize = 28; // 24 → 28 (더 크게)
                songTitleText.alignment = TextAlignmentOptions.MidlineLeft;
                songTitleText.color = UIColorPalette.NEON_CYAN_BRIGHT;
                songTitleText.fontStyle = FontStyles.Bold;
                songTitleText.textWrappingMode = TextWrappingModes.Normal;
                songTitleText.overflowMode = TextOverflowModes.Ellipsis;
                songTitleText.maxVisibleLines = 2;
            }

            // --- 점수 (중앙, LED 도트 매트릭스 느낌) ---
            if (scoreText != null)
            {
                var scorePanel = scoreText.transform.parent;
                if (scorePanel != null && scorePanel.name == "ScorePanel")
                {
                    var labelTransform = scorePanel.Find("ScoreLabel");
                    if (labelTransform != null)
                        labelTransform.gameObject.SetActive(false);

                    // 점수 패널 배경: 어두운 사각형
                    var panelImage = scorePanel.GetComponent<Image>();
                    if (panelImage != null)
                        panelImage.color = new Color(0.02f, 0.02f, 0.05f, 0.8f);
                    var panelOutline = scorePanel.GetComponent<Outline>();
                    if (panelOutline != null) Destroy(panelOutline);

                    scorePanel.SetParent(topBar.transform, false);
                    var scoreLE = scorePanel.gameObject.AddComponent<LayoutElement>();
                    scoreLE.flexibleWidth = 1;
                }

                scoreText.fontSize = 52; // 38 → 52 (더 크게)
                scoreText.alignment = TextAlignmentOptions.Center;
                scoreText.color = UIColorPalette.NEON_GOLD; // 네온 골드
                scoreText.fontStyle = FontStyles.Bold;
                scoreText.characterSpacing = 4f; // LED 느낌 글자 간격
                var korFont1 = KoreanFontManager.KoreanFont;
                if (korFont1 != null) scoreText.font = korFont1;
                // scoreText.outlineWidth = 0.12f; // Dynamic SDF에서 outline 비활성화
                // scoreText.outlineColor = new Color32(0, 200, 255, 140);
            }

            // --- COMBO (우측, 2줄: "COMBO" 라벨 + 숫자) ---
            if (comboText != null)
            {
                comboText.transform.SetParent(topBar.transform, false);
                var comboLE = comboText.gameObject.AddComponent<LayoutElement>();
                comboLE.preferredWidth = 120;
                comboText.fontSize = 48; // 40 → 48 (더 크게)
                comboText.alignment = TextAlignmentOptions.Center;
                comboText.color = Color.white;
                comboText.fontStyle = FontStyles.Bold;
                comboText.text = "";
            }

            // --- 일시정지 버튼: TopBar와 독립, 화면 우측 상단 고정 ---
            // (CreatePauseButton에서 앵커로 절대 배치됨 → reparent 불필요)

            // ============================================================
            // === JudgementText → 판정선 약간 위 (하단 30% 지점) ===
            // ============================================================
            if (judgementText != null)
            {
                var judgeRect = judgementText.GetComponent<RectTransform>();
                if (judgeRect != null)
                {
                    judgeRect.anchorMin = new Vector2(0.5f, 0.30f);
                    judgeRect.anchorMax = new Vector2(0.5f, 0.30f);
                    judgeRect.anchoredPosition = new Vector2(0, 0);
                    judgeRect.sizeDelta = new Vector2(600, 70);
                }
                judgementText.fontSize = 56;
                judgementText.fontStyle = FontStyles.Bold;
                var korFont2 = KoreanFontManager.KoreanFont;
                if (korFont2 != null) judgementText.font = korFont2;
                // judgementText.outlineWidth = 0.25f; // Dynamic SDF에서 outline 비활성화
                // judgementText.outlineColor = new Color32(0, 0, 0, 220);
            }
        }

        /// <summary>
        /// 모바일용 일시정지 버튼 — 마젠타 배경, TopBar 내부 배치용
        /// </summary>
        private void CreatePauseButton()
        {
            var pauseBtnGo = new GameObject("PauseButton");
            pauseBtnGo.transform.SetParent(transform, false);

            // 화면 우측 상단 절대 위치 (TopBar와 독립)
            var btnRect = pauseBtnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-10, -10); // 우측 상단에서 10px 여백
            btnRect.sizeDelta = new Vector2(100, 100);

            var btnImage = pauseBtnGo.AddComponent<Image>();
            btnImage.color = new Color(0.9f, 0.15f, 0.3f, 0.95f); // 밝은 레드-핑크 (고가시성)
            btnImage.raycastTarget = true;

            // "✕" 아이콘 (흰색, 큰 폰트)
            var textGo = new GameObject("PauseIcon");
            textGo.transform.SetParent(pauseBtnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var iconText = textGo.AddComponent<TextMeshProUGUI>();
            iconText.text = "✕";
            iconText.fontSize = 56;
            iconText.color = Color.white;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.fontStyle = FontStyles.Bold;
            iconText.raycastTarget = false;

            pauseButton = pauseBtnGo.AddComponent<Button>();
            pauseButton.targetGraphic = btnImage;
            pauseButton.onClick.AddListener(() =>
            {
                Debug.Log($"[GameplayUI] ✕ button clicked! gameplayCtrl={gameplayController != null}");
                if (gameplayController == null)
                    gameplayController = FindFirstObjectByType<GameplayController>();
                if (gameplayController != null)
                    gameplayController.PauseGame();
                else
                    Debug.LogError("[GameplayUI] GameplayController NOT found!");
            });
        }

        /// <summary>
        /// 일시정지 메뉴 패널 동적 생성 (이어하기 / 다시하기 / 나가기)
        /// </summary>
        private void CreatePausePanel()
        {
            if (pausePanel != null) return; // 씬에 이미 있으면 스킵

            pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(transform, false);

            var panelRect = pausePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 반투명 배경 (전체 화면 덮기)
            var bg = pausePanel.AddComponent<Image>();
            bg.color = UIColorPalette.BG_DEEP.WithAlpha(0.85f);
            bg.raycastTarget = true; // 뒤의 터치 차단

            // 세로 레이아웃 컨테이너 (가운데 정렬)
            var contentGo = new GameObject("PauseContent");
            contentGo.transform.SetParent(pausePanel.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.15f, 0.25f);
            contentRect.anchorMax = new Vector2(0.85f, 0.75f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 30;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(20, 20, 20, 20);

            // 타이틀: "일시정지"
            var titleGo = new GameObject("PauseTitle");
            titleGo.transform.SetParent(contentGo.transform, false);
            var titleLE = titleGo.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 80;
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "일시정지";
            titleText.fontSize = 60;
            titleText.color = UIColorPalette.NEON_CYAN_BRIGHT;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // 버튼들 생성
            resumeButton = CreatePauseMenuButton(contentGo.transform, "ResumeButton", "이어하기");
            restartButton = CreatePauseMenuButton(contentGo.transform, "RestartButton", "다시하기");
            quitButton = CreatePauseMenuButton(contentGo.transform, "QuitButton", "나가기");

            pausePanel.SetActive(false);
        }

        /// <summary>
        /// 일시정지 메뉴 버튼 생성 헬퍼 (디자인 에셋 사용)
        /// </summary>
        private Button CreatePauseMenuButton(Transform parent, string name, string label)
        {
            return UIButtonStyleHelper.CreateStyledButton(parent, name, label,
                preferredHeight: 80f, fontSize: 36f);
        }

        /// <summary>
        /// 카운트다운 패널 동적 생성 (화면 정중앙)
        /// </summary>
        private void CreateCountdownPanel()
        {
            if (countdownPanel != null) return; // 씬에 이미 있으면 스킵

            countdownPanel = new GameObject("CountdownPanel");
            countdownPanel.transform.SetParent(transform, false);

            var panelRect = countdownPanel.AddComponent<RectTransform>();
            // 화면 정중앙에 배치
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 400);

            // 반투명 배경
            var bg = countdownPanel.AddComponent<Image>();
            bg.color = UIColorPalette.BG_DEEP.WithAlpha(0.75f);

            // 카운트다운 텍스트
            var textGo = new GameObject("CountdownText");
            textGo.transform.SetParent(countdownPanel.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            countdownText = textGo.AddComponent<TextMeshProUGUI>();
            countdownText.text = "";
            countdownText.fontSize = 180;
            countdownText.color = UIColorPalette.NEON_CYAN_BRIGHT;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.fontStyle = FontStyles.Bold;
            var korFont3 = KoreanFontManager.KoreanFont;
            if (korFont3 != null) countdownText.font = korFont3;
            // countdownText.outlineWidth = 0.2f; // Dynamic SDF에서 outline 비활성화
            // countdownText.outlineColor = new Color32(0, 120, 255, 200);
        }

        /// <summary>
        /// 분석 오버레이 패널 생성 (전체 화면 - 카운트다운과 분리)
        /// </summary>
        private void CreateAnalysisOverlay()
        {
            if (analysisPanel != null) return;

            var korFont = KoreanFontManager.KoreanFont;

            // 전체 화면 패널
            analysisPanel = new GameObject("AnalysisOverlay");
            analysisPanel.transform.SetParent(transform, false);
            var panelRect = analysisPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 배경 (어두운 오버레이)
            var bg = analysisPanel.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.005f, 0.03f, 0.92f);
            bg.raycastTarget = true; // 뒤 UI 클릭 차단

            // ── 상단: 곡 제목 (anchor 0.3~0.5 영역) ──
            var songGo = new GameObject("AnalysisSongTitle");
            songGo.transform.SetParent(analysisPanel.transform, false);
            var songRect = songGo.AddComponent<RectTransform>();
            songRect.anchorMin = new Vector2(0.05f, 0.62f);
            songRect.anchorMax = new Vector2(0.95f, 0.72f);
            songRect.offsetMin = Vector2.zero;
            songRect.offsetMax = Vector2.zero;
            analysisSongText = songGo.AddComponent<TextMeshProUGUI>();
            analysisSongText.text = "";
            analysisSongText.fontSize = 32;
            analysisSongText.color = UIColorPalette.NEON_GOLD;
            analysisSongText.alignment = TextAlignmentOptions.Center;
            analysisSongText.fontStyle = FontStyles.Bold;
            analysisSongText.raycastTarget = false;
            if (korFont != null) analysisSongText.font = korFont;

            // ── 중앙: 상태 텍스트 "분석 중..." ──
            var statusGo = new GameObject("AnalysisStatusText");
            statusGo.transform.SetParent(analysisPanel.transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.48f);
            statusRect.anchorMax = new Vector2(0.95f, 0.60f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            analysisStatusText = statusGo.AddComponent<TextMeshProUGUI>();
            analysisStatusText.text = "분석 중...";
            analysisStatusText.fontSize = 36;
            analysisStatusText.color = new Color(0.7f, 0.75f, 0.9f, 0.9f);
            analysisStatusText.alignment = TextAlignmentOptions.Center;
            analysisStatusText.raycastTarget = false;
            if (korFont != null) analysisStatusText.font = korFont;

            // ── 프로그레스 바 영역 (anchor 0.38~0.46) ──
            // 바 배경 (외곽선)
            var barBgGo = new GameObject("AnalysisBarBg");
            barBgGo.transform.SetParent(analysisPanel.transform, false);
            var barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.1f, 0.40f);
            barBgRect.anchorMax = new Vector2(0.9f, 0.44f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.15f, 0.12f, 0.25f, 0.8f);
            barBgImg.raycastTarget = false;

            // 바 외곽선
            var outlineGo = new GameObject("AnalysisBarOutline");
            outlineGo.transform.SetParent(barBgGo.transform, false);
            var outlineRect = outlineGo.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outline = outlineGo.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.5f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);
            var outlineImg = outlineGo.AddComponent<Image>();
            outlineImg.color = Color.clear;
            outlineImg.raycastTarget = false;

            // 프로그레스 바 (채우기)
            var barFillGo = new GameObject("AnalysisBarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.anchorMin = new Vector2(0f, 0.1f);
            barFillRect.anchorMax = new Vector2(0f, 0.9f);
            barFillRect.pivot = new Vector2(0f, 0.5f);
            barFillRect.offsetMin = new Vector2(4f, 0f);
            barFillRect.offsetMax = new Vector2(4f, 0f);
            analysisProgressBar = barFillGo.AddComponent<Image>();
            analysisProgressBar.color = UIColorPalette.NEON_CYAN;
            analysisProgressBar.raycastTarget = false;

            // 글로우 이펙트
            var glowGo = new GameObject("AnalysisBarGlow");
            glowGo.transform.SetParent(barFillGo.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(1f, -1f);
            glowRect.anchorMax = new Vector2(1f, 2f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(30f, 0f);
            analysisProgressGlow = glowGo.AddComponent<Image>();
            analysisProgressGlow.color = UIColorPalette.NEON_CYAN.WithAlpha(0.5f);
            analysisProgressGlow.raycastTarget = false;

            // ── 퍼센트 텍스트 ──
            var pctGo = new GameObject("AnalysisPercentText");
            pctGo.transform.SetParent(analysisPanel.transform, false);
            var pctRect = pctGo.AddComponent<RectTransform>();
            pctRect.anchorMin = new Vector2(0.1f, 0.33f);
            pctRect.anchorMax = new Vector2(0.9f, 0.40f);
            pctRect.offsetMin = Vector2.zero;
            pctRect.offsetMax = Vector2.zero;
            analysisPercentText = pctGo.AddComponent<TextMeshProUGUI>();
            analysisPercentText.text = "0%";
            analysisPercentText.fontSize = 28;
            analysisPercentText.color = UIColorPalette.NEON_CYAN_BRIGHT;
            analysisPercentText.alignment = TextAlignmentOptions.Center;
            analysisPercentText.fontStyle = FontStyles.Bold;
            analysisPercentText.raycastTarget = false;
            if (korFont != null) analysisPercentText.font = korFont;

            // ── 이퀄라이저 바 (하단) ──
            int barCount = 16;
            analysisEqBars = new Image[barCount];
            var eqContainer = new GameObject("AnalysisEqualizer");
            eqContainer.transform.SetParent(analysisPanel.transform, false);
            var eqRect = eqContainer.AddComponent<RectTransform>();
            eqRect.anchorMin = new Vector2(0.05f, 0.08f);
            eqRect.anchorMax = new Vector2(0.95f, 0.28f);
            eqRect.offsetMin = Vector2.zero;
            eqRect.offsetMax = Vector2.zero;

            float barWidth = 1f / barCount;
            float gap = 0.003f;
            for (int i = 0; i < barCount; i++)
            {
                var barGo = new GameObject($"EqBar_{i}");
                barGo.transform.SetParent(eqContainer.transform, false);
                var bRect = barGo.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(i * barWidth + gap, 0f);
                bRect.anchorMax = new Vector2((i + 1) * barWidth - gap, 0.2f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                analysisEqBars[i] = barGo.AddComponent<Image>();
                float t = (float)i / (barCount - 1);
                Color barColor = t < 0.5f
                    ? Color.Lerp(UIColorPalette.NEON_PURPLE, UIColorPalette.NEON_CYAN, t * 2f)
                    : Color.Lerp(UIColorPalette.NEON_CYAN, UIColorPalette.NEON_GOLD, (t - 0.5f) * 2f);
                analysisEqBars[i].color = barColor.WithAlpha(0.7f);
                analysisEqBars[i].raycastTarget = false;
            }

            // ── 하단 팁 텍스트 ──
            var tipGo = new GameObject("AnalysisTipText");
            tipGo.transform.SetParent(analysisPanel.transform, false);
            var tipRect = tipGo.AddComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.05f, 0.02f);
            tipRect.anchorMax = new Vector2(0.95f, 0.08f);
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;
            analysisTipText = tipGo.AddComponent<TextMeshProUGUI>();
            analysisTipText.fontSize = 16;
            analysisTipText.color = new Color(0.5f, 0.55f, 0.7f, 0.6f);
            analysisTipText.alignment = TextAlignmentOptions.Center;
            analysisTipText.raycastTarget = false;
            if (korFont != null) analysisTipText.font = korFont;
        }

        // 분석 오버레이 팁 목록
        private static readonly string[] ANALYSIS_TIPS = new string[]
        {
            "AI가 오디오를 분석하여 노트를 배치합니다",
            "BPM과 비트를 감지하여 리듬에 맞는 패턴을 생성합니다",
            "곡의 강약에 따라 노트 밀도가 달라집니다",
            "PERFECT 판정은 \u00b150ms 이내입니다",
            "콤보를 유지하면 점수 보너스!",
            "노트가 판정선에 닿을 때 터치하세요",
            "리듬에 집중하면 높은 점수를 얻을 수 있어요"
        };

        /// <summary>
        /// 분석 오버레이 표시
        /// </summary>
        public void ShowAnalysisOverlay(bool show)
        {
            if (analysisPanel == null) return;

            if (show)
            {
                analysisPanel.SetActive(true);
                analysisPanel.transform.SetAsLastSibling();
                UpdateAnalysisProgress(0f);

                // 랜덤 팁
                if (analysisTipText != null)
                    analysisTipText.text = $"TIP: {ANALYSIS_TIPS[Random.Range(0, ANALYSIS_TIPS.Length)]}";

                // 애니메이션 시작
                if (analysisAnimCoroutine != null) StopCoroutine(analysisAnimCoroutine);
                analysisAnimCoroutine = StartCoroutine(AnalysisAnimationLoop());
                if (analysisEqCoroutine != null) StopCoroutine(analysisEqCoroutine);
                analysisEqCoroutine = StartCoroutine(AnalysisEqualizerLoop());
            }
            else
            {
                if (analysisAnimCoroutine != null) { StopCoroutine(analysisAnimCoroutine); analysisAnimCoroutine = null; }
                if (analysisEqCoroutine != null) { StopCoroutine(analysisEqCoroutine); analysisEqCoroutine = null; }
                analysisPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 분석 오버레이에 곡 제목 설정
        /// </summary>
        public void SetAnalysisSongTitle(string title)
        {
            if (analysisSongText != null)
                analysisSongText.text = title ?? "";
        }

        /// <summary>
        /// 분석 진행률 업데이트 (0~1)
        /// </summary>
        public void UpdateAnalysisProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            int pct = Mathf.RoundToInt(progress * 100);

            if (analysisPercentText != null)
                analysisPercentText.text = $"{pct}%";

            if (analysisProgressBar != null)
            {
                var rect = analysisProgressBar.rectTransform;
                rect.anchorMax = new Vector2(progress, 0.9f);
                rect.offsetMax = new Vector2(-4f, 0f);
            }

            // 상태 텍스트 업데이트
            if (analysisStatusText != null)
            {
                int dots = (int)(Time.unscaledTime * 2f) % 4;
                string dotStr = new string('.', dots);
                analysisStatusText.text = $"AI 분석 중{dotStr}";
            }
        }

        /// <summary>
        /// 분석 오버레이 이퀄라이저 애니메이션
        /// </summary>
        private IEnumerator AnalysisEqualizerLoop()
        {
            if (analysisEqBars == null) yield break;

            float[] phases = new float[analysisEqBars.Length];
            float[] speeds = new float[analysisEqBars.Length];
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = Random.Range(0f, Mathf.PI * 2f);
                speeds[i] = Random.Range(2f, 5f);
            }

            while (true)
            {
                float time = Time.unscaledTime;
                for (int i = 0; i < analysisEqBars.Length; i++)
                {
                    float height = 0.15f + 0.85f * Mathf.Abs(Mathf.Sin(time * speeds[i] + phases[i]));
                    var bRect = analysisEqBars[i].rectTransform;
                    bRect.anchorMax = new Vector2(bRect.anchorMax.x, height);
                }
                yield return null;
            }
        }

        /// <summary>
        /// 분석 오버레이 글로우/상태 텍스트 애니메이션
        /// </summary>
        private IEnumerator AnalysisAnimationLoop()
        {
            while (true)
            {
                float time = Time.unscaledTime;

                // 글로우 펄스
                if (analysisProgressGlow != null)
                {
                    float glowAlpha = 0.3f + 0.4f * Mathf.Abs(Mathf.Sin(time * 3f));
                    analysisProgressGlow.color = UIColorPalette.NEON_CYAN.WithAlpha(glowAlpha);
                }

                // 상태 텍스트 점 애니메이션
                if (analysisStatusText != null)
                {
                    int dots = (int)(time * 2f) % 4;
                    string dotStr = new string('.', dots);
                    analysisStatusText.text = $"AI 분석 중{dotStr}";
                }

                yield return null;
            }
        }

        /// <summary>
        /// 로딩 영상 패널 생성 (VideoPlayer + RawImage)
        /// 곡 선택 후 오디오 분석 중 전체 화면으로 영상 재생
        /// </summary>
        private void CreateLoadingVideoPanel()
        {
            if (loadingVideoPanel != null) return;

            // 패널: 전체 화면 커버
            loadingVideoPanel = new GameObject("LoadingVideoPanel");
            loadingVideoPanel.transform.SetParent(transform, false);

            var panelRect = loadingVideoPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 검정 배경 (영상 로드 전/비율 불일치 시)
            var bg = loadingVideoPanel.AddComponent<Image>();
            bg.color = Color.black;
            bg.raycastTarget = false;

            // RawImage: 영상 표시용
            var displayGo = new GameObject("VideoDisplay");
            displayGo.transform.SetParent(loadingVideoPanel.transform, false);
            var displayRect = displayGo.AddComponent<RectTransform>();
            displayRect.anchorMin = Vector2.zero;
            displayRect.anchorMax = Vector2.one;
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;

            videoDisplay = displayGo.AddComponent<RawImage>();
            videoDisplay.color = Color.white;
            videoDisplay.raycastTarget = false;

            // RenderTexture 생성 (영상 출력용)
            videoRenderTexture = new RenderTexture(1920, 1080, 0);
            videoRenderTexture.Create();
            videoDisplay.texture = videoRenderTexture;

            // VideoPlayer 컴포넌트 (패널에 부착)
            videoPlayer = loadingVideoPanel.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // 음소거

            // 영상 파일 경로 설정
            videoPlayer.source = VideoSource.Url;
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Videos", "loading_video.mp4");
#if UNITY_ANDROID && !UNITY_EDITOR
            videoPlayer.url = videoPath; // Android: jar:file:// 자동 포함
#else
            videoPlayer.url = "file://" + videoPath;
#endif

            // 카운트다운 패널보다 뒤에 위치 (카운트다운이 위에 표시)
            loadingVideoPanel.transform.SetSiblingIndex(0);

            Debug.Log($"[GameplayUI] Loading video panel created, URL: {videoPlayer.url}");
        }

        /// <summary>
        /// 로딩 영상 표시/숨김
        /// TODO: 로딩 비디오 기능 임시 비활성화 - 게임 중 겹침 버그로 인해
        /// </summary>
        public void ShowLoadingVideo(bool show)
        {
            // 로딩 비디오 기능 비활성화 - 분석 함수 중복 호출 문제 해결 전까지
            Debug.Log($"[GameplayUI] ShowLoadingVideo({show}) - DISABLED");
            return;
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
            var korFontEL = KoreanFontManager.KoreanFont;
            if (korFontEL != null) earlyLateText.font = korFontEL;
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
            rect.anchoredPosition = new Vector2(15, -175);
            rect.sizeDelta = new Vector2(400f, 60f);

            bonusScoreText = go.AddComponent<TextMeshProUGUI>();
            bonusScoreText.fontSize = 40;
            bonusScoreText.alignment = TextAlignmentOptions.Left;
            bonusScoreText.fontStyle = FontStyles.Bold;
            bonusScoreText.color = new Color(1f, 0.85f, 0.2f, 1f); // 골드
            var korFont4 = KoreanFontManager.KoreanFont;
            if (korFont4 != null) bonusScoreText.font = korFont4;
            // bonusScoreText.outlineWidth = 0.15f; // Dynamic SDF에서 outline 비활성화
            // bonusScoreText.outlineColor = new Color32(0, 0, 0, 180);
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
            bonusScoreText.text = $"보너스 +{totalBonus}";
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
        /// 판정 통계 HUD — unnamed.jpg 레퍼런스 기반
        /// TopBar 바로 아래, 각 셀에 고유 색상 배경
        /// [P 0] [G 0] [OK 0] [B 0] [M 0]
        /// </summary>
        private void CreateStatsHUD()
        {
            // 씬에 남아있는 기존 판정 텍스트 오브젝트 제거 (중복 방지)
            string[] legacyNames = { "PerfectText", "GreatText", "GoodText", "BadText", "MissText" };
            foreach (var name in legacyNames)
            {
                var legacy = transform.Find(name);
                if (legacy != null) Destroy(legacy.gameObject);
            }
            statsPanel = new GameObject("StatsBar");
            statsPanel.transform.SetParent(transform, false);

            var statsRect = statsPanel.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1);
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.pivot = new Vector2(0.5f, 1);
            statsRect.anchoredPosition = new Vector2(0, -100); // TopBar(100px) 바로 아래
            statsRect.sizeDelta = new Vector2(0, 60); // 50 → 60 (더 크게)

            // 배경 (더 투명하게)
            var bgImage = statsPanel.AddComponent<Image>();
            bgImage.color = new Color(0.01f, 0.005f, 0.03f, 0.65f);
            bgImage.raycastTarget = false;

            // 하단 네온 글로우 라인 (마젠타)
            var statsGlow = new GameObject("StatsBarGlow");
            statsGlow.transform.SetParent(statsPanel.transform, false);
            var statsGlowRect = statsGlow.AddComponent<RectTransform>();
            statsGlowRect.anchorMin = new Vector2(0, 0);
            statsGlowRect.anchorMax = new Vector2(1, 0);
            statsGlowRect.pivot = new Vector2(0.5f, 0);
            statsGlowRect.anchoredPosition = Vector2.zero;
            statsGlowRect.sizeDelta = new Vector2(0, 2);
            var statsGlowImg = statsGlow.AddComponent<Image>();
            statsGlowImg.color = new Color(0.9f, 0.2f, 0.8f, 0.5f); // 마젠타 글로우
            statsGlowImg.raycastTarget = false;

            // HorizontalLayoutGroup
            var layout = statsPanel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // 판정별 색상 셀 생성
            statsPerfectText = CreateStatsCell(statsPanel.transform, "P", "0", perfectColor, UIColorPalette.STATS_BG_PERFECT);
            statsGreatText = CreateStatsCell(statsPanel.transform, "G", "0", greatColor, UIColorPalette.STATS_BG_GREAT);
            statsGoodText = CreateStatsCell(statsPanel.transform, "OK", "0", goodColor, UIColorPalette.STATS_BG_GOOD);
            statsBadText = CreateStatsCell(statsPanel.transform, "B", "0", badColor, UIColorPalette.STATS_BG_BAD);
            statsMissText = CreateStatsCell(statsPanel.transform, "M", "0", missColor, UIColorPalette.STATS_BG_MISS);
        }

        /// <summary>
        /// 판정 통계 셀 — 고유 색상 배경 + 라벨 + 카운트
        /// </summary>
        private TMP_Text CreateStatsCell(Transform parent, string label, string value, Color textColor, Color bgColor)
        {
            var cell = new GameObject($"Stats_{label}");
            cell.transform.SetParent(parent, false);

            // 셀 배경 (각 판정의 고유 색상)
            var cellBg = cell.AddComponent<Image>();
            cellBg.color = bgColor;
            cellBg.raycastTarget = false;

            // 텍스트
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(cell.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{label}</color>{value}";
            tmp.fontSize = 34; // 28 → 34 (더 크게)
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            var korFontCell = KoreanFontManager.KoreanFont;
            if (korFontCell != null) tmp.font = korFontCell;

            return tmp;
        }

        /// <summary>
        /// 판정 통계 HUD 업데이트
        /// </summary>
        private void UpdateStatsHUD()
        {
            if (judgementSystem == null) return;

            UpdateStatsCell(statsPerfectText, "P", judgementSystem.PerfectCount, perfectColor);
            UpdateStatsCell(statsGreatText, "G", judgementSystem.GreatCount, greatColor);
            UpdateStatsCell(statsGoodText, "OK", judgementSystem.GoodCount, goodColor);
            UpdateStatsCell(statsBadText, "B", judgementSystem.BadCount, badColor);
            UpdateStatsCell(statsMissText, "M", judgementSystem.MissCount, missColor);
        }

        private void UpdateStatsCell(TMP_Text cell, string label, int count, Color color)
        {
            if (cell == null) return;
            cell.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{label}</color> {count}";
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
        /// 버튼에 한국어 텍스트 + 디자인 에셋 스타일 적용
        /// </summary>
        private void ApplyButtonStyle(Button btn, string koreanText)
        {
            UIButtonStyleHelper.ApplyDesignStyle(btn, koreanText, fontSize: 30f);
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

        private int lastDisplayedScore;

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0");

                // 점수 변화 시 글로우 펄스
                if (score > lastDisplayedScore && score > 0)
                {
                    scoreText.transform.localScale = Vector3.one * 1.05f;
                    UIAnimator.ScaleTo(this, scoreText.gameObject, Vector3.one, 0.12f);
                }
                lastDisplayedScore = score;
            }
        }

        public void UpdateCombo(int combo)
        {
            if (comboText != null)
            {
                // 네온 스타일: "COMBO" 라벨 (시안) + 숫자
                if (combo <= 0)
                {
                    comboText.text = "<size=12><color=#00D4D8>COMBO</color></size>\n<size=26>-</size>";
                }
                else
                {
                    comboText.text = $"<size=12><color=#00D4D8>COMBO</color></size>\n<size=32>{combo}</size>";
                }

                // 콤보 색상 단계
                if (combo >= 100)
                {
                    comboText.color = UIColorPalette.COMBO_100;
                }
                else if (combo >= 50)
                {
                    comboText.color = UIColorPalette.COMBO_50;
                }
                else if (combo >= 25)
                {
                    comboText.color = UIColorPalette.COMBO_25;
                }
                else if (combo >= 10)
                {
                    comboText.color = UIColorPalette.COMBO_10;
                }
                else
                {
                    comboText.color = Color.white;
                }

                // 100콤보 마일스톤: 최대 펄스 + 화면 플래시 + 카메라 쉐이크
                if (combo > 0 && combo % 100 == 0)
                {
                    comboText.transform.localScale = Vector3.one * 1.8f;
                    UIAnimator.ScaleTo(this, comboText.gameObject, Vector3.one, 0.4f);
                    UIAnimator.CameraShake(this, 0.08f, 0.25f);
                    UIAnimator.ScreenBorderFlash(this, transform, comboText.color, 0.4f);
                }
                // 50콤보 마일스톤: 큰 펄스 + 카메라 쉐이크
                else if (combo > 0 && combo % 50 == 0)
                {
                    comboText.transform.localScale = Vector3.one * 1.5f;
                    UIAnimator.ScaleTo(this, comboText.gameObject, Vector3.one, 0.35f);
                    UIAnimator.CameraShake(this, 0.05f, 0.15f);
                }
                // 10콤보 마일스톤: 중간 펄스
                else if (combo > 0 && combo % 10 == 0)
                {
                    comboText.transform.localScale = Vector3.one * 1.25f;
                    UIAnimator.ScaleTo(this, comboText.gameObject, Vector3.one, 0.2f);
                }
                // 매 콤보 작은 바운스
                else if (combo > 0)
                {
                    comboText.transform.localScale = Vector3.one * 1.08f;
                    UIAnimator.ScaleTo(this, comboText.gameObject, Vector3.one, 0.1f);
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
                JudgementResult.Perfect => ("PERFECT!", perfectColor),
                JudgementResult.Great => ("GREAT!", greatColor),
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
                    earlyLateText.text = isLate ? "느림" : "빠름";
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
                JudgementResult.Perfect => 1.6f,  // 더 강렬한 펄스
                JudgementResult.Great => 1.35f,
                JudgementResult.Good => 1.15f,
                JudgementResult.Bad => 1.1f,
                _ => 0.85f // Miss: 축소 등장
            };
            judgementText.transform.localScale = Vector3.one * startScale;
            UIAnimator.ScaleTo(this, judgementText.gameObject, Vector3.one, 0.15f);

            judgementCoroutine = StartCoroutine(HideJudgement());

            // 판정 통계 HUD 업데이트
            UpdateStatsHUD();

            // Spawn Visual Effect
            if (result != JudgementResult.Miss)
            {
                string assetName = result.ToString(); // "Perfect", "Great", ...
                SpawnEffect(assetName);
            }

            // Camera Shake — Perfect 판정 시 미세한 흔들림
            if (result == JudgementResult.Perfect)
            {
                UIAnimator.CameraShake(this, 0.03f, 0.08f);
            }

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
            Debug.Log($"[GameplayUI] ShowPauseMenu({show}), pausePanel:{pausePanel != null}");
            if (pausePanel != null)
            {
                pausePanel.SetActive(show);
                if (show)
                {
                    // 최상위에 렌더링되도록 보장
                    pausePanel.transform.SetAsLastSibling();
                }
            }
            // 일시정지 중에는 버튼 숨김
            if (pauseButton != null)
                pauseButton.gameObject.SetActive(!show);
        }

        /// <summary>
        /// ResultPanel이 없을 때 동적으로 생성
        /// </summary>
        private GameObject CreateResultPanel()
        {
            var panelGo = new GameObject("ResultPanel");
            panelGo.transform.SetParent(transform, false);

            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 배경 - AI 디자인 이미지 (콘서트 무대) 또는 어두운 불투명 폴백
            var bg = panelGo.AddComponent<Image>();
            Sprite resultBgSprite = ResourceHelper.LoadSpriteFromResources("AIBeat_Design/UI/Backgrounds/Result_BG");
            if (resultBgSprite != null)
            {
                bg.sprite = resultBgSprite;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = false;
                bg.color = new Color(0.7f, 0.7f, 0.7f, 1f); // 살짝 어둡게 (텍스트 가독성)
                Debug.Log("[GameplayUI] Loaded Result_BG as result background");
            }
            else
            {
                bg.color = new Color(0.04f, 0.06f, 0.12f, 1f);
            }
            bg.raycastTarget = true;

            var korFont = KoreanFontManager.KoreanFont;

            // ===== "RESULT" 타이틀 =====
            var titleText = CreateResultText(panelGo.transform, "ResultTitle", "RESULT", 36,
                new Vector2(0.1f, 0.92f), new Vector2(0.9f, 0.98f), korFont);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = UIColorPalette.NEON_CYAN;
            titleText.fontStyle = FontStyles.Bold;

            // ===== 상단 구분선 =====
            CreateResultDivider(panelGo.transform, 0.915f);

            // ===== 랭크 카드 배경 (밝은 보라/마젠타 - 눈에 확 띄게) =====
            var rankCard = new GameObject("RankCard");
            rankCard.transform.SetParent(panelGo.transform, false);
            var rankCardRect = rankCard.AddComponent<RectTransform>();
            rankCardRect.anchorMin = new Vector2(0.15f, 0.70f);
            rankCardRect.anchorMax = new Vector2(0.85f, 0.91f);
            rankCardRect.offsetMin = Vector2.zero;
            rankCardRect.offsetMax = Vector2.zero;
            var rankCardBg = rankCard.AddComponent<Image>();
            rankCardBg.color = new Color(0.45f, 0.20f, 0.70f, 1f); // 밝은 보라 배경 (높은 대비)
            rankCardBg.raycastTarget = false;

            // 랭크 텍스트 (카드 안에)
            resultRankText = CreateResultText(rankCard.transform, "ResultRankText", "", 100,
                Vector2.zero, Vector2.one, korFont);
            resultRankText.alignment = TextAlignmentOptions.Center;
            resultRankText.fontStyle = FontStyles.Bold;
            resultRankText.enableAutoSizing = true;
            resultRankText.fontSizeMin = 40;
            resultRankText.fontSizeMax = 100;

            // ===== 점수 =====
            resultScoreText = CreateResultText(panelGo.transform, "ResultScoreText", "0", 52,
                new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.72f), korFont);
            resultScoreText.alignment = TextAlignmentOptions.Center;
            resultScoreText.color = UIColorPalette.NEON_CYAN;
            resultScoreText.fontStyle = FontStyles.Bold;
            resultScoreText.enableAutoSizing = true;
            resultScoreText.fontSizeMin = 28;
            resultScoreText.fontSizeMax = 52;

            // ===== 콤보 + 정확도 (한 줄에) =====
            resultComboText = CreateResultText(panelGo.transform, "ResultComboText", "", 24,
                new Vector2(0.05f, 0.58f), new Vector2(0.5f, 0.63f), korFont);
            resultComboText.alignment = TextAlignmentOptions.Center;
            resultComboText.color = UIColorPalette.NEON_GOLD;

            resultAccuracyText = CreateResultText(panelGo.transform, "ResultAccuracyText", "", 24,
                new Vector2(0.5f, 0.58f), new Vector2(0.95f, 0.63f), korFont);
            resultAccuracyText.alignment = TextAlignmentOptions.Center;
            resultAccuracyText.color = UIColorPalette.NEON_GOLD;

            // ===== 판정 섹션 구분선 =====
            CreateResultDivider(panelGo.transform, 0.575f);

            // ===== 판정별 카운트 (카드 안에) =====
            var judgeCard = new GameObject("JudgeCard");
            judgeCard.transform.SetParent(panelGo.transform, false);
            var judgeCardRect = judgeCard.AddComponent<RectTransform>();
            judgeCardRect.anchorMin = new Vector2(0.08f, 0.32f);
            judgeCardRect.anchorMax = new Vector2(0.92f, 0.57f);
            judgeCardRect.offsetMin = Vector2.zero;
            judgeCardRect.offsetMax = Vector2.zero;
            var judgeCardBg = judgeCard.AddComponent<Image>();
            judgeCardBg.color = new Color(0.10f, 0.12f, 0.30f, 1f); // 네이비 카드 배경
            judgeCardBg.raycastTarget = false;

            // 판정 텍스트들 (카드 내부 상대 위치)
            resultPerfectText = CreateResultText(judgeCard.transform, "PerfectText", "", 22,
                new Vector2(0.05f, 0.80f), new Vector2(0.95f, 1.0f), korFont);
            resultPerfectText.color = UIColorPalette.JUDGE_PERFECT;

            resultGreatText = CreateResultText(judgeCard.transform, "GreatText", "", 22,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.80f), korFont);
            resultGreatText.color = UIColorPalette.JUDGE_GREAT;

            resultGoodText = CreateResultText(judgeCard.transform, "GoodText", "", 22,
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.60f), korFont);
            resultGoodText.color = UIColorPalette.JUDGE_GOOD;

            resultBadText = CreateResultText(judgeCard.transform, "BadText", "", 22,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.40f), korFont);
            resultBadText.color = UIColorPalette.JUDGE_BAD;

            resultMissText = CreateResultText(judgeCard.transform, "MissText", "", 22,
                new Vector2(0.05f, 0.0f), new Vector2(0.95f, 0.20f), korFont);
            resultMissText.color = new Color(0.6f, 0.6f, 0.6f);

            // ===== 하단 구분선 =====
            CreateResultDivider(panelGo.transform, 0.31f);

            // ===== 버튼 영역 (더 크게) =====
            retryButton = CreateResultButton(panelGo.transform, "RetryButton", "다시하기",
                new Vector2(0.08f, 0.18f), new Vector2(0.48f, 0.28f), korFont);
            retryButton.onClick.AddListener(() => GameManager.Instance?.LoadScene("Gameplay"));

            menuButton = CreateResultButton(panelGo.transform, "MenuButton", "메뉴로",
                new Vector2(0.52f, 0.18f), new Vector2(0.92f, 0.28f), korFont);
            menuButton.onClick.AddListener(() => GameManager.Instance?.LoadScene("MainMenuScene"));

            panelGo.SetActive(false);

            Debug.Log("[GameplayUI] ResultPanel 동적 생성 완료");
            return panelGo;
        }

        private void CreateResultDivider(Transform parent, float yPos)
        {
            var divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            var divRect = divider.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.1f, yPos);
            divRect.anchorMax = new Vector2(0.9f, yPos + 0.003f);
            divRect.offsetMin = Vector2.zero;
            divRect.offsetMax = Vector2.zero;
            var divImg = divider.AddComponent<Image>();
            divImg.color = UIColorPalette.NEON_CYAN.WithAlpha(0.4f);
            divImg.raycastTarget = false;
        }

        private TMP_Text CreateResultText(Transform parent, string name, string text, float fontSize,
            Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }

        private Button CreateResultButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.55f, 0.25f, 0.90f, 1f); // 밝은 보라 버튼

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 30;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 30;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;

            return btn;
        }

        public void ShowResult(GameResult result)
        {
            Debug.Log($"[GameplayUI] ShowResult called - resultPanel:{resultPanel != null}, Score:{result.Score}, Rank:{result.Rank}, Miss:{result.MissCount}");

            // 통계 HUD + 일시정지 버튼 숨기기
            if (statsPanel != null) statsPanel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);

            if (resultPanel == null)
            {
                Debug.LogError("[GameplayUI] ResultPanel is NULL! Attempting dynamic creation...");
                resultPanel = CreateResultPanel();
            }
            if (resultPanel == null)
            {
                Debug.LogError("[GameplayUI] ResultPanel creation FAILED! Cannot show result.");
                return;
            }

            // 결과 화면 표시 시 다른 패널 숨기기
            if (pausePanel != null) pausePanel.SetActive(false);
            if (judgementText != null) judgementText.gameObject.SetActive(false);

            // 배경색은 CreateResultPanel에서 이미 설정됨
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
                resultPerfectText.text = $"퍼펙트: {result.PerfectCount}";

            if (resultGreatText != null)
                resultGreatText.text = $"그레이트: {result.GreatCount}";

            if (resultGoodText != null)
                resultGoodText.text = $"굿: {result.GoodCount}";

            if (resultBadText != null)
                resultBadText.text = $"배드: {result.BadCount}";

            if (resultMissText != null)
                resultMissText.text = $"미스: {result.MissCount}";

            // NEW RECORD 체크 및 표시
            CheckAndShowNewRecord(result);

            // 최상위 렌더링 보장: Canvas 직계 자식으로 이동 (SafeAreaPanel 밖으로)
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && resultPanel.transform.parent != canvas.transform)
            {
                resultPanel.transform.SetParent(canvas.transform, false);
            }
            // RectTransform 풀스크린 재설정 (항상 보장)
            var rrt = resultPanel.GetComponent<RectTransform>();
            if (rrt == null)
            {
                Debug.LogWarning($"[GameplayUI] ResultPanel '{resultPanel.name}' has no RectTransform! Adding one...");
                rrt = resultPanel.AddComponent<RectTransform>();
            }
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;
            resultPanel.transform.SetAsLastSibling();
            resultPanel.transform.localScale = Vector3.one;

            // 안전장치: 1프레임 후 활성 상태 재확인
            StartCoroutine(EnsureResultPanelActive());

            Debug.Log($"[GameplayUI] ResultPanel shown - active:{resultPanel.activeSelf}, scale:{resultPanel.transform.localScale}, siblingIndex:{resultPanel.transform.GetSiblingIndex()}, parent:{resultPanel.transform.parent?.name}");
        }

        private System.Collections.IEnumerator EnsureResultPanelActive()
        {
            // 3프레임 동안 ResultPanel 활성 상태 강제 유지
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                if (resultPanel != null && !resultPanel.activeSelf)
                {
                    Debug.LogWarning($"[GameplayUI] ResultPanel was deactivated on frame {i+1}! Re-activating...");
                    resultPanel.SetActive(true);
                    resultPanel.transform.SetAsLastSibling();
                }
            }
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
                resultSongInfoText.text = $"{currentSongData.Title}  |  {AIBeat.Data.PromptOptions.GetGenreDisplay(currentSongData.Genre)}  |  BPM {currentSongData.BPM:F0}";
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
            resultSongInfoText.text = $"{currentSongData.Title}  |  {AIBeat.Data.PromptOptions.GetGenreDisplay(currentSongData.Genre)}  |  BPM {currentSongData.BPM:F0}";
            resultSongInfoText.fontSize = 18;
            resultSongInfoText.color = UIColorPalette.NEON_BLUE;
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

            resultBonusScoreText.text = $"(보너스 +{result.BonusScore:N0})";
            resultBonusScoreText.color = UIColorPalette.NEON_YELLOW.WithAlpha(0.9f);
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
                resultNewRecordText.text = "** 신기록! **";
                resultNewRecordText.color = UIColorPalette.NEON_YELLOW;
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
                "S+" => UIColorPalette.NEON_YELLOW,      // 골드
                "S" => UIColorPalette.EQ_YELLOW,          // 옐로우
                "A" => UIColorPalette.NEON_CYAN,          // 시안
                "B" => UIColorPalette.NEON_GREEN,         // 그린
                "C" => UIColorPalette.TEXT_GRAY,           // 라벤더
                _ => UIColorPalette.TEXT_DIM               // 다크
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

        private void EnsureCanvasScaler()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Screen Space - Overlay 유지: UI 요소가 없는 영역은 투명하여 3D 노트가 보임
            // 배경 Image를 제거했으므로 노트 영역은 자연스럽게 투명
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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

        // ==================================================================================
        // Public API for Judgement Display
        // ==================================================================================
        
        private void SpawnEffect(string type)
        {
            // Find free effect controller
            JudgementEffectController available = null;
            foreach(var eff in effectPool)
            {
                if (eff != null && !eff.gameObject.activeSelf)
                {
                    available = eff;
                    break;
                }
            }

            if (available == null)
            {
                if (effectControllerPrefab != null)
                {
                    var obj = Instantiate(effectControllerPrefab.gameObject, transform);
                    available = obj.GetComponent<JudgementEffectController>();
                    effectPool.Add(available);
                }
            }

            if (available != null)
            {
                // UI 기반: Canvas 내부 RectTransform 위치 (판정선 위, 화면 하단 30% 지점)
                var rect = available.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.25f);
                    rect.anchorMax = new Vector2(0.5f, 0.25f);
                    rect.anchoredPosition = Vector2.zero;
                }
                available.Play(type);
            }
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (restartButton != null) restartButton.onClick.RemoveAllListeners();
            if (quitButton != null) quitButton.onClick.RemoveAllListeners();
            if (retryButton != null) retryButton.onClick.RemoveAllListeners();
            if (menuButton != null) menuButton.onClick.RemoveAllListeners();

            // 동적 생성 패널 명시적 정리
            if (resultPanel != null) Destroy(resultPanel);
            if (pausePanel != null) Destroy(pausePanel);
            if (countdownPanel != null) Destroy(countdownPanel);
            if (analysisPanel != null) Destroy(analysisPanel);

            // 판정 이펙트 풀 정리
            if (effectPool != null)
            {
                foreach (var effect in effectPool)
                {
                    if (effect != null && effect.gameObject != null)
                        Destroy(effect.gameObject);
                }
                effectPool.Clear();
            }

            // 영상 리소스 해제 (ShowLoadingVideo(false)에서 이미 해제되었을 수 있음)
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer = null;
            }
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
                videoRenderTexture = null;
            }
        }
    }
}
