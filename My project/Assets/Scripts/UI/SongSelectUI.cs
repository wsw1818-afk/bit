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
