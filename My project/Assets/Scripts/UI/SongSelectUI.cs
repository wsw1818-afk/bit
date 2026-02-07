using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;

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

        private void Start()
        {
            EnsureCanvasScaler();
            CreateBITBackground();
            AutoSetupReferences();
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
            img.raycastTarget = false; // 배경은 터치 차단 안 함
            var tex = Resources.Load<Texture2D>("UI/BIT");
            if (tex != null)
            {
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = new Color(1f, 1f, 1f, 0.35f);
            }
            else
            {
                img.color = UIColorPalette.BG_DEEP;
            }

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
            containerRect.sizeDelta = new Vector2(0, 100);

            int barCount = 25;
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

            // 뒤로가기 버튼 스타일 (타이틀 바 왼쪽에 배치)
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
                var backRect = backButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    // 왼쪽 상단 고정, 타이틀 바(120px) 안에 수직 정렬
                    backRect.anchorMin = new Vector2(0, 1);
                    backRect.anchorMax = new Vector2(0, 1);
                    backRect.pivot = new Vector2(0, 1);
                    backRect.anchoredPosition = new Vector2(10, -10);
                    backRect.sizeDelta = new Vector2(200, 100);
                }
                // 투명 배경 (타이틀 바 위에 겹치므로 별도 배경 불필요)
                var btnImg = backButton.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = new Color(0f, 0f, 0f, 0f);
                // 기존 outline 제거
                var outline = backButton.GetComponent<Outline>();
                if (outline != null)
                    Destroy(outline);
                // 텍스트 스타일
                var btnTmp = backButton.GetComponentInChildren<TMP_Text>();
                if (btnTmp != null)
                {
                    btnTmp.text = "\u2190 뒤로";
                    btnTmp.fontSize = 48;
                    btnTmp.fontStyle = FontStyles.Bold;
                    btnTmp.color = UIColorPalette.NEON_MAGENTA;
                    btnTmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }

            // 타이틀 바 생성
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
            rect.sizeDelta = new Vector2(0, 120);

            var bg = titleBar.AddComponent<Image>();
            bg.color = UIColorPalette.BG_TOPBAR;

            // 타이틀 텍스트 (왼쪽 여백 확보하여 뒤로 버튼과 겹치지 않게)
            var textGo = new GameObject("TitleText");
            textGo.transform.SetParent(titleBar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(200, 0); // 왼쪽 200px 여백 (뒤로 버튼 공간)
            textRect.offsetMax = new Vector2(-20, 0);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "내 라이브러리";
            tmp.fontSize = 52;
            tmp.color = UIColorPalette.NEON_CYAN_BRIGHT;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.1f;
            tmp.outlineColor = new Color32(0, 100, 255, 150);
        }

        /// <summary>
        /// 음악 파일을 스캔하여 라이브러리에 자동 등록
        /// 1) StreamingAssets 내장 곡 (빌드 시 포함, Android 호환)
        /// 2) persistentDataPath/Music 폴더 (사용자 추가 곡)
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
            // 빌드 시 포함된 파일 목록을 하드코딩
            string[] builtInFiles = { "jpop_energetic.mp3" };
            foreach (var fileName in builtInFiles)
            {
                RegisterSongFile(fileName, "streaming");
            }

            // 2) persistentDataPath/Music 폴더 스캔 (사용자 추가 곡 — 모든 플랫폼 지원)
            string musicPath = Path.Combine(Application.persistentDataPath, "Music");
            if (Directory.Exists(musicPath))
            {
                string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
                foreach (var ext in extensions)
                {
                    string[] files = Directory.GetFiles(musicPath, ext);
                    foreach (var filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);
                        RegisterSongFile(fileName, "persistent");
                    }
                }
            }
            else
            {
                // Music 폴더가 없으면 생성 (사용자가 곡을 넣을 수 있도록)
                Directory.CreateDirectory(musicPath);
                Debug.Log($"[SongSelect] Music 폴더 생성: {musicPath}");
            }

#if !UNITY_ANDROID || UNITY_EDITOR
            // PC/에디터에서는 StreamingAssets 직접 스캔도 가능
            string streamingPath = Application.streamingAssetsPath;
            if (Directory.Exists(streamingPath))
            {
                string[] extensions = { "*.mp3", "*.wav", "*.ogg" };
                foreach (var ext in extensions)
                {
                    string[] files = Directory.GetFiles(streamingPath, ext);
                    foreach (var filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);
                        RegisterSongFile(fileName, "streaming");
                    }
                }
            }
#endif
        }

        private void RegisterSongFile(string fileName, string source)
        {
            string titleFromFile = Path.GetFileNameWithoutExtension(fileName)
                .Replace("_", " ");

            // source prefix 추가하여 로드 시 구분
            string audioRef = source == "persistent" ? "music:" + fileName : fileName;

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
        }
    }
}
