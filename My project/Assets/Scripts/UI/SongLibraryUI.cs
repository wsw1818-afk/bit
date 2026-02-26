using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;
using AIBeat.Utils;

namespace AIBeat.UI
{
    /// <summary>
    /// 곡 라이브러리 UI - 리디자인 v2
    /// 하단 버튼 없이 곡 카드 터치로 바로 플레이
    /// </summary>
    public class SongLibraryUI : MonoBehaviour
    {
        // 색상
        private static readonly Color NEON_CYAN = UIColorPalette.BORDER_CYAN;
        private static readonly Color NEON_CYAN_BRIGHT = UIColorPalette.NEON_CYAN_BRIGHT;
        private static readonly Color CARD_BG = UIColorPalette.BG_CARD;

        // UI 참조
        private GameObject rootPanel;
        private Transform contentContainer;
        private ScrollRect scrollRect;
        private TextMeshProUGUI emptyText;
        private TextMeshProUGUI songCountText;

        // 곡 아이템 리스트
        private List<GameObject> songItems = new List<GameObject>();
        private List<SongRecord> displayedSongs = new List<SongRecord>();

        // 앨범 아트 기본값
        private Sprite defaultAlbumArt;

        // 삭제 확인 상태
        private int deleteConfirmIndex = -1;

        // 난이도 선택 팝업
        private GameObject difficultyPopup;
        private SongRecord pendingSong;

        public void Initialize(RectTransform parentPanel)
        {
            if (rootPanel != null) return;

            // 기본 앨범 아트 로드
            defaultAlbumArt = ResourceHelper.LoadSpriteFromResources("AIBeat_Design/UI/Default_Album_Art");

            CreateLibraryUI(parentPanel);
            KoreanFontManager.ApplyFontToAll(rootPanel);
            RefreshSongList();

            if (SongLibraryManager.Instance != null)
                SongLibraryManager.Instance.OnLibraryChanged += RefreshSongList;
        }

        private void CreateLibraryUI(RectTransform parent)
        {
            // 루트 패널 (타이틀바 100px 아래 ~ 이퀄라이저 120px 위)
            rootPanel = new GameObject("LibraryPanel");
            rootPanel.transform.SetParent(parent, false);
            var rootRect = rootPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(0, 120);  // 이퀄라이저 바(120px) 위부터
            rootRect.offsetMax = new Vector2(0, -100); // 타이틀 바(100px) 아래부터

            // 배경 없음 (투명) - 메인 배경 보이게
            var rootBg = rootPanel.AddComponent<Image>();
            rootBg.color = Color.clear;
            rootBg.raycastTarget = false;

            // ScrollRect 곡 목록 (먼저 생성)
            CreateScrollArea(rootPanel.transform);

            // 곡 수 표시 바 (나중에 생성 - 앞에 렌더링)
            CreateSongCountBar(rootPanel.transform);
        }

        private void CreateSongCountBar(Transform parent)
        {
            var countBar = new GameObject("CountBar");
            countBar.transform.SetParent(parent, false);
            var countRect = countBar.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0, 1);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.pivot = new Vector2(0.5f, 1);
            countRect.anchoredPosition = Vector2.zero;
            countRect.sizeDelta = new Vector2(0, 55);

            // 배경 (반투명 진한 보라)
            var bgImg = countBar.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.03f, 0.15f, 0.9f);
            bgImg.raycastTarget = false;

            // 하단 얇은 라인 (마젠타)
            var bottomLine = new GameObject("BottomLine");
            bottomLine.transform.SetParent(countBar.transform, false);
            var lineRect = bottomLine.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(1, 0);
            lineRect.pivot = new Vector2(0.5f, 0);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(0, 2);
            var lineImg = bottomLine.AddComponent<Image>();
            lineImg.color = UIColorPalette.NEON_MAGENTA.WithAlpha(0.6f);
            lineImg.raycastTarget = false;

            var textGo = new GameObject("SongCount");
            textGo.transform.SetParent(countBar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            songCountText = textGo.AddComponent<TextMeshProUGUI>();
            songCountText.text = "0곡";
            songCountText.fontSize = 32;
            songCountText.color = Color.white;
            songCountText.alignment = TextAlignmentOptions.Center;
            songCountText.fontStyle = FontStyles.Normal;
        }

        private void CreateScrollArea(Transform parent)
        {
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.offsetMin = new Vector2(10, 8);
            scrollViewRect.offsetMax = new Vector2(-10, -60);

            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.clear;
            viewport.AddComponent<RectMask2D>();

            scrollRect.viewport = viewportRect;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            contentContainer = content.transform;
            scrollRect.content = contentRect;

            // 빈 목록 텍스트
            var emptyGo = new GameObject("EmptyText");
            emptyGo.transform.SetParent(content.transform, false);
            var emptyRect = emptyGo.AddComponent<RectTransform>();
            emptyRect.sizeDelta = new Vector2(0, 600);
            var emptyLayout = emptyGo.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 600;

            emptyText = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyText.text = "\n\n<size=52>음악을 추가해보세요!</size>\n\n<size=30>핸드폰의 Music 또는\nDownloads 폴더에\nMP3 파일을 넣으면\n자동으로 인식됩니다</size>\n\n<size=26><color=#00D9FF>Suno AI로 만든 음악도\n바로 플레이 가능!</color></size>";
            emptyText.fontSize = 40;
            emptyText.color = new Color(0.7f, 0.7f, 0.8f);
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.richText = true;
        }

        public void RefreshSongList()
        {
            foreach (var item in songItems)
            {
                if (item != null) Destroy(item);
            }
            songItems.Clear();
            deleteConfirmIndex = -1;

            var manager = SongLibraryManager.Instance;
            if (manager == null)
            {
                UpdateEmptyState(true);
                return;
            }

            List<SongRecord> songs = manager.GetSongsSortedByDate();
            displayedSongs = songs;

            if (songCountText != null)
                songCountText.text = songs.Count > 0
                    ? $"{songs.Count}곡 · 터치하여 플레이"
                    : "0곡";

            UpdateEmptyState(songs.Count == 0);

            for (int i = 0; i < songs.Count; i++)
            {
                CreateSongCard(songs[i], i);
            }

            StartCoroutine(AnimateCardsEntrance());
            KoreanFontManager.ApplyFontToAll(rootPanel);

            Canvas.ForceUpdateCanvases();
            if (contentContainer != null)
            {
                var contentRect = contentContainer.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
        }

        /// <summary>
        /// 곡 카드 생성 - 컴팩트 2행 구조 + 앨범 아트
        /// </summary>
        private void CreateSongCard(SongRecord song, int index)
        {
            var card = new GameObject($"SongCard_{index}");
            card.transform.SetParent(contentContainer, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0, 140);
            var cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 140;

            var cardBg = card.AddComponent<Image>();
            cardBg.color = CARD_BG;

            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = NEON_CYAN;
            cardOutline.effectDistance = new Vector2(1, -1);

            var cardButton = card.AddComponent<Button>();
            var cardColors = cardButton.colors;
            cardColors.normalColor = Color.white;
            cardColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            cardColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cardButton.colors = cardColors;
            int capturedIndex = index;
            cardButton.onClick.AddListener(() => OnSongCardClicked(capturedIndex));

            var hLayout = card.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(14, 10, 8, 8);
            hLayout.spacing = 10;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // 1. 앨범 아트 (좌측)
            var artGo = new GameObject("AlbumArt");
            artGo.transform.SetParent(card.transform, false);
            var artLayout = artGo.AddComponent<LayoutElement>();
            artLayout.preferredWidth = 120;
            artLayout.preferredHeight = 120;
            
            var artImg = artGo.AddComponent<Image>();
            artImg.sprite = defaultAlbumArt;
            artImg.color = Color.white;
            artImg.preserveAspect = true;

            // 앨범 아트 테두리
            var artOutline = artGo.AddComponent<Outline>();
            artOutline.effectColor = new Color(0f, 1f, 1f, 0.5f);
            artOutline.effectDistance = new Vector2(1, -1);

            var artMask = artGo.AddComponent<Mask>(); // (Optional) Rounded Mask could be better
            artMask.showMaskGraphic = true;

            // 좌측 정보 패널
            var infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(card.transform, false);
            infoPanel.AddComponent<RectTransform>();
            var infoLayout = infoPanel.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;

            var infoVLayout = infoPanel.AddComponent<VerticalLayoutGroup>();
            infoVLayout.spacing = 4;
            infoVLayout.childControlWidth = true;
            infoVLayout.childControlHeight = true;
            infoVLayout.childForceExpandWidth = true;
            infoVLayout.childForceExpandHeight = false;
            infoVLayout.childAlignment = TextAnchor.MiddleLeft;

            // Row 1: 곡 제목
            string displayTitle = FormatTitle(song.Title);
            CreateTMPText(infoPanel, "Title", displayTitle, 36, Color.white,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            // Row 2: 아티스트 · BPM · 난이도
            string artistStr = !string.IsNullOrEmpty(song.Artist) && song.Artist != "Unknown"
                ? song.Artist : "";
            string diffStars = "Lv." + Mathf.Clamp(song.DifficultyLevel, 0, 10);
            string playsStr = song.PlayCount > 0 ? $"{song.PlayCount}회" : "NEW";
            string row2;
            if (!string.IsNullOrEmpty(artistStr))
                row2 = song.BPM > 0
                    ? $"{artistStr} · {song.BPM} BPM · {playsStr}"
                    : $"{artistStr} · {playsStr}";
            else
                row2 = song.BPM > 0
                    ? $"{song.BPM} BPM · {diffStars} · {playsStr}"
                    : $"{diffStars} · {playsStr}";
            CreateTMPText(infoPanel, "Info", row2, 24,
                NEON_CYAN_BRIGHT, TextAlignmentOptions.MidlineLeft);

            // Row 3: 랭크 + 점수
            string rankDisplay = string.IsNullOrEmpty(song.BestRank) ? "-" : song.BestRank;
            string scoreDisplay = song.BestScore > 0 ? song.BestScore.ToString("N0") : "--";
            var rankTmp = CreateTMPText(infoPanel, "RankScore",
                $"Best: <color=#{ColorUtility.ToHtmlStringRGB(GetRankColor(song.BestRank))}>{rankDisplay}</color> · {scoreDisplay}",
                22, new Color(0.75f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft);
            rankTmp.richText = true;

            // 우측 플레이 아이콘
            var playPanel = new GameObject("PlayPanel");
            playPanel.transform.SetParent(card.transform, false);
            playPanel.AddComponent<RectTransform>();
            var playLayout = playPanel.AddComponent<LayoutElement>();
            playLayout.preferredWidth = 70;

            var playIconGo = new GameObject("PlayIcon");
            playIconGo.transform.SetParent(playPanel.transform, false);
            var playIconRect = playIconGo.AddComponent<RectTransform>();
            playIconRect.anchorMin = Vector2.zero;
            playIconRect.anchorMax = Vector2.one;
            playIconRect.offsetMin = Vector2.zero;
            playIconRect.offsetMax = Vector2.zero;

            var playIconTmp = playIconGo.AddComponent<TextMeshProUGUI>();
            playIconTmp.text = "▶";
            playIconTmp.fontSize = 44;
            playIconTmp.color = UIColorPalette.NEON_MAGENTA;
            playIconTmp.alignment = TextAlignmentOptions.Center;
            playIconTmp.raycastTarget = false;

            songItems.Add(card);
        }

        private string FormatTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;
            var words = title.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        private void OnSongCardClicked(int index)
        {
            if (index < 0 || index >= displayedSongs.Count) return;

            var song = displayedSongs[index];
            Debug.Log($"[SongLibrary] 곡 선택: {song.Title}");

            // 난이도 선택 팝업 표시
            pendingSong = song;
            ShowDifficultyPopup();
        }

        /// <summary>
        /// 선택된 난이도로 게임 시작
        /// </summary>
        private void StartGameWithDifficulty(int difficultyLevel)
        {
            if (pendingSong == null) return;

            Application.runInBackground = true;
            pendingSong.DifficultyLevel = difficultyLevel;
            CloseDifficultyPopup();
            Debug.Log($"[SongLibrary] 난이도 {difficultyLevel} 선택 → 게임 시작 준비");

            if (!string.IsNullOrEmpty(pendingSong.AudioFileName))
            {
                StartCoroutine(LoadAudioAndStartGame(pendingSong));
            }
            else
            {
                var songData = CreateSongDataFromRecord(pendingSong, null);
                GameManager.Instance?.StartGame(songData);
            }
            pendingSong = null;
        }

        private IEnumerator LoadAudioAndStartGame(SongRecord song)
        {
            if (songCountText != null)
                songCountText.text = "로딩 중...";

            string audioFileName = song.AudioFileName;
            string url;

            if (audioFileName.StartsWith("music:"))
            {
                string realName = audioFileName.Substring(6);
                string fullPath = System.IO.Path.Combine(Application.persistentDataPath, "Music", realName);
                url = "file://" + fullPath;
                audioFileName = realName;
            }
            else if (audioFileName.StartsWith("ext:"))
            {
                string fullPath = audioFileName.Substring(4);
                url = "file://" + fullPath;
                audioFileName = System.IO.Path.GetFileName(fullPath);
            }
            else
            {
                string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, audioFileName);
#if UNITY_ANDROID && !UNITY_EDITOR
                url = fullPath;
#else
                url = "file://" + fullPath;
#endif
            }

            AudioType audioType = GetAudioType(audioFileName);
            Debug.Log($"[SongLibrary] 오디오 로드 시작: {url} (type: {audioType})");

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return www.SendWebRequest();
                Debug.Log($"[SongLibrary] 오디오 로드 결과: {www.result}");

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    Debug.Log($"[SongLibrary] 오디오 클립 로드 완료: {clip.name}, 길이: {clip.length}s");
                    var songData = CreateSongDataFromRecord(song, clip);
                    GameManager.Instance?.StartGame(songData);
                }
                else
                {
                    Debug.LogError($"[SongLibrary] 오디오 로드 실패: {www.error}");
                    string ext = System.IO.Path.GetExtension(audioFileName).ToLower();
                    if (ext == ".m4a" || ext == ".flac")
                    {
                        if (songCountText != null)
                            songCountText.text = "미지원 포맷 (MP3/WAV/OGG 권장)";
                    }
                    else
                    {
                        if (songCountText != null)
                            songCountText.text = "로드 실패!";
                    }
                    yield return new WaitForSeconds(2f);
                    RefreshSongList();
                }
            }
        }

        /// <summary>
        /// 난이도 선택 팝업 표시
        /// </summary>
        private void ShowDifficultyPopup()
        {
            CloseDifficultyPopup();

            // Canvas 최상위에 팝업 부착
            var parentCanvas = rootPanel.GetComponentInParent<Canvas>();
            Transform popupParent = parentCanvas != null ? parentCanvas.transform : rootPanel.transform.parent;

            difficultyPopup = new GameObject("DifficultyPopup");
            difficultyPopup.transform.SetParent(popupParent, false);
            var popupRect = difficultyPopup.AddComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;
            difficultyPopup.transform.SetAsLastSibling();

            // 별도 Canvas + 높은 sortingOrder로 모든 UI 위에 렌더링
            var popupCanvas = difficultyPopup.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 100;
            difficultyPopup.AddComponent<GraphicRaycaster>();

            // 반투명 배경 (터치로 닫기) — 거의 불투명
            var bgImage = difficultyPopup.AddComponent<Image>();
            bgImage.color = new Color(0.03f, 0.03f, 0.05f, 1f);
            var bgBtn = difficultyPopup.AddComponent<Button>();
            bgBtn.targetGraphic = bgImage;
            var bgColors = bgBtn.colors;
            bgColors.normalColor = Color.white;
            bgColors.highlightedColor = Color.white;
            bgColors.pressedColor = Color.white;
            bgBtn.colors = bgColors;
            bgBtn.onClick.AddListener(() => CloseDifficultyPopup());

            // 중앙 카드 — 컴팩트 (나노바나나 디자인 참조)
            var card = new GameObject("Card");
            card.transform.SetParent(difficultyPopup.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.06f, 0.20f);
            cardRect.anchorMax = new Vector2(0.94f, 0.80f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            var cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.08f, 0.08f, 0.12f, 1f);

            // 카드 테두리 (얇은 시안 네온)
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.3f, 0.4f, 0.5f, 0.12f);
            cardOutline.effectDistance = new Vector2(1, -1);

            // ===== 카드 내부 레이아웃 =====
            // [0.82~0.96] 곡 제목
            // [0.74~0.82] SELECT DIFFICULTY
            // [0.50~0.72] EASY
            // [0.27~0.49] NORMAL
            // [0.04~0.26] HARD
            // 하단 바깥에 CANCEL

            // 곡 제목
            var titleObj = new GameObject("SongTitle");
            titleObj.transform.SetParent(card.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.06f, 0.82f);
            titleRect.anchorMax = new Vector2(0.94f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = pendingSong?.Title ?? "곡 선택";
            titleText.fontSize = 36;
            titleText.fontSizeMin = 20; titleText.fontSizeMax = 36;
            titleText.enableAutoSizing = true;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;

            // "난이도를 선택하세요" 라벨
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(card.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.06f, 0.74f);
            labelRect.anchorMax = new Vector2(0.94f, 0.82f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "난이도를 선택하세요";
            labelText.fontSize = 22;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.45f, 0.45f, 0.50f);

            // 난이도 버튼 3개
            CreateDifficultyButton(card.transform, "쉬움",   "노트 적음 · 느린 간격",       3,  new Color(0.30f, 0.85f, 0.40f), 0.50f, 0.72f);
            CreateDifficultyButton(card.transform, "보통",   "빠른 간격 · 동시타격",        7,  new Color(1.0f, 0.80f, 0.15f),  0.27f, 0.49f);
            CreateDifficultyButton(card.transform, "어려움", "폭타 · 동시타격 다수",        10, new Color(1.0f, 0.25f, 0.25f),  0.04f, 0.26f);

            // CANCEL 버튼 — 카드 바깥 아래
            var closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(difficultyPopup.transform, false);
            var closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.25f, 0.12f);
            closeRect.anchorMax = new Vector2(0.75f, 0.18f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            var closeBtnImage = closeObj.AddComponent<Image>();
            closeBtnImage.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            var closeOutline = closeObj.AddComponent<Outline>();
            closeOutline.effectColor = new Color(0.35f, 0.35f, 0.40f, 0.5f);
            closeOutline.effectDistance = new Vector2(1, -1);
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImage;
            closeBtn.onClick.AddListener(() => CloseDifficultyPopup());

            var closeTxt = new GameObject("Text");
            closeTxt.transform.SetParent(closeObj.transform, false);
            var closeTxtRect = closeTxt.AddComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.offsetMin = Vector2.zero;
            closeTxtRect.offsetMax = Vector2.zero;
            var closeTmp = closeTxt.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "취소";
            closeTmp.fontSize = 26;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = new Color(0.7f, 0.7f, 0.75f);

            KoreanFontManager.ApplyFontToAll(difficultyPopup);
        }

        /// <summary>
        /// 난이도 버튼 생성 — 나노바나나 디자인
        /// </summary>
        private void CreateDifficultyButton(Transform parent, string label, string desc, int level, Color accentColor, float anchorYMin, float anchorYMax)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.06f, anchorYMin);
            btnRect.anchorMax = new Vector2(0.94f, anchorYMax);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            // 버튼 배경 (매우 짙은 색)
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.07f, 0.07f, 0.10f);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            var btnColors = btn.colors;
            btnColors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            btnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = btnColors;

            // 좌측 두꺼운 액센트 바 (6px)
            var accentBar = new GameObject("Accent");
            accentBar.transform.SetParent(btnObj.transform, false);
            var accentRect = accentBar.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(6f, 0f);
            var accentImage = accentBar.AddComponent<Image>();
            accentImage.color = accentColor;
            accentImage.raycastTarget = false;

            // 상단 라인 (난이도 색상)
            var topLine = new GameObject("TopLine");
            topLine.transform.SetParent(btnObj.transform, false);
            var topLineRect = topLine.AddComponent<RectTransform>();
            topLineRect.anchorMin = new Vector2(0f, 1f);
            topLineRect.anchorMax = new Vector2(1f, 1f);
            topLineRect.pivot = new Vector2(0.5f, 1f);
            topLineRect.anchoredPosition = Vector2.zero;
            topLineRect.sizeDelta = new Vector2(0f, 1.5f);
            var topLineImage = topLine.AddComponent<Image>();
            topLineImage.color = accentColor * new Color(1, 1, 1, 0.5f);
            topLineImage.raycastTarget = false;

            // 하단 라인 (난이도 색상)
            var bottomLine = new GameObject("BottomLine");
            bottomLine.transform.SetParent(btnObj.transform, false);
            var bottomLineRect = bottomLine.AddComponent<RectTransform>();
            bottomLineRect.anchorMin = new Vector2(0f, 0f);
            bottomLineRect.anchorMax = new Vector2(1f, 0f);
            bottomLineRect.pivot = new Vector2(0.5f, 0f);
            bottomLineRect.anchoredPosition = Vector2.zero;
            bottomLineRect.sizeDelta = new Vector2(0f, 1.5f);
            var bottomLineImage = bottomLine.AddComponent<Image>();
            bottomLineImage.color = accentColor * new Color(1, 1, 1, 0.5f);
            bottomLineImage.raycastTarget = false;

            // 난이도 이름 (좌측 상단, 크게)
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.40f);
            nameRect.anchorMax = new Vector2(0.70f, 0.95f);
            nameRect.offsetMin = new Vector2(24f, 0f);
            nameRect.offsetMax = Vector2.zero;
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = label;
            nameTmp.fontSize = 38;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = accentColor;
            nameTmp.raycastTarget = false;

            // 설명 (좌측 하단, 작게)
            var descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform, false);
            var descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0.05f);
            descRect.anchorMax = new Vector2(0.70f, 0.42f);
            descRect.offsetMin = new Vector2(24f, 0f);
            descRect.offsetMax = Vector2.zero;
            var descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = desc;
            descTmp.fontSize = 20;
            descTmp.alignment = TextAlignmentOptions.MidlineLeft;
            descTmp.color = new Color(0.50f, 0.50f, 0.55f);
            descTmp.raycastTarget = false;

            // 우측 레벨 표시
            var lvObj = new GameObject("Level");
            lvObj.transform.SetParent(btnObj.transform, false);
            var lvRect = lvObj.AddComponent<RectTransform>();
            lvRect.anchorMin = new Vector2(0.70f, 0f);
            lvRect.anchorMax = new Vector2(1f, 1f);
            lvRect.offsetMin = Vector2.zero;
            lvRect.offsetMax = new Vector2(-10f, 0f);
            var lvTmp = lvObj.AddComponent<TextMeshProUGUI>();
            lvTmp.text = $"Lv.{level}";
            lvTmp.fontSize = 30;
            lvTmp.fontStyle = FontStyles.Bold;
            lvTmp.alignment = TextAlignmentOptions.Center;
            lvTmp.color = new Color(0.55f, 0.55f, 0.60f);
            lvTmp.raycastTarget = false;

            int capturedLevel = level;
            btn.onClick.AddListener(() => StartGameWithDifficulty(capturedLevel));
        }

        /// <summary>
        /// 에디터 테스트용: 난이도 팝업 강제 표시
        /// </summary>
        public void ForceShowDifficultyPopup()
        {
            if (displayedSongs != null && displayedSongs.Count > 0)
                pendingSong = displayedSongs[0];
            ShowDifficultyPopup();
            Debug.Log("[SongLibrary] ForceShowDifficultyPopup() 완료");
        }

        /// <summary>
        /// 난이도 팝업 닫기
        /// </summary>
        private void CloseDifficultyPopup()
        {
            if (difficultyPopup != null)
            {
                Destroy(difficultyPopup);
                difficultyPopup = null;
            }
        }

        private SongData CreateSongDataFromRecord(SongRecord record, AudioClip clip)
        {
            var songData = ScriptableObject.CreateInstance<SongData>();
            songData.Id = $"local_{record.Title.GetHashCode():X8}";
            songData.Title = record.Title;
            songData.Artist = record.Artist ?? "Unknown";
            songData.BPM = record.BPM;
            songData.Duration = clip != null ? clip.length : record.Duration;
            songData.Genre = record.Genre ?? "";
            songData.Mood = record.Mood ?? "";
            songData.Difficulty = record.DifficultyLevel;
            songData.AudioClip = clip;
            return songData;
        }

        private IEnumerator AnimateCardsEntrance()
        {
            for (int i = 0; i < songItems.Count; i++)
            {
                var card = songItems[i];
                if (card == null) continue;

                var rectTransform = card.GetComponent<RectTransform>();
                var canvasGroup = card.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = card.AddComponent<CanvasGroup>();

                rectTransform.localScale = new Vector3(0.85f, 0.85f, 1f);
                canvasGroup.alpha = 0f;

                float delay = i * 0.04f;
                StartCoroutine(AnimateSingleCard(rectTransform, canvasGroup, delay));
            }
            yield return null;
        }

        private IEnumerator AnimateSingleCard(RectTransform rect, CanvasGroup cg, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (rect == null || cg == null) yield break;

            float duration = 0.2f;
            float elapsed = 0f;

            Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                if (rect == null || cg == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float overshoot = 1.3f;
                float eased = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);

                rect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                cg.alpha = Mathf.Clamp01(t * 2.5f);
                yield return null;
            }

            if (rect != null) rect.localScale = endScale;
            if (cg != null) cg.alpha = 1f;
        }

        private void UpdateEmptyState(bool isEmpty)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(isEmpty);
        }

        private Color GetRankColor(string rank)
        {
            if (string.IsNullOrEmpty(rank)) return Color.gray;

            return rank switch
            {
                "S+" => UIColorPalette.NEON_YELLOW,
                "S" => UIColorPalette.EQ_YELLOW,
                "A" => UIColorPalette.NEON_GREEN,
                "B" => UIColorPalette.NEON_CYAN,
                "C" => UIColorPalette.TEXT_WHITE,
                "D" => UIColorPalette.TEXT_DIM,
                _ => UIColorPalette.TEXT_DIM
            };
        }

        private TextMeshProUGUI CreateTMPText(GameObject parent, string name, string text,
            float fontSize, Color color, TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        public void Show(bool visible)
        {
            if (rootPanel != null)
                rootPanel.SetActive(visible);
        }

        public bool IsVisible => rootPanel != null && rootPanel.activeSelf;

        private static AudioType GetAudioType(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".mp3" => AudioType.MPEG,
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                ".m4a" => AudioType.UNKNOWN,
                ".flac" => AudioType.UNKNOWN,
                _ => AudioType.MPEG
            };
        }

        private void OnDestroy()
        {
            if (SongLibraryManager.Instance != null)
                SongLibraryManager.Instance.OnLibraryChanged -= RefreshSongList;

            foreach (var item in songItems)
            {
                if (item != null)
                {
                    var btn = item.GetComponent<Button>();
                    if (btn != null) btn.onClick.RemoveAllListeners();
                }
            }
        }
    }
}
