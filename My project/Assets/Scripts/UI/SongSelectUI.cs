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

            // generateButton 동적 생성 (하단 중앙 배치)
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
            // 버튼 위치 강제 조정
            var genRect = generateButton.GetComponent<RectTransform>();
            genRect.anchorMin = new Vector2(0.5f, 0);
            genRect.anchorMax = new Vector2(0.5f, 0);
            genRect.pivot = new Vector2(0.5f, 0);
            genRect.anchoredPosition = new Vector2(0, 50); // 바닥에서 50 띄움
            genRect.sizeDelta = new Vector2(400, 70);


            // 1. Genre Container (Left Column: 0.05 ~ 0.35)
            if (genreButtonContainer == null)
            {
                var existing = transform.Find("GenreContainer");
                if (existing != null) genreButtonContainer = existing;
                else genreButtonContainer = CreateColumnContainer("GenreContainer");
            }
            LayoutColumn(genreButtonContainer, 0.05f, 0.35f, "장르");

            // 2. Mood Container (Center Column: 0.35 ~ 0.65)
            if (moodButtonContainer == null)
            {
                var existing = transform.Find("MoodContainer");
                if (existing != null) moodButtonContainer = existing;
                else moodButtonContainer = CreateColumnContainer("MoodContainer");
            }
            LayoutColumn(moodButtonContainer, 0.35f, 0.65f, "분위기");

            // 3. BPM Container (Right Column: 0.65 ~ 0.95)
            if (bpmSlider == null)
            {
                var existing = transform.Find("BpmContainer");
                if (existing != null)
                {
                    // 기존 컨테이너의 Content가 비어있는지 확인
                    var content = existing.Find("Viewport/Content");
                    if (content != null && content.childCount == 0)
                    {
                        // Content가 비어있으면 버튼 재생성
                        Debug.Log("[SongSelectUI] BpmContainer exists but empty, regenerating buttons");
                        GameObject.Destroy(existing.gameObject);
                        bpmSlider = CreateBpmSlider();
                    }
                    else
                    {
                        // Content에 버튼이 있으면 기존 슬라이더 찾기
                        bpmSlider = existing.GetComponentInChildren<Slider>();
                    }
                }
                else
                {
                    bpmSlider = CreateBpmSlider();
                }
            }
             // BPM 컨테이너 위치 잡기 (CreateBpmSlider에서 생성된 컨테이너)
            var bpmContainer = transform.Find("BpmContainer");
            if (bpmContainer != null)
            {
                LayoutColumn(bpmContainer, 0.65f, 0.95f, "빠르기");
            }


            // Preview Texts - 숨김 (UI 단순화)
            if (previewGenreText != null) previewGenreText.gameObject.SetActive(false);
            if (previewMoodText != null) previewMoodText.gameObject.SetActive(false);
            if (previewBpmText != null) previewBpmText.gameObject.SetActive(false);
            if (bpmValueText != null) bpmValueText.gameObject.SetActive(false);

            // Energy Text - 상단이나 하단으로 이동
            if (energyText == null)
            {
                var existing = transform.Find("EnergyText");
                if (existing != null) energyText = existing.GetComponent<TextMeshProUGUI>();
                else energyText = CreateUIText("EnergyText", "에너지: 3/3", Vector2.zero, 18);
            }
            // Energy Text 위치: Generate 버튼 아래
            var energyRect = energyText.GetComponent<RectTransform>();
            energyRect.anchorMin = new Vector2(0.5f, 0);
            energyRect.anchorMax = new Vector2(0.5f, 0);
            energyRect.pivot = new Vector2(0.5f, 1);
            energyRect.anchoredPosition = new Vector2(0, 40); // Generate 버튼 바로 아래


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

            // optionButtonPrefab: 프리팹 없으면 동적 생성 패턴
            if (optionButtonPrefab == null)
            {
                optionButtonPrefab = CreateOptionButtonTemplate();
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
        /// 세로 컬럼 컨테이너 생성 (Vertical Column) - 화면 높이의 대부분을 차지
        /// </summary>
        private Transform CreateColumnContainer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            // Anchor는 LayoutColumn에서 설정

            // ScrollRect (Vertical)
            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            // Viewport (Mask)
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            
            var mask = viewport.AddComponent<RectMask2D>(); // 성능상 RectMask2D 선호
            // viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0); // RectMask2D는 Image 필요 없음

            // Content (VerticalLayoutGroup)
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            // 높이는 ContentSizeFitter가 제어

            var vLayout = content.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 15;
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false; // 버튼 높이는 자체 설정
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
        /// 컬럼 컨테이너 레이아웃 및 라벨 설정 헬퍼
        /// </summary>
        private void LayoutColumn(Transform containerContent, float xMin, float xMax, string labelText)
        {
            if (containerContent == null) return;
            
            // containerContent는 ScrollRect의 Content임.
            // ScrollRect가 있는 GameObject(컨테이너 본체)를 찾아야 함
            // CreateColumnContainer 구조: Container -> Viewport -> Content
            
            var scrollRect = containerContent.GetComponentInParent<ScrollRect>();
            if (scrollRect == null) return;
            
            var containerTr = scrollRect.transform;
            var rect = containerTr.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(xMin, 0.2f); // 하단 여백 (버튼 영역)
                rect.anchorMax = new Vector2(xMax, 0.9f); // 상단 여백 (탭바 아래)
                rect.offsetMin = new Vector2(5, 0);
                rect.offsetMax = new Vector2(-5, -40); // 라벨 공간 확보
            }
            
            // 섹션 라벨 생성/위치 잡기
            // 라벨은 컨테이너 위에 배치 (컨테이너의 형제/부모 좌표계 기준)
            string labelName = $"{containerTr.name}_Label";
            var labelTr = transform.Find(labelName);
            if (labelTr == null)
            {
                CreateSectionLabel(labelName, labelText, xMin, xMax);
            }
        }

        /// <summary>
        /// BPM 슬라이더 동적 생성
        /// </summary>
        /// <summary>
        /// BPM 선택 컨테이너 생성 (슬라이더 대신 버튼 리스트 방식)
        /// </summary>
        private Slider CreateBpmSlider()
        {
            // 더미 슬라이더 (기존 코드 호환성을 위해 유지, 실제로는 사용 안함)
            var dummySlider = new GameObject("BpmSlider_Dummy");
            dummySlider.transform.SetParent(transform, false);
            var dummyRect = dummySlider.AddComponent<RectTransform>();
            dummyRect.sizeDelta = new Vector2(0, 0);
            var slider = dummySlider.AddComponent<Slider>();
            slider.minValue = 80;
            slider.maxValue = 180;
            slider.value = 140;
            dummySlider.SetActive(false);  // 숨김

            // BPM 버튼 컨테이너 (세로 컬럼)
            var container = CreateColumnContainer("BpmContainer");
            // 위치 설정은 AutoSetupReferences의 LayoutColumn에서 처리됨 (여기서는 생성만)

            // BPM 옵션 버튼들 생성 (80, 100, 120, 140, 160, 180)
            int[] bpmOptions = { 80, 100, 120, 140, 160, 180 };
            List<Button> bpmButtons = new List<Button>();

            Debug.Log($"[SongSelectUI] CreateBpmSlider: optionButtonPrefab={optionButtonPrefab}, container={container}");

            foreach (int bpm in bpmOptions)
            {
                var btnGo = GameObject.Instantiate(optionButtonPrefab, container.transform);
                btnGo.name = $"BPM_{bpm}";
                btnGo.SetActive(true);  // 명시적 활성화
                Debug.Log($"[SongSelectUI] Created BPM button: {bpm}");

                // 버튼 크기 및 스타일 조정
                var btnRect = btnGo.GetComponent<RectTransform>();
                if (btnRect != null)
                {
                    // 세로 리스트에서는 가로가 꽉 차므로(VerticalLayoutGroup childForceExpandWidth=true)
                    // 높이만 설정하면 됨
                    btnRect.sizeDelta = new Vector2(0, 50); 
                }

                // 텍스트 설정 (폰트 크기+색상 강제 적용)
                var btnText = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = $"{bpm} BPM";
                    btnText.fontSize = 22;  // 20→22 (더 크게)
                    btnText.fontStyle = FontStyles.Bold;
                    btnText.color = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안 (명확히 보이도록)
                }

                // 버튼 이벤트
                var btn = btnGo.GetComponent<Button>();
                if (btn != null)
                {
                    int capturedBpm = bpm;
                    btn.onClick.AddListener(() => OnBpmButtonClicked(capturedBpm, btn, bpmButtons));
                }

                bpmButtons.Add(btn);
            }

            // 기본값 140 BPM 선택 (4번째 버튼)
            if (bpmButtons.Count >= 4)
                OnBpmButtonClicked(140, bpmButtons[3], bpmButtons);

            // BPM 버튼에 한국어 폰트 강제 적용 (텍스트 가시성 보장)
            KoreanFontManager.ApplyFontToAll(container.gameObject);

            return slider;
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
            rect.sizeDelta = new Vector2(130, 50);

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
            tRect.offsetMin = new Vector2(6, 2);
            tRect.offsetMax = new Vector2(-6, -2);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Option";
            tmp.fontSize = 20;  // 18→20 (MainMenuUI와 일관성)
            tmp.fontStyle = FontStyles.Bold;  // Bold 추가
            tmp.color = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안 (MainMenuUI와 일치)
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        /// <summary>
        /// 섹션 라벨 동적 생성 (컬럼 상단 중앙)
        /// </summary>
        private void CreateSectionLabel(string name, string text, float xMin, float xMax)
        {
            if (transform.Find(name) != null) return; // 이미 존재

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, 0.9f); // 컨테이너 상단
            rect.anchorMax = new Vector2(xMax, 0.9f);
            rect.pivot = new Vector2(0.5f, 0); // 바닥 기준
            rect.anchoredPosition = new Vector2(0, 10); // 조금 띄움
            rect.sizeDelta = new Vector2(0, 30); 

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22; 
            tmp.color = new Color(0.6f, 0.9f, 1f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Bottom; // 텍스트 하단 정렬(버튼쪽으로)
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
            CreateOptionButtons();
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

            // 한국어 폰트 적용 (□□□ 방지)
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
        /// 탭 버튼 생성 헬퍼
        /// </summary>
        private Button CreateTabButton(Transform parent, string text)
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

            // 동적으로 생성된 섹션 라벨 숨기기
            var genreLabel = transform.Find("GenreLabel");
            if (genreLabel != null)
                genreLabel.gameObject.SetActive(visible);

            var moodLabel = transform.Find("MoodLabel");
            if (moodLabel != null)
                moodLabel.gameObject.SetActive(visible);

            var bpmLabel = transform.Find("BpmLabel");
            if (bpmLabel != null)
                bpmLabel.gameObject.SetActive(visible);
        }

        private void CreateOptionButtons()
        {
            // 장르 버튼 생성 (한국어 표시명 사용)
            foreach (string genre in PromptOptions.Genres)
            {
                string displayName = PromptOptions.GetGenreDisplay(genre);
                CreateOptionButton(genre, displayName, genreButtonContainer, genreButtons, OnGenreSelected);
            }

            // 분위기 버튼 생성 (한국어 표시명 사용)
            foreach (string mood in PromptOptions.Moods)
            {
                string displayName = PromptOptions.GetMoodDisplay(mood);
                CreateOptionButton(mood, displayName, moodButtonContainer, moodButtons, OnMoodSelected);
            }

            // 첫 번째 옵션 선택
            if (genreButtons.Count > 0) SelectButton(genreButtons[0], genreButtons);
            if (moodButtons.Count > 0) SelectButton(moodButtons[0], moodButtons);
        }

        private void CreateOptionButton(string key, string displayText, Transform container, List<Button> buttonList,
            System.Action<string, Button> onClick)
        {
            if (optionButtonPrefab == null || container == null) return;

            GameObject obj = Instantiate(optionButtonPrefab, container);
            Button btn = obj.GetComponent<Button>();
            TextMeshProUGUI tmpText = obj.GetComponentInChildren<TextMeshProUGUI>();

            if (tmpText != null) tmpText.text = displayText;

            if (btn != null)
            {
                buttonList.Add(btn);
                btn.onClick.AddListener(() => onClick(key, btn));
            }
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
