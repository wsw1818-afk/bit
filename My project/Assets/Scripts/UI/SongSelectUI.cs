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
        private static readonly Color TAB_ACTIVE = new Color(0f, 0.4f, 0.6f, 0.9f);
        private static readonly Color TAB_INACTIVE = new Color(0.1f, 0.1f, 0.25f, 0.6f);

        private void Start()
        {
            Initialize();
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

            // 탭 바 배경
            var tabBg = tabBar.AddComponent<Image>();
            tabBg.color = new Color(0.02f, 0.02f, 0.08f, 0.95f);

            var hLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 2;
            hLayout.padding = new RectOffset(10, 10, 5, 5);
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // "새 곡 생성" 탭 버튼
            generateTabButton = CreateTabButton(tabBar.transform, "NEW BEAT");
            generateTabButton.onClick.AddListener(SwitchToGenerateTab);

            // "내 라이브러리" 탭 버튼
            libraryTabButton = CreateTabButton(tabBar.transform, "MY LIBRARY");
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
            tmp.color = Color.white;
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
        }

        private void CreateOptionButtons()
        {
            // 장르 버튼 생성
            foreach (string genre in PromptOptions.Genres)
            {
                CreateOptionButton(genre, genreButtonContainer, genreButtons, OnGenreSelected);
            }

            // 분위기 버튼 생성
            foreach (string mood in PromptOptions.Moods)
            {
                CreateOptionButton(mood, moodButtonContainer, moodButtons, OnMoodSelected);
            }

            // 첫 번째 옵션 선택
            if (genreButtons.Count > 0) SelectButton(genreButtons[0], genreButtons);
            if (moodButtons.Count > 0) SelectButton(moodButtons[0], moodButtons);
        }

        private void CreateOptionButton(string text, Transform container, List<Button> buttonList,
            System.Action<string, Button> onClick)
        {
            if (optionButtonPrefab == null || container == null) return;

            GameObject obj = Instantiate(optionButtonPrefab, container);
            Button btn = obj.GetComponent<Button>();
            TextMeshProUGUI tmpText = obj.GetComponentInChildren<TextMeshProUGUI>();

            if (tmpText != null) tmpText.text = text;

            if (btn != null)
            {
                buttonList.Add(btn);
                btn.onClick.AddListener(() => onClick(text, btn));
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
                var colors = btn.colors;
                colors.normalColor = (btn == selected) ? selectedColor : normalColor;
                btn.colors = colors;
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
            if (previewGenreText != null) previewGenreText.text = selectedGenre;
            if (previewMoodText != null) previewMoodText.text = selectedMood;
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
                energyText.text = $"Energy: {currentEnergy}/{maxEnergy}";
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
                    timerStr = $" ({mins}m)";
                }
                energyText.text = $"Energy: {currentEnergy}/{maxEnergy}{timerStr}";
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
            string message = "Energy depleted!\n";

            string lastUseStr = PlayerPrefs.GetString("EnergyLastUseTime", "");
            if (!string.IsNullOrEmpty(lastUseStr) && System.DateTime.TryParse(lastUseStr, out System.DateTime lastUse))
            {
                double minutesElapsed = (System.DateTime.Now - lastUse).TotalMinutes;
                double minutesUntilNext = energyRechargeMinutes - (minutesElapsed % energyRechargeMinutes);
                int mins = Mathf.CeilToInt((float)minutesUntilNext);
                message += $"Next energy in {mins} min";
            }
            else
            {
                message += "Please wait for recharge.";
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
