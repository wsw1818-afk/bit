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
    /// 곡 선택 UI - BIT.jpg 네온 사이버펑크 디자인
    /// 배경 이미지 + 이퀄라이저 바 + 곡 라이브러리
    /// </summary>
    public class SongSelectUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button backButton;

        // 라이브러리 UI
        private SongLibraryUI songLibraryUI;

        // 이퀄라이저 바
        private List<Image> eqBars = new List<Image>();
        private Coroutine eqAnimCoroutine;

        // 하단 버튼
        private Button playButton;

        private void Start()
        {
            Debug.Log("[SongSelectUI] Start() 호출됨");

            // 에디터에서 포커스 손실 시에도 게임 루프 유지 (MCP 스크린샷 캡처용)
            Application.runInBackground = true;

            // TMP_Text 생성 전에 한국어 폰트를 글로벌 기본값으로 설정
            var _ = KoreanFontManager.KoreanFont;

            EnsureEventSystem(); // 터치/클릭 입력을 위한 EventSystem 보장
            EnsureCanvasScaler();
            CreateBITBackground();
            AutoSetupReferences();
            RequestStoragePermissionAndInitialize();
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

        private void RequestStoragePermissionAndInitialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 13+ (API 33): READ_MEDIA_AUDIO 권한 요청
            if (!Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_AUDIO")
                && !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += (perm) => {
                    Debug.Log($"[SongSelect] 권한 허용: {perm}");
                    FinishInitialize();
                };
                callbacks.PermissionDenied += (perm) => {
                    Debug.LogWarning($"[SongSelect] 권한 거부: {perm} — 내부 저장소만 사용");
                    FinishInitialize();
                };
                callbacks.PermissionDeniedAndDontAskAgain += (perm) => {
                    Debug.LogWarning($"[SongSelect] 권한 영구 거부: {perm}");
                    FinishInitialize();
                };
                // API 33+ 먼저 시도, 실패 시 레거시 권한
                Permission.RequestUserPermission("android.permission.READ_MEDIA_AUDIO", callbacks);
            }
            else
            {
                FinishInitialize();
            }
#else
            FinishInitialize();
#endif
        }

        private void FinishInitialize()
        {
            Initialize();
            CreateEqualizerBar();
            EnsureSiblingOrder();
            EnsureSafeArea();
        }

        /// <summary>
        /// BIT.jpg 배경 이미지 + 어두운 오버레이
        /// </summary>
        private void CreateBITBackground()
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
            img.type = Image.Type.Sliced;
            
            // Legacy Resource Load 제거됨

            // 오버레이
            var overlayGo = new GameObject("DarkOverlay");
            overlayGo.transform.SetParent(transform, false);
            overlayGo.transform.SetSiblingIndex(1);
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.raycastTarget = false; // 오버레이도 터치 차단 안 함
            overlayImg.color = new Color(0.01f, 0.005f, 0.04f, 0.65f);
        }

        /// <summary>
        /// 하단 이퀄라이저 바 (BIT.jpg 스타일)
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
            containerRect.sizeDelta = new Vector2(0, 150); // Height increased for dramatic effect

            int barCount = 30; // More bars
            float barWidth = 1f / barCount;

            for (int i = 0; i < barCount; i++)
            {
                var barGo = new GameObject($"EqBar_{i}");
                barGo.transform.SetParent(eqContainer.transform, false);
                var barRect = barGo.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(i * barWidth + 0.002f, 0);
                barRect.anchorMax = new Vector2((i + 1) * barWidth - 0.002f, 0.4f);
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;

                var barImg = barGo.AddComponent<Image>();
                barImg.raycastTarget = false; // 이퀄라이저 바는 터치 차단 안 함
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

        private void AutoSetupReferences()
        {
            // backButton: 씬에 "BackButton" 존재
            if (backButton == null)
            {
                var backObj = transform.Find("BackButton");
                if (backObj != null)
                    backButton = backObj.GetComponent<Button>();
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

            // 뒤로가기 버튼 — 슬림 아이콘 "◀" (56x56px)
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
                var backRect = backButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    backRect.anchorMin = new Vector2(0, 1);
                    backRect.anchorMax = new Vector2(0, 1);
                    backRect.pivot = new Vector2(0, 1);
                    backRect.anchoredPosition = new Vector2(8, -12);
                    backRect.sizeDelta = new Vector2(56, 56);
                }
                // 반투명 배경 (터치 영역 시각화)
                var btnImg = backButton.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = new Color(0.06f, 0.04f, 0.18f, 0.7f);
                // 기존 outline 제거 후 마젠타 테두리
                var outline = backButton.GetComponent<Outline>();
                if (outline == null)
                    outline = backButton.gameObject.AddComponent<Outline>();
                outline.effectColor = UIColorPalette.NEON_MAGENTA.WithAlpha(0.5f);
                outline.effectDistance = new Vector2(1, -1);
                // 아이콘만 표시
                var btnTmp = backButton.GetComponentInChildren<TMP_Text>();
                if (btnTmp != null)
                {
                    btnTmp.text = "<";
                    btnTmp.fontSize = 28;
                    btnTmp.fontStyle = FontStyles.Bold;
                    btnTmp.color = UIColorPalette.NEON_MAGENTA;
                    btnTmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // 타이틀 바 생성
            CreateTitleBar();

            // 하단 버튼 패널 생성 (플레이, 설정, 뒤로)
            CreateBottomButtonPanel();

            // 라이브러리 UI 초기화 + 표시
            songLibraryUI = gameObject.AddComponent<SongLibraryUI>();
            var parentRect = GetComponent<RectTransform>();
            songLibraryUI.Initialize(parentRect);
            songLibraryUI.Show(true);

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(gameObject);
        }

        /// <summary>
        /// 하단 버튼 패널 생성 (플레이, 설정, 뒤로)
        /// </summary>
        private void CreateBottomButtonPanel()
        {
            if (transform.Find("BottomButtonPanel") != null) return;

            var panel = new GameObject("BottomButtonPanel");
            panel.transform.SetParent(transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.14f);
            panelRect.anchorMax = new Vector2(0.98f, 0.58f);  // 더 넓은 영역
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 배경 제거 - 버튼만 보이게
            // (패널 배경이 버튼을 가리지 않도록)

            var vLayout = panel.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 15;  // 버튼 간 여백
            vLayout.padding = new RectOffset(8, 8, 10, 10);
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 플레이 버튼
            playButton = CreateStyledButton(panel.transform, "플레이", "PLAY", UIColorPalette.NEON_MAGENTA);
            playButton.onClick.AddListener(OnPlayClicked);

            // 뒤로 버튼 (설정은 MainMenu에서 접근)
            var backBtn = CreateStyledButton(panel.transform, "뒤로", "BACK", UIColorPalette.NEON_CYAN);
            backBtn.onClick.AddListener(OnBackClicked);

            Debug.Log("[SongSelectUI] 하단 버튼 패널 생성 완료");
        }

        /// <summary>
        /// 프리미엄 스타일 버튼 생성
        /// </summary>
        private Button CreateStyledButton(Transform parent, string mainText, string subText, Color accentColor)
        {
            var btnGo = new GameObject($"Btn_{mainText}");
            btnGo.transform.SetParent(parent, false);

            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(0, 110f);

            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = 110f;
            le.minHeight = 110f;

            // 배경 (확실하게 보이는 진한 색)
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.18f, 0.12f, 0.32f, 1f);  // 불투명, 진한 보라

            // 버튼 컴포넌트
            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.12f, 0.32f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.20f, 0.45f, 1f);
            colors.pressedColor = accentColor.WithAlpha(0.9f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;

            // 다중 테두리로 네온 글로우 효과
            var outline1 = btnGo.AddComponent<Outline>();
            outline1.effectColor = accentColor;
            outline1.effectDistance = new Vector2(2f, -2f);

            var outline2 = btnGo.AddComponent<Outline>();
            outline2.effectColor = accentColor.WithAlpha(0.6f);
            outline2.effectDistance = new Vector2(4f, -4f);

            var shadow = btnGo.AddComponent<Shadow>();
            shadow.effectColor = accentColor.WithAlpha(0.4f);
            shadow.effectDistance = new Vector2(6f, -6f);

            // === 좌측 악센트 바 (20px, 전체 높이) ===
            var accentBar = new GameObject("AccentBar");
            accentBar.transform.SetParent(btnGo.transform, false);
            var accentRect = accentBar.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(20, 0);  // 20px, 전체 높이
            var accentImg = accentBar.AddComponent<Image>();
            accentImg.color = accentColor;
            accentImg.raycastTarget = false;

            // === 메인 텍스트 (한국어) - 48pt ===
            var mainTextGo = new GameObject("MainText");
            mainTextGo.transform.SetParent(btnGo.transform, false);
            var mainTextRect = mainTextGo.AddComponent<RectTransform>();
            mainTextRect.anchorMin = new Vector2(0, 0.38f);
            mainTextRect.anchorMax = new Vector2(0.80f, 1);
            mainTextRect.pivot = new Vector2(0, 0.5f);
            mainTextRect.offsetMin = new Vector2(32, 0);  // 악센트바(20) + 여백(12)
            mainTextRect.offsetMax = new Vector2(0, -5);
            var mainTmp = mainTextGo.AddComponent<TextMeshProUGUI>();
            mainTmp.text = mainText;
            mainTmp.fontSize = 48;
            mainTmp.fontStyle = FontStyles.Bold;
            mainTmp.color = Color.white;
            mainTmp.alignment = TextAlignmentOptions.BottomLeft;
            mainTmp.overflowMode = TextOverflowModes.Overflow;
            mainTmp.raycastTarget = false;

            // === 서브 텍스트 (영어) - 18pt ===
            var subTextGo = new GameObject("SubText");
            subTextGo.transform.SetParent(btnGo.transform, false);
            var subTextRect = subTextGo.AddComponent<RectTransform>();
            subTextRect.anchorMin = new Vector2(0, 0);
            subTextRect.anchorMax = new Vector2(0.80f, 0.40f);
            subTextRect.pivot = new Vector2(0, 0.5f);
            subTextRect.offsetMin = new Vector2(32, 5);
            subTextRect.offsetMax = new Vector2(0, 0);
            var subTmp = subTextGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = subText;
            subTmp.fontSize = 18;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.color = accentColor;
            subTmp.characterSpacing = 6f;
            subTmp.alignment = TextAlignmentOptions.TopLeft;
            subTmp.overflowMode = TextOverflowModes.Overflow;
            subTmp.raycastTarget = false;

            // === 우측 화살표 (▶ 48pt) ===
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(btnGo.transform, false);
            var arrowRect = arrowGo.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.offsetMin = new Vector2(-75, 0);
            arrowRect.offsetMax = new Vector2(-15, 0);
            var arrowTmp = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowTmp.text = "▶";
            arrowTmp.fontSize = 48;
            arrowTmp.fontStyle = FontStyles.Bold;
            arrowTmp.color = accentColor;
            arrowTmp.alignment = TextAlignmentOptions.Center;
            arrowTmp.raycastTarget = false;

            return btn;
        }

        private void OnPlayClicked()
        {
            Debug.Log("[SongSelectUI] 플레이 버튼 클릭");
            // 디버그 게임 시작 (빈 SongData로 시작 - GameplayController가 테스트 곡 생성)
            GameManager.Instance?.StartGame(null);
        }

        /// <summary>
        /// 렌더링 순서 보장
        /// </summary>
        private void EnsureSiblingOrder()
        {
            var bitBg = transform.Find("BIT_Background");
            if (bitBg != null) bitBg.SetAsFirstSibling();
            var overlay = transform.Find("DarkOverlay");
            if (overlay != null) overlay.SetSiblingIndex(1);

            // LibraryPanel은 SongLibraryUI가 생성 → index 1
            // TitleBar, BackButton은 그 위에 렌더링
            var titleBar = transform.Find("TitleBar");
            if (titleBar != null) titleBar.SetAsLastSibling();

            var backBtn = transform.Find("BackButton");
            if (backBtn != null) backBtn.SetAsLastSibling();
        }

        /// <summary>
        /// 상단 타이틀 바 생성
        /// </summary>
        private void CreateTitleBar()
        {
            if (transform.Find("TitleBar") != null) return;

            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(transform, false);

            var rect = titleBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 80); // 슬림 헤더 (120→80px)

            var bg = titleBar.AddComponent<Image>();
            bg.color = UIColorPalette.BG_TOPBAR;
            bg.raycastTarget = false; // 터치 차단 방지

            // 타이틀 텍스트 (뒤로 버튼 56px + padding 후 시작)
            var textGo = new GameObject("TitleText");
            textGo.transform.SetParent(titleBar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(72, 0); // 뒤로 버튼(56) + 여백(16)
            textRect.offsetMax = new Vector2(-20, 0);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "\uB0B4 \uC74C\uC545"; // "내 음악"
            tmp.fontSize = 36;
            tmp.color = UIColorPalette.NEON_CYAN_BRIGHT;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontStyle = FontStyles.Bold;
            // 폰트를 먼저 적용해야 outlineWidth 설정 시 material null 방지
            var korFont = KoreanFontManager.KoreanFont;
            if (korFont != null) tmp.font = korFont;
            // tmp.outlineWidth = 0.1f; // Dynamic SDF에서 outline 비활성화
            // tmp.outlineColor = new Color32(0, 100, 255, 150);
        }

        /// <summary>
        /// 음악 파일을 스캔하여 라이브러리에 자동 등록
        /// 1) StreamingAssets 내장 곡 (빌드 시 포함, Android 호환)
        /// 2) persistentDataPath/Music 폴더 (사용자 추가 곡)
        /// 3) Android 외부 저장소 (Music, Download 폴더)
        /// </summary>
        private void ScanAndRegisterStreamingAssets()
        {
            if (SongLibraryManager.Instance == null) return;

            // AudioFileName이 비어있는 기존 곡 정리 (이전 버전 호환)
            var existingSongs = SongLibraryManager.Instance.GetAllSongs();
            foreach (var song in existingSongs)
            {
                if (string.IsNullOrEmpty(song.AudioFileName))
                {
                    SongLibraryManager.Instance.DeleteSong(song.Title);
                }
            }

            // 1) StreamingAssets 내장 곡 등록 (Android에서는 Directory.GetFiles 불가)
            string[] builtInFiles = { "jpop_energetic.mp3" };
            foreach (var fileName in builtInFiles)
            {
                RegisterSongFile(fileName, "streaming");
            }

            // 2) persistentDataPath/Music 폴더 스캔 (모든 플랫폼)
            string musicPath = Path.Combine(Application.persistentDataPath, "Music");
            if (Directory.Exists(musicPath))
            {
                ScanFolderForAudio(musicPath, "persistent");
            }
            else
            {
                Directory.CreateDirectory(musicPath);
                Debug.Log($"[SongSelect] Music 폴더 생성: {musicPath}");
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // 3) Android 외부 저장소 스캔 (Suno AI 등에서 다운로드한 곡)
            string sdcard = "/sdcard";
            string[] androidScanPaths = {
                Path.Combine(sdcard, "Music"),
                Path.Combine(sdcard, "Download"),
                Path.Combine(sdcard, "Downloads"),
                Path.Combine(sdcard, "DCIM", "Suno"),  // Suno AI 앱 저장 경로
                Path.Combine(sdcard, "Android", "media", "com.suno.android", "Music"), // Suno 앱 미디어
            };
            foreach (var scanPath in androidScanPaths)
            {
                if (Directory.Exists(scanPath))
                {
                    ScanFolderForAudio(scanPath, "external");
                    Debug.Log($"[SongSelect] Android 외부 폴더 스캔: {scanPath}");
                }
            }
#else
            // PC/에디터에서는 StreamingAssets 직접 스캔 + 사용자 Music 폴더
            string streamingPath = Application.streamingAssetsPath;
            if (Directory.Exists(streamingPath))
            {
                ScanFolderForAudio(streamingPath, "streaming");
            }

            // PC: 사용자 Music 폴더도 스캔
            string userMusicPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic);
            if (!string.IsNullOrEmpty(userMusicPath) && Directory.Exists(userMusicPath))
            {
                ScanFolderForAudio(userMusicPath, "external");
            }

            // PC: Downloads 폴더도 스캔 (Suno AI 웹에서 다운로드)
            string userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                string downloadsPath = Path.Combine(userProfile, "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    ScanFolderForAudio(downloadsPath, "external");
                }
            }
#endif
        }

        /// <summary>
        /// 지정 폴더에서 오디오 파일(mp3/wav/ogg) 스캔 후 등록
        /// </summary>
        private void ScanFolderForAudio(string folderPath, string source)
        {
            string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
            int count = 0;
            foreach (var ext in extensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(folderPath, ext);
                    foreach (var filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);
                        RegisterSongFile(fileName, source, filePath);
                        count++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SongSelect] 폴더 스캔 실패 ({folderPath}): {e.Message}");
                }
            }
            if (count > 0)
                Debug.Log($"[SongSelect] {folderPath}: {count}개 오디오 파일 발견");
        }

        private void RegisterSongFile(string fileName, string source, string fullPath = null)
        {
            string titleFromFile = Path.GetFileNameWithoutExtension(fileName)
                .Replace("_", " ");

            // source prefix로 로드 시 경로 구분
            string audioRef;
            if (source == "persistent")
                audioRef = "music:" + fileName;
            else if (source == "external" && fullPath != null)
                audioRef = "ext:" + fullPath;  // 외부 저장소: 전체 경로 저장
            else
                audioRef = fileName;  // StreamingAssets

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

        // SetupBackground 제거됨 — CreateBITBackground()로 대체

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
            if (backButton != null) backButton.onClick.RemoveAllListeners();
            if (playButton != null) playButton.onClick.RemoveAllListeners();
        }
    }
}
