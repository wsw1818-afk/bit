using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace AIBeat.UI
{
    /// <summary>
    /// 곡 선택 UI - 리디자인 v2
    /// 단순 구조: 타이틀바(뒤로 버튼 포함) → 곡 목록 → 이퀄라이저
    /// 곡 카드 터치로 바로 플레이 (하단 버튼 패널 제거)
    /// </summary>
    public class SongSelectUI : MonoBehaviour
    {
        // 라이브러리 UI
        private SongLibraryUI songLibraryUI;

        // 이퀄라이저 바
        private List<Image> eqBars = new List<Image>();
        private Coroutine eqAnimCoroutine;

        private void Start()
        {
            Debug.Log("[SongSelectUI] Start() 호출됨 - 리디자인 v2");

            // 에디터에서 포커스 손실 시에도 게임 루프 유지 (MCP 스크린샷 캡처용)
            Application.runInBackground = true;

            // TMP_Text 생성 전에 한국어 폰트를 글로벌 기본값으로 설정
            var _ = KoreanFontManager.KoreanFont;

            EnsureEventSystem();
            EnsureCanvasScaler();
            CreateFullscreenBackground();
            Initialize();
            CreateEqualizerBar();
            EnsureSafeArea();
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
                Debug.Log("[SongSelectUI] EventSystem 자동 생성됨");
            }

            // Canvas에 GraphicRaycaster 확인
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[SongSelectUI] GraphicRaycaster 자동 추가됨");
            }
        }

        /// <summary>
        /// 전체 화면 배경 (Canvas 직접 자식으로 Safe Area 밖에도 표시)
        /// 단색 그라데이션 배경 - 격자 없음
        /// </summary>
        private void CreateFullscreenBackground()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // 기존 배경 제거
            var existingBg = canvas.transform.Find("FullscreenBackground");
            if (existingBg != null) DestroyImmediate(existingBg.gameObject);
            var existingOverlay = canvas.transform.Find("FullscreenOverlay");
            if (existingOverlay != null) DestroyImmediate(existingOverlay.gameObject);

            // 전체 화면 배경 (Canvas 직접 자식, 맨 뒤) - 단색
            var bgGo = new GameObject("FullscreenBackground");
            bgGo.transform.SetParent(canvas.transform, false);
            bgGo.transform.SetAsFirstSibling();

            var rect = bgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = bgGo.AddComponent<Image>();
            img.raycastTarget = false;
            // 단색 어두운 보라 배경 (격자 없음)
            img.color = new Color(0.04f, 0.02f, 0.08f, 1f);
        }

        /// <summary>
        /// 하단 이퀄라이저 바 (사이버펑크 스타일)
        /// </summary>
        private void CreateEqualizerBar()
        {
            var existing = transform.Find("EqualizerBar");
            if (existing != null) DestroyImmediate(existing.gameObject);

            var eqContainer = new GameObject("EqualizerBar");
            eqContainer.transform.SetParent(transform, false);

            var containerRect = eqContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, 120);

            int barCount = 25;
            float barWidth = 1f / barCount;

            for (int i = 0; i < barCount; i++)
            {
                var barGo = new GameObject($"EqBar_{i}");
                barGo.transform.SetParent(eqContainer.transform, false);
                var barRect = barGo.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(i * barWidth + 0.003f, 0);
                barRect.anchorMax = new Vector2((i + 1) * barWidth - 0.003f, 0.4f);
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;

                var barImg = barGo.AddComponent<Image>();
                barImg.raycastTarget = false;
                float t = (float)i / barCount;
                barImg.color = Color.Lerp(UIColorPalette.EQ_ORANGE, UIColorPalette.EQ_YELLOW, t);
                eqBars.Add(barImg);
            }

            eqAnimCoroutine = StartCoroutine(AnimateEqualizer());
        }

        private IEnumerator AnimateEqualizer()
        {
            float[] phases = new float[eqBars.Count];
            float[] speeds = new float[eqBars.Count];
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = Random.Range(0f, Mathf.PI * 2f);
                speeds[i] = Random.Range(1.5f, 3.5f);
            }
            while (true)
            {
                for (int i = 0; i < eqBars.Count; i++)
                {
                    if (eqBars[i] == null) continue;
                    float h = 0.15f + 0.85f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * speeds[i] + phases[i]));
                    var r = eqBars[i].GetComponent<RectTransform>();
                    r.anchorMax = new Vector2(r.anchorMax.x, h);
                }
                yield return null;
            }
        }

        private void Initialize()
        {
            // SongLibraryManager 싱글톤 보장
            if (SongLibraryManager.Instance == null)
            {
                var libGo = new GameObject("SongLibraryManager");
                libGo.AddComponent<SongLibraryManager>();
            }

            // StreamingAssets 내 MP3 파일 자동 스캔 → 라이브러리에 등록
            ScanAndRegisterStreamingAssets();

            // 기존 BackButton 제거 (씬에 남아있는 경우)
            // 1) 직접 자식에서 찾기
            var oldBackBtn = transform.Find("BackButton");
            if (oldBackBtn != null) DestroyImmediate(oldBackBtn.gameObject);

            // 2) SafeAreaPanel 내부에서 찾기 (SafeAreaApplier가 이동시킨 경우)
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var safeAreaPanel = canvas.transform.Find("SafeAreaPanel");
                if (safeAreaPanel != null)
                {
                    var oldBackBtnInSafe = safeAreaPanel.Find("BackButton");
                    if (oldBackBtnInSafe != null)
                    {
                        Debug.Log("[SongSelectUI] SafeAreaPanel 내부의 기존 BackButton 제거");
                        DestroyImmediate(oldBackBtnInSafe.gameObject);
                    }
                }
            }

            // 타이틀 바 생성 (뒤로 버튼 포함)
            CreateTitleBar();

            // 라이브러리 UI 초기화 + 표시
            songLibraryUI = gameObject.AddComponent<SongLibraryUI>();
            var parentRect = GetComponent<RectTransform>();
            songLibraryUI.Initialize(parentRect);
            songLibraryUI.Show(true);

            // 설정 FAB 버튼 생성 (우하단)
            CreateSettingsFAB();

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(gameObject);
        }

        /// <summary>
        /// 상단 타이틀 바 (뒤로 버튼 통합) - 사이버펑크 스타일
        /// </summary>
        private void CreateTitleBar()
        {
            var existing = transform.Find("TitleBar");
            if (existing != null) DestroyImmediate(existing.gameObject);

            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(transform, false);

            var rect = titleBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 100);

            // 배경 (진한 보라 + 하단 네온 라인)
            var bg = titleBar.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.02f, 0.12f, 0.98f);
            bg.raycastTarget = false;

            // 하단 네온 라인 (시안)
            var bottomLine = new GameObject("BottomLine");
            bottomLine.transform.SetParent(titleBar.transform, false);
            var lineRect = bottomLine.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(1, 0);
            lineRect.pivot = new Vector2(0.5f, 0);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(0, 3);
            var lineImg = bottomLine.AddComponent<Image>();
            lineImg.color = UIColorPalette.NEON_CYAN_BRIGHT;
            lineImg.raycastTarget = false;

            // 뒤로 버튼 (좌측) - 원형 스타일
            var backBtn = new GameObject("BackButton");
            backBtn.transform.SetParent(titleBar.transform, false);
            var backRect = backBtn.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.anchoredPosition = new Vector2(15, 0);
            backRect.sizeDelta = new Vector2(65, 65);

            var backBg = backBtn.AddComponent<Image>();
            backBg.color = new Color(0.12f, 0.04f, 0.25f, 0.9f);

            var backBtnComp = backBtn.AddComponent<Button>();
            backBtnComp.onClick.AddListener(OnBackClicked);

            var backOutline = backBtn.AddComponent<Outline>();
            backOutline.effectColor = UIColorPalette.NEON_MAGENTA;
            backOutline.effectDistance = new Vector2(2, -2);

            var backTextGo = new GameObject("BackText");
            backTextGo.transform.SetParent(backBtn.transform, false);
            var backTextRect = backTextGo.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.offsetMin = Vector2.zero;
            backTextRect.offsetMax = Vector2.zero;

            var backTmp = backTextGo.AddComponent<TextMeshProUGUI>();
            backTmp.text = "◀";
            backTmp.fontSize = 32;
            backTmp.fontStyle = FontStyles.Bold;
            backTmp.color = UIColorPalette.NEON_MAGENTA;
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.raycastTarget = false;

            // 타이틀 텍스트 (중앙) - 큰 글씨 + 글로우 효과
            var textGo = new GameObject("TitleText");
            textGo.transform.SetParent(titleBar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(95, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "MY MUSIC";
            tmp.fontSize = 42;
            tmp.color = UIColorPalette.NEON_CYAN_BRIGHT;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 6f;

            // 타이틀 글로우 효과
            var titleOutline = textGo.AddComponent<Outline>();
            titleOutline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.4f);
            titleOutline.effectDistance = new Vector2(2, -2);

            var korFont = KoreanFontManager.KoreanFont;
            if (korFont != null) tmp.font = korFont;
        }

        /// <summary>
        /// 음악 파일을 스캔하여 라이브러리에 자동 등록
        /// Android: MediaStore API (IS_MUSIC 필터 + 녹음 제외)
        /// Editor/PC: 파일 시스템 폴백
        /// </summary>
        private void ScanAndRegisterStreamingAssets()
        {
            if (SongLibraryManager.Instance == null) return;

            // AudioFileName이 비어있는 기존 곡 정리
            var existingSongs = SongLibraryManager.Instance.GetAllSongs();
            foreach (var song in existingSongs)
            {
                if (string.IsNullOrEmpty(song.AudioFileName))
                {
                    SongLibraryManager.Instance.DeleteSong(song.Title);
                }
            }

            // 1) StreamingAssets 내장 곡 등록
            string[] builtInFiles = { "jpop_energetic.mp3" };
            foreach (var fileName in builtInFiles)
            {
                RegisterSongFile(fileName, "streaming");
            }

            // 2) 권한 확인 후 로컬 음악 스캔 (MediaStore API)
            if (AndroidMusicScanner.HasPermission())
            {
                ScanLocalMusic();
            }
            else
            {
                AndroidMusicScanner.RequestPermission(granted =>
                {
                    if (granted)
                    {
                        ScanLocalMusic();
                        songLibraryUI?.RefreshSongList();
                    }
                    else
                    {
                        Debug.LogWarning("[SongSelectUI] 음악 파일 접근 권한이 거부됨");
                        // 권한 없이도 접근 가능한 폴더만 스캔
                        string musicPath = Path.Combine(Application.persistentDataPath, "Music");
                        if (!Directory.Exists(musicPath)) Directory.CreateDirectory(musicPath);
                        ScanFolderForAudio(musicPath, "persistent");
                    }
                });
            }
        }

        /// <summary>
        /// AndroidMusicScanner를 통해 로컬 음악 스캔 및 등록
        /// </summary>
        private void ScanLocalMusic()
        {
            var scannedList = AndroidMusicScanner.ScanMusicFiles();

            int addedCount = 0;
            foreach (var music in scannedList)
            {
                var record = AndroidMusicScanner.ToSongRecord(music);
                if (SongLibraryManager.Instance.AddSong(record))
                    addedCount++;
            }

            Debug.Log($"[SongSelectUI] 로컬 음악 스캔: {scannedList.Count}곡 발견, {addedCount}곡 새로 등록");
        }

        private void ScanFolderForAudio(string folderPath, string source)
        {
            string[] extensions = { "*.mp3", "*.wav", "*.ogg", "*.m4a", "*.flac" };
            foreach (var ext in extensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(folderPath, ext);
                    foreach (var filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);
                        RegisterSongFile(fileName, source, filePath);
                    }
                }
                catch (System.Exception) { }
            }
        }

        private void RegisterSongFile(string fileName, string source, string fullPath = null)
        {
            string titleFromFile = Path.GetFileNameWithoutExtension(fileName).Replace("_", " ");

            string audioRef;
            if (source == "persistent")
                audioRef = "music:" + fileName;
            else if (source == "external" && fullPath != null)
                audioRef = "ext:" + fullPath;
            else
                audioRef = fileName;

            var record = new SongRecord
            {
                Title = titleFromFile,
                Artist = "Unknown",
                Genre = "EDM",
                Mood = "Energetic",
                BPM = 0,
                DifficultyLevel = 5,
                Duration = 0,
                AudioFileName = audioRef
            };

            SongLibraryManager.Instance.AddSong(record);
        }

        private void OnBackClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        // 설정 패널/FAB 참조
        private GameObject settingsFAB;
        private GameObject settingsPanel;

        /// <summary>
        /// 설정 FAB 버튼 (우하단 플로팅 액션 버튼)
        /// 이퀄라이저 바 위에 배치, 네온 시안 색상
        /// </summary>
        private void CreateSettingsFAB()
        {
            var existing = transform.Find("SettingsFAB");
            if (existing != null) DestroyImmediate(existing.gameObject);

            settingsFAB = new GameObject("SettingsFAB");
            settingsFAB.transform.SetParent(transform, false);

            var rect = settingsFAB.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-25, 140); // 이퀄라이저(120px) 위
            rect.sizeDelta = new Vector2(70, 70);

            // 원형 배경 (시안)
            var bg = settingsFAB.AddComponent<Image>();
            bg.color = UIColorPalette.NEON_CYAN;

            // 그림자
            var shadow = settingsFAB.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(3, -3);

            // 글로우 아웃라인
            var outline = settingsFAB.AddComponent<Outline>();
            outline.effectColor = UIColorPalette.NEON_CYAN.WithAlpha(0.6f);
            outline.effectDistance = new Vector2(2, 2);

            // 기어 아이콘
            var iconGo = new GameObject("SettingsIcon");
            iconGo.transform.SetParent(settingsFAB.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconText = iconGo.AddComponent<TextMeshProUGUI>();
            iconText.text = "\u2699"; // ⚙ 기어 유니코드
            iconText.fontSize = 40;
            iconText.color = Color.white;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.fontStyle = FontStyles.Bold;
            iconText.raycastTarget = false;

            // 버튼 컴포넌트
            var btn = settingsFAB.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(ToggleSettingsPanel);
        }

        /// <summary>
        /// 설정 패널 표시/숨기기 토글
        /// </summary>
        private void ToggleSettingsPanel()
        {
            if (settingsPanel == null)
                CreateSettingsPanel();

            bool isActive = settingsPanel.activeSelf;
            settingsPanel.SetActive(!isActive);

            if (!isActive)
            {
                // 열기 애니메이션
                settingsPanel.transform.localScale = Vector3.zero;
                UIAnimator.ScaleTo(this, settingsPanel, Vector3.one, 0.25f);
            }
        }

        /// <summary>
        /// 인라인 설정 패널 (노트 속도, 판정 오프셋, 배경 밝기)
        /// </summary>
        private void CreateSettingsPanel()
        {
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(transform, false);

            var panelRect = settingsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 반투명 배경 (터치 차단)
            var overlay = settingsPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0.02f, 0.85f);
            overlay.raycastTarget = true;

            // 설정 카드 (가운데)
            var card = new GameObject("SettingsCard");
            card.transform.SetParent(settingsPanel.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.08f, 0.2f);
            cardRect.anchorMax = new Vector2(0.92f, 0.8f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.02f, 0.02f, 0.08f, 0.95f);

            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = UIColorPalette.NEON_CYAN;
            cardOutline.effectDistance = new Vector2(2, -2);

            // 세로 레이아웃
            var vLayout = card.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(30, 30, 25, 25);
            vLayout.spacing = 20;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 타이틀
            CreateSettingsLabel(card.transform, "설정", 42, UIColorPalette.NEON_CYAN_BRIGHT, 60);

            // 노트 속도 슬라이더
            float noteSpeed = SettingsManager.Instance != null ? SettingsManager.Instance.NoteSpeed : 5f;
            CreateSettingsSlider(card.transform, "노트 속도", noteSpeed, 1f, 10f, (val) =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.NoteSpeed = val;
            });

            // 판정 오프셋 슬라이더
            float offset = SettingsManager.Instance != null ? SettingsManager.Instance.JudgementOffset * 1000f : 0f;
            CreateSettingsSlider(card.transform, "판정 오프셋 (ms)", offset, -100f, 100f, (val) =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.JudgementOffset = val / 1000f;
            });

            // BGM 볼륨
            float bgmVol = SettingsManager.Instance != null ? SettingsManager.Instance.BGMVolume * 100f : 80f;
            CreateSettingsSlider(card.transform, "음악 볼륨", bgmVol, 0f, 100f, (val) =>
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.BGMVolume = val / 100f;
            });

            // 닫기 버튼
            var closeBtn = UIButtonStyleHelper.CreateStyledButton(card.transform, "CloseSettingsBtn", "닫기",
                preferredHeight: 70f, fontSize: 32f);
            closeBtn.onClick.AddListener(() => settingsPanel.SetActive(false));

            // 최상위 렌더링
            settingsPanel.transform.SetAsLastSibling();

            // 한국어 폰트 적용
            KoreanFontManager.ApplyFontToAll(settingsPanel);
        }

        private void CreateSettingsLabel(Transform parent, string text, float fontSize, Color color, float height)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
        }

        private void CreateSettingsSlider(Transform parent, string label, float value, float min, float max, System.Action<float> onChanged)
        {
            var container = new GameObject("Slider_" + label);
            container.transform.SetParent(parent, false);
            var containerLE = container.AddComponent<LayoutElement>();
            containerLE.preferredHeight = 90;

            var vLayout = container.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 5;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;

            // 라벨 + 값
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(container.transform, false);
            var labelLE = labelGo.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 30;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = $"{label}: {value:F0}";
            labelTmp.fontSize = 24;
            labelTmp.color = UIColorPalette.NEON_CYAN_BRIGHT;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 슬라이더
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(container.transform, false);
            var sliderLE = sliderGo.AddComponent<LayoutElement>();
            sliderLE.preferredHeight = 40;

            // 슬라이더 배경
            var sliderBgGo = new GameObject("Background");
            sliderBgGo.transform.SetParent(sliderGo.transform, false);
            var sliderBgRect = sliderBgGo.AddComponent<RectTransform>();
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.offsetMin = Vector2.zero;
            sliderBgRect.offsetMax = Vector2.zero;
            var sliderBgImg = sliderBgGo.AddComponent<Image>();
            sliderBgImg.color = new Color(0.05f, 0.05f, 0.12f, 0.9f);

            // 필 영역
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = UIColorPalette.NEON_MAGENTA;

            // 핸들 영역
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30, 0);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            // Slider 컴포넌트
            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = (max - min > 10); // 범위가 크면 정수

            slider.onValueChanged.AddListener((val) =>
            {
                labelTmp.text = $"{label}: {val:F0}";
                onChanged?.Invoke(val);
            });
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
        }
    }
}
