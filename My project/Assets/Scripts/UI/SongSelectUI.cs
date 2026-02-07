using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;
using AIBeat.Network;

namespace AIBeat.UI
{
    /// <summary>
    /// 곡 선택 및 프롬프트 입력 UI
    /// 상단 탭: "새 곡 생성" | "내 라이브러리"
    /// </summary>
    public class SongSelectUI : MonoBehaviour
    {
        [Header("Prompt Options")]
        [SerializeField] private Transform genreButtonContainer;
        [SerializeField] private Transform moodButtonContainer;
        [SerializeField] private Slider bpmSlider;
        [SerializeField] private TextMeshProUGUI bpmValueText;

        [Header("Button Prefab")]
        [SerializeField] private GameObject optionButtonPrefab;

        [Header("Generation")]
        [SerializeField] private Button generateButton;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("Energy System")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private int maxEnergy = 3;
        [SerializeField] private float energyRechargeMinutes = 10f; // 에너지 1개 충전 시간 (분)

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("Preview")]
        [SerializeField] private TextMeshProUGUI previewGenreText;
        [SerializeField] private TextMeshProUGUI previewMoodText;
        [SerializeField] private TextMeshProUGUI previewBpmText;

        [Header("Tab System")]
        [SerializeField] private RectTransform generateTabContent;  // 기존 생성 UI 컨테이너
        [SerializeField] private RectTransform libraryTabContent;   // 라이브러리 UI 컨테이너

        [Header("Generator Mode")]
        [SerializeField] private bool useApiClient = false;  // true: AIApiClient, false: FakeSongGenerator

        // 선택된 옵션
        private string selectedGenre = "EDM";
        private string selectedMood = "Aggressive";
        private int selectedBpm = 140;

        private int currentEnergy;
        private FakeSongGenerator songGenerator;
        private ISongGenerator activeGenerator;
        private List<Button> genreButtons = new List<Button>();
        private List<Button> moodButtons = new List<Button>();

        private Color selectedColor = new Color(0.2f, 0.8f, 1f);
        private Color normalColor = Color.white;

        // 탭 시스템
        private Button generateTabButton;
        private Button libraryTabButton;
        private SongLibraryUI songLibraryUI;

        // 탭 색상
        private static readonly Color TAB_ACTIVE = new Color(0f, 0.4f, 0.6f, 1f);  // 완전 불투명
        private static readonly Color TAB_INACTIVE = new Color(0.1f, 0.1f, 0.25f, 0.9f);  // 거의 불투명

        private void Start()
        {
            AutoSetupReferences();
            Initialize();
        }

        /// <summary>
        /// Inspector 미연결 시 동적으로 UI 요소 생성/탐색
        /// </summary>
        private void AutoSetupReferences()
        {
            // 배경 이미지 설정
            SetupBackground();

            // 0. 센터 오버레이(배경) 제거
            if (generateTabContent != null)
            {
                var bgImage = generateTabContent.GetComponent<Image>();
                if (bgImage != null) bgImage.enabled = false;
            }

            // backButton: 씬에 "BackButton" 존재
            if (backButton == null)
            {
                var backObj = transform.Find("BackButton");
                if (backObj != null)
                    backButton = backObj.GetComponent<Button>();
            }

            // 옵션 버튼 프리팹 먼저 생성 (모든 탭에서 사용)
            if (optionButtonPrefab == null)
            {
                optionButtonPrefab = CreateOptionButtonTemplate();
            }

            // 모바일 최적화: 단일 컬럼 컨테이너 (전체 화면 사용)
            // Genre/Mood/BPM을 같은 공간에서 탭 전환 방식으로 표시
            if (genreButtonContainer == null)
            {
                var existing = transform.Find("OptionContainer");
                if (existing != null) genreButtonContainer = existing;
                else genreButtonContainer = CreateFullScreenColumn("OptionContainer");
            }
            // Mood/BPM은 같은 컨테이너 공유 (탭 전환으로 내용만 바뀜)
            moodButtonContainer = genreButtonContainer;

            // generateButton 동적 생성 (하단 고정)
            if (generateButton == null)
            {
                var existing = transform.Find("GenerateButton");
                if (existing != null)
                {
                    generateButton = existing.GetComponent<Button>();
                }
                else
                {
                    generateButton = CreateUIButton("GenerateButton", "곡 생성하기", new Vector2(0, -500));
                }
            }
            // 버튼 위치: 하단 고정, 터치하기 쉬운 크기
            var genRect = generateButton.GetComponent<RectTransform>();
            genRect.anchorMin = new Vector2(0, 0);
            genRect.anchorMax = new Vector2(1, 0);
            genRect.pivot = new Vector2(0.5f, 0);
            genRect.anchoredPosition = new Vector2(0, 20); // 바닥에서 20px 띄움
            genRect.sizeDelta = new Vector2(-40, 60); // 좌우 20px 여백, 높이 60px


            // Preview Texts - 숨김 (UI 단순화)
            if (previewGenreText != null) previewGenreText.gameObject.SetActive(false);
            if (previewMoodText != null) previewMoodText.gameObject.SetActive(false);
            if (previewBpmText != null) previewBpmText.gameObject.SetActive(false);
            if (bpmValueText != null) bpmValueText.gameObject.SetActive(false);

            // Energy Text - 상단 우측으로 이동 (모바일 최적화)
            if (energyText == null)
            {
                var existing = transform.Find("EnergyText");
                if (existing != null) energyText = existing.GetComponent<TextMeshProUGUI>();
                else energyText = CreateUIText("EnergyText", "에너지: 3/3", Vector2.zero, 18);
            }
            // Energy Text 위치: 상단 우측 (탭 바 옆)
            var energyRect = energyText.GetComponent<RectTransform>();
            energyRect.anchorMin = new Vector2(1, 1);
            energyRect.anchorMax = new Vector2(1, 1);
            energyRect.pivot = new Vector2(1, 1);
            energyRect.anchoredPosition = new Vector2(-10, -70); // 탭 바 아래


            // loadingPanel 동적 생성
            if (loadingPanel == null)
            {
                var existing = transform.Find("LoadingPanel");
                if (existing != null)
                {
                    loadingPanel = existing.gameObject;
                }
                else
                {
                    loadingPanel = new GameObject("LoadingPanel");
                    loadingPanel.transform.SetParent(transform, false);
                    var rect = loadingPanel.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    var bg = loadingPanel.AddComponent<Image>();
                    bg.color = new Color(0, 0, 0, 0.85f);
                }
            }

            // loadingText (LoadingPanel 내부)
            if (loadingText == null)
            {
                var lt = loadingPanel.transform.Find("LoadingText");
                if (lt != null)
                {
                    loadingText = lt.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    loadingText = CreateUIText("LoadingText", "Loading...", Vector2.zero, 24);
                    loadingText.transform.SetParent(loadingPanel.transform, false);
                    loadingText.alignment = TextAlignmentOptions.Center;
                    var lRect = loadingText.GetComponent<RectTransform>();
                    lRect.anchorMin = new Vector2(0.1f, 0.4f);
                    lRect.anchorMax = new Vector2(0.9f, 0.6f);
                    lRect.offsetMin = Vector2.zero;
                    lRect.offsetMax = Vector2.zero;
                }
            }

            // progressSlider (LoadingPanel 내부)
            if (progressSlider == null)
            {
                var ps = loadingPanel.transform.Find("ProgressSlider");
                if (ps != null)
                {
                    progressSlider = ps.GetComponent<Slider>();
                }
                else
                {
                    var sliderGo = new GameObject("ProgressSlider");
                    sliderGo.transform.SetParent(loadingPanel.transform, false);
                    var sRect = sliderGo.AddComponent<RectTransform>();
                    sRect.anchorMin = new Vector2(0.15f, 0.3f);
                    sRect.anchorMax = new Vector2(0.85f, 0.35f);
                    sRect.offsetMin = Vector2.zero;
                    sRect.offsetMax = Vector2.zero;

                    // Slider 배경
                    var bgImg = sliderGo.AddComponent<Image>();
                    bgImg.color = new Color(0.2f, 0.2f, 0.3f);

                    // Fill Area
                    var fillArea = new GameObject("Fill Area");
                    fillArea.transform.SetParent(sliderGo.transform, false);
                    var faRect = fillArea.AddComponent<RectTransform>();
                    faRect.anchorMin = Vector2.zero;
                    faRect.anchorMax = Vector2.one;
                    faRect.offsetMin = Vector2.zero;
                    faRect.offsetMax = Vector2.zero;

                    var fill = new GameObject("Fill");
                    fill.transform.SetParent(fillArea.transform, false);
                    var fillRect = fill.AddComponent<RectTransform>();
                    fillRect.anchorMin = Vector2.zero;
                    fillRect.anchorMax = Vector2.one;
                    fillRect.offsetMin = Vector2.zero;
                    fillRect.offsetMax = Vector2.zero;
                    var fillImg = fill.AddComponent<Image>();
                    fillImg.color = new Color(0f, 0.8f, 1f);

                    progressSlider = sliderGo.AddComponent<Slider>();
                    progressSlider.fillRect = fillRect;
                    progressSlider.interactable = false;
                    progressSlider.value = 0;
                }
            }
        }

        /// <summary>
        /// UI 버튼 동적 생성 헬퍼
        /// </summary>
        private Button CreateUIButton(string name, string text, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 1);
            rect.anchorMax = new Vector2(0.85f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(0, 56);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0.5f, 0.7f, 0.9f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0f, 0.7f, 1f);
            colors.pressedColor = new Color(0f, 0.3f, 0.5f);
            btn.colors = colors;

            // 텍스트
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안 (MainMenuUI와 일치)
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }


        /// <summary>
        /// 모바일 최적화: 전체 화면 단일 컬럼 생성 (탭 전환 방식)
        /// </summary>
        private Transform CreateFullScreenColumn(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(20, 100); // 하단: Generate 버튼 영역 확보 (바닥에서 100px)
            rect.offsetMax = new Vector2(-20, -62); // 상단: 메인 탭(56px) + 여백(6px) = 62px

            // ScrollRect (Vertical)
            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f; // 모바일 최적화
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            // Viewport (Mask)
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;

            var mask = viewport.AddComponent<RectMask2D>();

            // Content (VerticalLayoutGroup)
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var vLayout = content.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 6; // 가로 화면: 간격 축소 (GridLayout 내부에서 간격 처리)
            vLayout.padding = new RectOffset(10, 10, 10, 10); // 가로 화면: 여백 축소
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childAlignment = TextAnchor.UpperCenter;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cRect;
            scrollRect.viewport = vpRect;

            return content.transform; // 버튼들이 추가될 부모
        }



        /// <summary>
        /// BPM 버튼 클릭 이벤트
        /// </summary>
        private void OnBpmButtonClicked(int bpm, Button clickedButton, List<Button> allButtons)
        {
            selectedBpm = bpm;

            // 선택 시각 피드백 (선택된 버튼만 하이라이트)
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;

                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    if (btn == clickedButton)
                    {
                        // 선택됨: 네온 시안 배경
                        img.color = new Color(0f, 0.4f, 0.6f, 0.9f);
                    }
                    else
                    {
                        // 선택 안됨: 어두운 배경
                        img.color = new Color(0.12f, 0.12f, 0.22f, 0.9f);
                    }
                }
            }

            // BPM 텍스트 업데이트
            if (bpmValueText != null)
                bpmValueText.text = $"{bpm} BPM";

            // Preview 텍스트 업데이트
            if (previewBpmText != null)
                previewBpmText.text = $"{bpm} BPM";
        }

        /// <summary>
        /// 텍스트 UI 요소 동적 생성
        /// </summary>
        private TextMeshProUGUI CreateUIText(string name, string text, Vector2 anchoredPos, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(200, 30);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안 (MainMenuUI와 일치)
            tmp.alignment = TextAlignmentOptions.Center;

            return tmp;
        }

        /// <summary>
        /// 옵션 버튼 프리팹 템플릿 동적 생성
        /// </summary>
        private GameObject CreateOptionButtonTemplate()
        {
            var go = new GameObject("OptionButtonTemplate");
            go.SetActive(false); // 템플릿이므로 비활성화

            var rect = go.AddComponent<RectTransform>();
            // GridLayout이 cellSize로 크기를 제어하므로 sizeDelta는 기본값 유지

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.22f, 0.9f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.3f, 0.6f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            go.AddComponent<Button>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(4, 2);
            tRect.offsetMax = new Vector2(-4, -2);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Option";
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        /// <summary>
        /// 모든 옵션을 한 화면에 표시 (장르 → 분위기 → 빠르기 순서)
        /// 가로 화면 최적화: 각 섹션 버튼을 GridLayout 다열 배치
        /// </summary>
        private void PopulateAllOptions()
        {
            // 컨테이너의 Content 찾기
            var container = transform.Find("OptionContainer");
            if (container == null) return;

            var content = container.Find("Viewport/Content");
            if (content == null) return;

            // 기존 버튼들 모두 제거
            foreach (Transform child in content)
            {
                GameObject.Destroy(child.gameObject);
            }

            // 1. 장르 섹션
            CreateInlineSectionLabel(content, "장르");
            var genreGrid = CreateButtonGrid(content, "GenreGrid", 4);
            CreateGenreButtons(genreGrid);

            // 2. 분위기 섹션
            CreateInlineSectionLabel(content, "분위기");
            var moodGrid = CreateButtonGrid(content, "MoodGrid", 4);
            CreateMoodButtons(moodGrid);

            // 3. 빠르기(BPM) 섹션
            CreateInlineSectionLabel(content, "빠르기 (BPM)");
            var bpmGrid = CreateButtonGrid(content, "BpmGrid", 3);
            CreateBPMButtons(bpmGrid);
        }

        /// <summary>
        /// 가로 화면용 GridLayoutGroup 버튼 컨테이너 생성
        /// </summary>
        private Transform CreateButtonGrid(Transform parent, string name, int columns)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 0); // ContentSizeFitter가 자동 조절

            var grid = go.AddComponent<GridLayoutGroup>();
            // 셀 크기: 가로는 부모 폭 / columns - 간격, 세로 48px
            grid.cellSize = new Vector2(200, 48); // 초기값 (LayoutGroup이 자동 조절)
            grid.spacing = new Vector2(8, 8);
            grid.padding = new RectOffset(0, 0, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;

            // 셀 너비를 부모에 맞게 자동 계산하는 헬퍼 컴포넌트 대신
            // 런타임에 RectTransform 이벤트로 처리
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // LayoutElement로 부모 VerticalLayoutGroup에서 전체 폭 사용
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            // 셀 크기 자동 계산: 부모 Content의 폭 기준
            // Content에 padding 40px (좌20+우20), grid padding 0
            // 실제 사용 가능 폭에서 columns로 나눔
            StartCoroutine(AdjustGridCellSize(rect, grid, columns));

            return go.transform;
        }

        /// <summary>
        /// GridLayout 셀 크기를 부모 폭에 맞게 자동 조절 (1프레임 대기)
        /// </summary>
        private System.Collections.IEnumerator AdjustGridCellSize(RectTransform gridRect, GridLayoutGroup grid, int columns)
        {
            yield return null; // 레이아웃 계산 대기

            // Canvas의 RectTransform에서 전체 폭 계산
            var canvasRect = GetComponent<RectTransform>();
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 0;

            // OptionContainer offsetMin.x=20, offsetMax.x=-20 → 좌우 40px 축소
            // VerticalLayoutGroup padding 좌우 10+10 = 20px
            float availableWidth = canvasWidth - 40 - 20;

            if (availableWidth <= 0)
            {
                // fallback: gridRect 자체에서 시도
                availableWidth = gridRect.rect.width;
            }

            if (availableWidth > 0)
            {
                float totalSpacing = grid.spacing.x * (columns - 1);
                float cellWidth = (availableWidth - totalSpacing) / columns;
                grid.cellSize = new Vector2(cellWidth, grid.cellSize.y);
            }
        }

        /// <summary>
        /// 스크롤 컨텐츠 내부에 섹션 헤더 라벨 생성
        /// </summary>
        private void CreateInlineSectionLabel(Transform parent, string text)
        {
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 28); // 가로 화면: 높이 축소

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28;
            le.preferredHeight = 28;
            le.flexibleWidth = 1;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.5f, 0.85f, 1f, 0.9f); // 밝은 시안 (약간 투명)
            tmp.alignment = TextAlignmentOptions.Left;

            // 좌측 패딩
            var padding = new Vector4(2, 0, 0, 0);
            tmp.margin = padding;
        }


        private void Initialize()
        {
            // 에너지 로드 + 시간 경과에 따른 충전
            RechargeEnergyFromTime();
            UpdateEnergyDisplay();
            StartCoroutine(EnergyRechargeLoop());

            // 곡 생성기 찾기 (API/Fake 토글)
            if (useApiClient)
            {
                var apiClient = FindFirstObjectByType<AIApiClient>();
                if (apiClient == null)
                {
                    var go = new GameObject("AIApiClient");
                    apiClient = go.AddComponent<AIApiClient>();
                }
                activeGenerator = apiClient;
            }
            else
            {
                songGenerator = FindFirstObjectByType<FakeSongGenerator>();
                if (songGenerator == null)
                {
                    var go = new GameObject("FakeSongGenerator");
                    songGenerator = go.AddComponent<FakeSongGenerator>();
                }
                activeGenerator = songGenerator;
            }

            // SongLibraryManager 싱글톤 보장
            if (SongLibraryManager.Instance == null)
            {
                var libGo = new GameObject("SongLibraryManager");
                libGo.AddComponent<SongLibraryManager>();
            }

            // 이벤트 연결 (ISongGenerator 인터페이스)
            activeGenerator.OnGenerationProgress += OnProgress;
            activeGenerator.OnGenerationComplete += OnComplete;
            activeGenerator.OnGenerationError += OnError;

            // UI 초기화
            CreateTabSystem();
            SetupBpmSlider();

            if (generateButton != null)
                generateButton.onClick.AddListener(OnGenerateClicked);

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
                // 최소 터치 크기 보장
                var backRect = backButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    var size = backRect.sizeDelta;
                    if (size.y < 50f) { size.y = 50f; backRect.sizeDelta = size; }
                }
            }

            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            UpdatePreview();

            // 모든 옵션을 한 화면에 표시 (장르 → 분위기 → 빠르기 순서)
            PopulateAllOptions();

            // 한국어 폰트 적용 (□□□ 방지) — 모든 UI 생성 완료 후 마지막에 적용
            KoreanFontManager.ApplyFontToAll(gameObject);

            // Library 탭 열기 플래그 확인
            if (GameManager.Instance != null && GameManager.Instance.OpenLibraryOnSongSelect)
            {
                GameManager.Instance.OpenLibraryOnSongSelect = false;
                SwitchToLibraryTab();
            }
        }

        /// <summary>
        /// 탭 시스템 생성 (상단 "새 곡 생성" | "내 라이브러리")
        /// </summary>
        private void CreateTabSystem()
        {
            // 중복 생성 방지
            if (transform.Find("TabBar") != null)
            {
                Debug.Log("[SongSelectUI] TabBar already exists, skipping creation");
                return;
            }

            // 탭 바 컨테이너 (Canvas의 최상위에 배치)
            var tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(transform, false);
            tabBar.transform.SetAsFirstSibling(); // 가장 위에 배치

            var tabRect = tabBar.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0, 1);
            tabRect.anchorMax = new Vector2(1, 1);
            tabRect.pivot = new Vector2(0.5f, 1);
            tabRect.anchoredPosition = new Vector2(0, 0);
            tabRect.sizeDelta = new Vector2(0, 56);

            // 탭 바 배경 (완전 불투명)
            var tabBg = tabBar.AddComponent<Image>();
            tabBg.color = new Color(0.02f, 0.02f, 0.08f, 1f);

            var hLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 2;
            hLayout.padding = new RectOffset(10, 10, 5, 5);
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // "새 곡 만들기" 탭 버튼
            generateTabButton = CreateTabButton(tabBar.transform, "새 곡 만들기");
            generateTabButton.onClick.AddListener(SwitchToGenerateTab);

            // "내 라이브러리" 탭 버튼
            libraryTabButton = CreateTabButton(tabBar.transform, "내 라이브러리");
            libraryTabButton.onClick.AddListener(SwitchToLibraryTab);

            // 라이브러리 UI 컴포넌트 추가
            songLibraryUI = gameObject.AddComponent<SongLibraryUI>();

            // 초기 상태: 생성 탭 활성화
            UpdateTabVisuals(false);
        }

        /// <summary>
        /// 탭 버튼 생성 헬퍼 (콜백 없음)
        /// </summary>
        private Button CreateTabButton(Transform parent, string text)
        {
            return CreateTabButton(parent, text, null);
        }

        /// <summary>
        /// 탭 버튼 생성 헬퍼 (콜백 있음)
        /// </summary>
        private Button CreateTabButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Tab_{text}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var bg = go.AddComponent<Image>();
            bg.color = TAB_INACTIVE;

            // 네온 테두리
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.8f, 1f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            btn.colors = colors;

            // 콜백 등록
            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            // 텍스트
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안 (MainMenuUI와 일치)
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        /// <summary>
        /// "새 곡 생성" 탭으로 전환
        /// </summary>
        private void SwitchToGenerateTab()
        {
            UpdateTabVisuals(false);

            // 기존 생성 UI 요소들 표시
            SetGenerateUIVisible(true);

            // 라이브러리 UI 숨기기
            if (songLibraryUI != null)
                songLibraryUI.Show(false);
        }

        /// <summary>
        /// "내 라이브러리" 탭으로 전환
        /// </summary>
        private void SwitchToLibraryTab()
        {
            UpdateTabVisuals(true);

            // 기존 생성 UI 요소들 숨기기
            SetGenerateUIVisible(false);

            // 라이브러리 UI 표시 (초기화 포함)
            if (songLibraryUI != null)
            {
                // 라이브러리 패널이 없으면 초기화
                if (!songLibraryUI.IsVisible)
                {
                    var parentRect = GetComponent<RectTransform>();
                    songLibraryUI.Initialize(parentRect);
                }
                songLibraryUI.Show(true);
                songLibraryUI.RefreshSongList();
            }
        }

        /// <summary>
        /// 탭 버튼 시각 효과 업데이트
        /// </summary>
        private void UpdateTabVisuals(bool libraryActive)
        {
            if (generateTabButton != null)
            {
                var img = generateTabButton.GetComponent<Image>();
                if (img != null) img.color = libraryActive ? TAB_INACTIVE : TAB_ACTIVE;
            }
            if (libraryTabButton != null)
            {
                var img = libraryTabButton.GetComponent<Image>();
                if (img != null) img.color = libraryActive ? TAB_ACTIVE : TAB_INACTIVE;
            }
        }

        /// <summary>
        /// 생성 UI 요소들 표시/숨기기
        /// </summary>
        private void SetGenerateUIVisible(bool visible)
        {
            // SerializeField로 연결된 요소들의 부모 패널을 제어
            if (genreButtonContainer != null)
                genreButtonContainer.gameObject.SetActive(visible);
            if (moodButtonContainer != null)
                moodButtonContainer.gameObject.SetActive(visible);
            if (bpmSlider != null)
                bpmSlider.gameObject.SetActive(visible);
            if (bpmValueText != null)
                bpmValueText.gameObject.SetActive(visible);
            if (generateButton != null)
                generateButton.gameObject.SetActive(visible);
            if (previewGenreText != null)
                previewGenreText.gameObject.SetActive(visible);
            if (previewMoodText != null)
                previewMoodText.gameObject.SetActive(visible);
            if (previewBpmText != null)
                previewBpmText.gameObject.SetActive(visible);
            if (energyText != null)
                energyText.gameObject.SetActive(visible);

            // generateTabContent이 Inspector에서 설정되어 있으면 그것도 제어
            if (generateTabContent != null)
                generateTabContent.gameObject.SetActive(visible);

            // 옵션 컨테이너 표시/숨김
            var optContainer = transform.Find("OptionContainer");
            if (optContainer != null)
                optContainer.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Genre 버튼 생성 (GridLayout 컨테이너에 배치)
        /// </summary>
        private void CreateGenreButtons(Transform parent)
        {
            genreButtons.Clear();

            foreach (string genre in PromptOptions.Genres)
            {
                string displayName = PromptOptions.GetGenreDisplay(genre);
                var btnGo = GameObject.Instantiate(optionButtonPrefab, parent);
                btnGo.name = $"Genre_{genre}";
                btnGo.SetActive(true);

                var btnText = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = displayName;
                    btnText.fontSize = 18;
                    btnText.fontStyle = FontStyles.Bold;
                    btnText.color = new Color(0.4f, 0.95f, 1f, 1f);
                }

                var btn = btnGo.GetComponent<Button>();
                if (btn != null)
                {
                    string capturedGenre = genre;
                    btn.onClick.AddListener(() => OnGenreSelected(capturedGenre, btn));
                    genreButtons.Add(btn);
                }
            }

            // 첫 번째 선택
            if (genreButtons.Count > 0)
                OnGenreSelected(PromptOptions.Genres[0], genreButtons[0]);
        }

        /// <summary>
        /// Mood 버튼 생성 (GridLayout 컨테이너에 배치)
        /// </summary>
        private void CreateMoodButtons(Transform parent)
        {
            moodButtons.Clear();

            foreach (string mood in PromptOptions.Moods)
            {
                string displayName = PromptOptions.GetMoodDisplay(mood);
                var btnGo = GameObject.Instantiate(optionButtonPrefab, parent);
                btnGo.name = $"Mood_{mood}";
                btnGo.SetActive(true);

                var btnText = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = displayName;
                    btnText.fontSize = 18;
                    btnText.fontStyle = FontStyles.Bold;
                    btnText.color = new Color(0.4f, 0.95f, 1f, 1f);
                }

                var btn = btnGo.GetComponent<Button>();
                if (btn != null)
                {
                    string capturedMood = mood;
                    btn.onClick.AddListener(() => OnMoodSelected(capturedMood, btn));
                    moodButtons.Add(btn);
                }
            }

            // 첫 번째 선택
            if (moodButtons.Count > 0)
                OnMoodSelected(PromptOptions.Moods[0], moodButtons[0]);
        }

        /// <summary>
        /// BPM 버튼 생성 (GridLayout 컨테이너에 배치)
        /// </summary>
        private void CreateBPMButtons(Transform parent)
        {
            List<Button> bpmButtons = new List<Button>();
            int[] bpmOptions = { 80, 100, 120, 140, 160, 180 };

            foreach (int bpm in bpmOptions)
            {
                var btnGo = GameObject.Instantiate(optionButtonPrefab, parent);
                btnGo.name = $"BPM_{bpm}";
                btnGo.SetActive(true);

                var btnText = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = $"{bpm} BPM";
                    btnText.fontSize = 20;
                    btnText.fontStyle = FontStyles.Bold;
                    btnText.color = new Color(0.4f, 0.95f, 1f, 1f);
                }

                var btn = btnGo.GetComponent<Button>();
                if (btn != null)
                {
                    int capturedBpm = bpm;
                    btn.onClick.AddListener(() => OnBpmButtonClicked(capturedBpm, btn, bpmButtons));
                    bpmButtons.Add(btn);
                }
            }

            // 기본값 140 BPM 선택 (4번째 버튼)
            if (bpmButtons.Count >= 4)
                OnBpmButtonClicked(140, bpmButtons[3], bpmButtons);
        }


        private void OnGenreSelected(string genre, Button btn)
        {
            selectedGenre = genre;
            SelectButton(btn, genreButtons);
            UpdatePreview();
        }

        private void OnMoodSelected(string mood, Button btn)
        {
            selectedMood = mood;
            SelectButton(btn, moodButtons);
            UpdatePreview();
        }

        private void SelectButton(Button selected, List<Button> allButtons)
        {
            foreach (var btn in allButtons)
            {
                bool isSelected = (btn == selected);
                // 배경색 변경
                var bg = btn.GetComponent<Image>();
                if (bg != null)
                    bg.color = isSelected
                        ? new Color(0f, 0.4f, 0.6f, 0.9f)
                        : new Color(0.12f, 0.12f, 0.22f, 0.9f);
                // 텍스트 Bold/Normal
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                    tmp.color = isSelected ? selectedColor : normalColor;
                }
            }
        }

        private void SetupBpmSlider()
        {
            if (bpmSlider == null) return;

            bpmSlider.minValue = 80;
            bpmSlider.maxValue = 180;
            bpmSlider.value = 140;
            bpmSlider.wholeNumbers = true;

            bpmSlider.onValueChanged.AddListener(OnBpmChanged);
            OnBpmChanged(bpmSlider.value);
        }

        private void OnBpmChanged(float value)
        {
            selectedBpm = (int)value;
            if (bpmValueText != null)
                bpmValueText.text = $"{selectedBpm} BPM";
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (previewGenreText != null) previewGenreText.text = PromptOptions.GetGenreDisplay(selectedGenre);
            if (previewMoodText != null) previewMoodText.text = PromptOptions.GetMoodDisplay(selectedMood);
            if (previewBpmText != null) previewBpmText.text = $"{selectedBpm} BPM";
        }

        private void OnGenerateClicked()
        {
            if (currentEnergy <= 0)
            {
                ShowNoEnergyDialog();
                return;
            }

            // 에너지 소모 + 타임스탬프 기록
            currentEnergy--;
            PlayerPrefs.SetInt("Energy", currentEnergy);
            if (currentEnergy < maxEnergy)
                PlayerPrefs.SetString("EnergyLastUseTime", System.DateTime.Now.ToString("o"));
            UpdateEnergyDisplay();

            // 로딩 시작
            if (loadingPanel != null)
                loadingPanel.SetActive(true);

            if (loadingText != null)
                loadingText.text = "AI가 비트를 조립 중입니다...";

            // 생성 요청
            var options = new PromptOptions
            {
                Genre = selectedGenre,
                BPM = selectedBpm,
                Mood = selectedMood,
                Duration = 90,
                Structure = "intro-build-drop-outro"
            };

            activeGenerator.Generate(options);
        }

        private void OnProgress(float progress)
        {
            if (progressSlider != null)
                progressSlider.value = progress;

            if (loadingText != null)
            {
                string[] messages = {
                    "AI가 비트를 조립 중입니다...",
                    "멜로디를 생성하고 있습니다...",
                    "노트 패턴을 계산하고 있습니다...",
                    "마무리 중입니다..."
                };
                int idx = Mathf.FloorToInt(progress * messages.Length);
                idx = Mathf.Clamp(idx, 0, messages.Length - 1);
                loadingText.text = messages[idx];
            }
        }

        private void OnComplete(SongData songData)
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            // 라이브러리에 곡 기록 저장
            SaveToLibrary(songData);

            // 게임 시작
            GameManager.Instance?.StartGame(songData);
        }

        /// <summary>
        /// 생성된 곡을 라이브러리에 저장
        /// </summary>
        private void SaveToLibrary(SongData songData)
        {
            if (SongLibraryManager.Instance == null) return;

            var record = new SongRecord
            {
                Title = songData.Title,
                Artist = songData.Artist,
                Genre = songData.Genre,
                Mood = songData.Mood,
                BPM = (int)songData.BPM,
                DifficultyLevel = songData.Difficulty,
                Duration = songData.Duration,
                BestRank = "",
                BestScore = 0,
                BestCombo = 0,
                PlayCount = 0,
                Seed = UnityEngine.Random.Range(1, 999999)
            };

            SongLibraryManager.Instance.AddSong(record);
        }

        private void OnError(string error)
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            Debug.LogError($"[SongSelect] Generation error: {error}");

            // 에너지 복구
            currentEnergy++;
            PlayerPrefs.SetInt("Energy", currentEnergy);
            UpdateEnergyDisplay();

            // 에러 메시지 표시 (간단한 구현)
            if (loadingText != null)
            {
                loadingText.text = $"생성 실패: {error}";
                loadingPanel.SetActive(true);
                Invoke(nameof(HideLoading), 2f);
            }
        }

        private void HideLoading()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        private void UpdateEnergyDisplay()
        {
            if (energyText == null) return;

            if (currentEnergy >= maxEnergy)
            {
                energyText.text = $"에너지: {currentEnergy}/{maxEnergy}";
            }
            else
            {
                // 남은 충전 시간 표시
                string timerStr = "";
                string lastUseStr = PlayerPrefs.GetString("EnergyLastUseTime", "");
                if (!string.IsNullOrEmpty(lastUseStr) && System.DateTime.TryParse(lastUseStr, out System.DateTime lastUse))
                {
                    double minutesElapsed = (System.DateTime.Now - lastUse).TotalMinutes;
                    double minutesUntilNext = energyRechargeMinutes - (minutesElapsed % energyRechargeMinutes);
                    int mins = Mathf.Max(1, Mathf.CeilToInt((float)minutesUntilNext));
                    timerStr = $" ({mins}분)";
                }
                energyText.text = $"에너지: {currentEnergy}/{maxEnergy}{timerStr}";
            }
        }

        /// <summary>
        /// 오프라인 시간 경과에 따른 에너지 자동 충전
        /// </summary>
        private void RechargeEnergyFromTime()
        {
            currentEnergy = PlayerPrefs.GetInt("Energy", maxEnergy);
            if (currentEnergy >= maxEnergy) return;

            string lastUseStr = PlayerPrefs.GetString("EnergyLastUseTime", "");
            if (string.IsNullOrEmpty(lastUseStr)) return;

            if (System.DateTime.TryParse(lastUseStr, out System.DateTime lastUse))
            {
                double minutesElapsed = (System.DateTime.Now - lastUse).TotalMinutes;
                int recharged = Mathf.FloorToInt((float)(minutesElapsed / energyRechargeMinutes));
                if (recharged > 0)
                {
                    currentEnergy = Mathf.Min(currentEnergy + recharged, maxEnergy);
                    PlayerPrefs.SetInt("Energy", currentEnergy);
                    // 남은 시간 보존을 위해 lastUseTime 갱신
                    double usedMinutes = recharged * energyRechargeMinutes;
                    var newLastUse = lastUse.AddMinutes(usedMinutes);
                    PlayerPrefs.SetString("EnergyLastUseTime", newLastUse.ToString("o"));
                }
            }
        }

        /// <summary>
        /// 실시간 에너지 충전 타이머 (1분마다 체크)
        /// </summary>
        private System.Collections.IEnumerator EnergyRechargeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f);
                if (currentEnergy < maxEnergy)
                {
                    RechargeEnergyFromTime();
                    UpdateEnergyDisplay();
                }
            }
        }

        private void ShowNoEnergyDialog()
        {
            // 에너지 부족 다이얼로그: 남은 충전 시간 표시
            string message = "에너지가 부족합니다!\n";

            string lastUseStr = PlayerPrefs.GetString("EnergyLastUseTime", "");
            if (!string.IsNullOrEmpty(lastUseStr) && System.DateTime.TryParse(lastUseStr, out System.DateTime lastUse))
            {
                double minutesElapsed = (System.DateTime.Now - lastUse).TotalMinutes;
                double minutesUntilNext = energyRechargeMinutes - (minutesElapsed % energyRechargeMinutes);
                int mins = Mathf.CeilToInt((float)minutesUntilNext);
                message += $"{mins}분 후 충전됩니다";
            }
            else
            {
                message += "잠시 후 다시 시도해주세요.";
            }

            // 로딩 패널을 임시 다이얼로그로 재활용
            if (loadingPanel != null && loadingText != null)
            {
                loadingText.text = message;
                loadingPanel.SetActive(true);
                if (progressSlider != null) progressSlider.gameObject.SetActive(false);
                Invoke(nameof(HideNoEnergyDialog), 3f);
            }

#if UNITY_EDITOR
            Debug.Log($"[SongSelect] {message}");
#endif
        }

        private void HideNoEnergyDialog()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            if (progressSlider != null)
                progressSlider.gameObject.SetActive(true);
        }

        private void OnBackClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        /// <summary>
        /// 배경 이미지 설정 (SongSelectBG.jpg)
        /// </summary>
        private void SetupBackground()
        {
            var existing = transform.Find("BackgroundImage");
            if (existing != null) return; // 이미 존재

            var bgGo = new GameObject("BackgroundImage");
            bgGo.transform.SetParent(transform, false);
            bgGo.transform.SetAsFirstSibling(); // 가장 뒤에 배치

            var rect = bgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = bgGo.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.3f); // 반투명 (UI 가독성)

            // Resources에서 로드 시도, 없으면 Sprite로 직접 로드
            var tex = Resources.Load<Texture2D>("UI/SongSelectBG");
            if (tex == null)
            {
                // AssetDatabase 사용 불가(런타임) → Sprite 직접 생성 불가
                // 대신 어두운 그라데이션 배경 사용
                img.color = new Color(0.02f, 0.02f, 0.08f, 0.95f);
                Debug.Log("[SongSelectUI] Background image not found in Resources, using dark fallback");
                return;
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        private void OnDestroy()
        {
            if (activeGenerator != null)
            {
                activeGenerator.OnGenerationProgress -= OnProgress;
                activeGenerator.OnGenerationComplete -= OnComplete;
                activeGenerator.OnGenerationError -= OnError;
            }

            if (generateButton != null) generateButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (bpmSlider != null) bpmSlider.onValueChanged.RemoveAllListeners();
            if (generateTabButton != null) generateTabButton.onClick.RemoveAllListeners();
            if (libraryTabButton != null) libraryTabButton.onClick.RemoveAllListeners();

            foreach (var btn in genreButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();

            foreach (var btn in moodButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }
}
