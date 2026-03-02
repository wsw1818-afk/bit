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
            viewportImg.raycastTarget = false;
            // RectMask2D 제거 - 텍스트 클리핑 문제 방지 (ScrollRect는 Mask 없이도 동작)
            // viewport.AddComponent<RectMask2D>();

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

            // 카드 layout 계산 + TMP mesh 생성 (alpha=0 세팅 전에 먼저 실행)
            Canvas.ForceUpdateCanvases();
            if (contentContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    contentContainer.GetComponent<RectTransform>());

            foreach (var tmp in rootPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.ForceMeshUpdate(true, true);

            StartCoroutine(AnimateCardsEntrance());
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

            // 곡의 커버 이미지가 있으면 비동기 로드
            if (!string.IsNullOrEmpty(song.CoverImagePath))
                StartCoroutine(LoadCoverImage(artImg, song.CoverImagePath));

            // 좌측 정보 패널
            var infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(card.transform, false);
            infoPanel.AddComponent<RectTransform>();
            var infoLayout = infoPanel.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;

            var infoVLayout = infoPanel.AddComponent<VerticalLayoutGroup>();
            infoVLayout.spacing = 2;
            infoVLayout.padding = new RectOffset(0, 0, 4, 4);
            infoVLayout.childControlWidth = true;
            infoVLayout.childControlHeight = false;  // 자식 높이 직접 제어 (TMP size 충돌 방지)
            infoVLayout.childForceExpandWidth = true;
            infoVLayout.childForceExpandHeight = false;
            infoVLayout.childAlignment = TextAnchor.UpperLeft;

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
            CreateTMPText(infoPanel, "Info", row2, 28,
                NEON_CYAN_BRIGHT, TextAlignmentOptions.MidlineLeft);

            // Row 3: 랭크 + 점수
            string rankDisplay = string.IsNullOrEmpty(song.BestRank) ? "-" : song.BestRank;
            string scoreDisplay = song.BestScore > 0 ? song.BestScore.ToString("N0") : "--";
            var rankTmp = CreateTMPText(infoPanel, "RankScore",
                $"Best: <color=#{ColorUtility.ToHtmlStringRGB(GetRankColor(song.BestRank))}>{rankDisplay}</color> · {scoreDisplay}",
                26, new Color(0.75f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft);
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

            // 중앙 카드 — 콤팩트한 패널
            var card = new GameObject("Card");
            card.transform.SetParent(difficultyPopup.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.09f, 0.15f);
            cardRect.anchorMax = new Vector2(0.91f, 0.83f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            var cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.075f, 0.075f, 0.118f, 1f); // #13131e

            // 카드 테두리 (borderRadius 11px 효과 — 다중 Outline)
            var cardOutline1 = card.AddComponent<Outline>();
            cardOutline1.effectColor = new Color(0.20f, 0.25f, 0.35f, 0.35f);
            cardOutline1.effectDistance = new Vector2(2f, -2f);
            var cardOutline2 = card.AddComponent<Outline>();
            cardOutline2.effectColor = new Color(0.20f, 0.25f, 0.35f, 0.20f);
            cardOutline2.effectDistance = new Vector2(-2f, 2f);
            var cardOutline3 = card.AddComponent<Outline>();
            cardOutline3.effectColor = new Color(0.15f, 0.20f, 0.30f, 0.15f);
            cardOutline3.effectDistance = new Vector2(1f, -1f);

            // ===== 카드 내부 레이아웃 =====
            // 상단: 곡제목 (0.88~0.97) + "SELECT DIFFICULTY" 라벨 (0.82~0.88)
            // 중간: 버튼 3개 — 각각 독립 박스 (간격 포함)
            // 하단: CANCEL 버튼 (0.02~0.12)

            // 곡 제목
            var titleObj = new GameObject("SongTitle");
            titleObj.transform.SetParent(card.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.86f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);
            titleRect.offsetMin = new Vector2(16f, 0f);
            titleRect.offsetMax = new Vector2(-16f, 0f);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = pendingSong?.Title ?? "곡 선택";
            titleText.fontSize = 28;
            titleText.fontSizeMin = 16; titleText.fontSizeMax = 28;
            titleText.enableAutoSizing = true;
            titleText.characterSpacing = -4f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;

            // "SELECT DIFFICULTY" 라벨
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(card.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.79f);
            labelRect.anchorMax = new Vector2(1f, 0.86f);
            labelRect.offsetMin = new Vector2(16f, 0f);
            labelRect.offsetMax = new Vector2(-16f, 0f);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "난이도를 선택하세요";
            labelText.fontSize = 20;
            labelText.characterSpacing = -6f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.45f, 0.45f, 0.52f);

            // 난이도 버튼 3개 — 독립 박스 (디자인 원본)
            // 레이아웃: 취소(0.03~0.11) → 어려움(0.14~0.29) → 보통(0.32~0.47) → 쉬움(0.50~0.65)
            // 버튼 높이 0.15, 간격 0.03, 라벨 끝(0.72)과 쉬움 시작(0.65) → 간격 0.07
            var easyColor  = new Color(0.298f, 0.851f, 0.400f); // #4cd966
            var normalColor = new Color(1.0f, 0.800f, 0.145f); // #ffcc25
            var hardColor  = new Color(1.0f, 0.251f, 0.251f); // #ff4040

            CreateDifficultyButton(card.transform, "쉬움",   "노트 적음 · 느린 간격",  3,  easyColor,   0.56f, 0.71f);
            CreateDifficultyButton(card.transform, "보통",   "빠른 간격 · 동시타격",   7,  normalColor, 0.37f, 0.52f);
            CreateDifficultyButton(card.transform, "어려움", "폭타 · 동시타격 다수",   10, hardColor,   0.18f, 0.33f);

            // 취소 버튼 — 카드 내부 최하단
            var closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(card.transform, false);
            var closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.12f, 0.03f);
            closeRect.anchorMax = new Vector2(0.88f, 0.14f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            var closeBtnImage = closeObj.AddComponent<Image>();
            closeBtnImage.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            var closeOutline = closeObj.AddComponent<Outline>();
            closeOutline.effectColor = new Color(0.30f, 0.30f, 0.38f, 0.5f);
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
            closeTmp.characterSpacing = -6f;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = new Color(0.65f, 0.65f, 0.70f);

            KoreanFontManager.ApplyFontToAll(difficultyPopup);
        }

        /// <summary>
        /// 난이도 버튼 생성 — 나노바나나 디자인
        /// </summary>
        // anchorYMin/anchorYMax: 카드 내 버튼의 세로 비율 위치 (0=하단, 1=상단)
        private void CreateDifficultyButton(Transform parent, string label, string desc, int level, Color accentColor, float anchorYMin, float anchorYMax)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0f, anchorYMin);
            btnRect.anchorMax = new Vector2(1f, anchorYMax);
            btnRect.offsetMin = new Vector2(14f, 0f);
            btnRect.offsetMax = new Vector2(-14f, 0f);

            // 버튼 배경 — 디자인 원본처럼 독립 박스 + 테두리
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.10f, 0.10f, 0.14f);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            var btnColors = btn.colors;
            btnColors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            btnColors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = btnColors;

            // 버튼 테두리 — 불투명도 36%, 둥글기 4px 효과
            var btnOutline1 = btnObj.AddComponent<Outline>();
            btnOutline1.effectColor = accentColor * new Color(1, 1, 1, 0.36f);
            btnOutline1.effectDistance = new Vector2(1, -1);
            var btnOutline2 = btnObj.AddComponent<Outline>();
            btnOutline2.effectColor = accentColor * new Color(1, 1, 1, 0.20f);
            btnOutline2.effectDistance = new Vector2(-1, 1);

            // 좌측 두꺼운 액센트 바 (5px)
            var accentBar = new GameObject("Accent");
            accentBar.transform.SetParent(btnObj.transform, false);
            var accentRect = accentBar.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(8f, 0f);
            var accentImage = accentBar.AddComponent<Image>();
            accentImage.color = accentColor;
            accentImage.raycastTarget = false;

            // 난이도 이름 (좌측 상단)
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.42f);
            nameRect.anchorMax = new Vector2(0.68f, 0.95f);
            nameRect.offsetMin = new Vector2(24f, 0f);
            nameRect.offsetMax = Vector2.zero;
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = label;
            nameTmp.fontSize = 32;
            nameTmp.characterSpacing = -6f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = accentColor;
            nameTmp.raycastTarget = false;

            // 설명 (좌측 하단)
            var descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform, false);
            var descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0.05f);
            descRect.anchorMax = new Vector2(0.68f, 0.44f);
            descRect.offsetMin = new Vector2(24f, 0f);
            descRect.offsetMax = Vector2.zero;
            var descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = desc;
            descTmp.fontSize = 18;
            descTmp.characterSpacing = -6f;
            descTmp.alignment = TextAlignmentOptions.MidlineLeft;
            descTmp.color = new Color(0.55f, 0.55f, 0.60f);
            descTmp.raycastTarget = false;

            // 우측 레벨 표시
            var lvObj = new GameObject("Level");
            lvObj.transform.SetParent(btnObj.transform, false);
            var lvRect = lvObj.AddComponent<RectTransform>();
            lvRect.anchorMin = new Vector2(0.68f, 0f);
            lvRect.anchorMax = new Vector2(1f, 1f);
            lvRect.offsetMin = Vector2.zero;
            lvRect.offsetMax = new Vector2(-10f, 0f);
            var lvTmp = lvObj.AddComponent<TextMeshProUGUI>();
            lvTmp.text = $"Lv.{level}";
            lvTmp.fontSize = 28;
            lvTmp.characterSpacing = -4f;
            lvTmp.fontStyle = FontStyles.Bold;
            lvTmp.alignment = TextAlignmentOptions.Center;
            lvTmp.color = accentColor;
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
        /// 에디터 테스트용: 팝업 없이 바로 난이도 선택 후 게임 시작
        /// </summary>
        public void ForceStartWithDifficulty(int difficultyLevel)
        {
            if (displayedSongs != null && displayedSongs.Count > 0)
                pendingSong = displayedSongs[0];
            if (pendingSong == null)
            {
                Debug.LogError("[SongLibrary] ForceStartWithDifficulty: 곡이 없습니다");
                return;
            }
            Debug.Log($"[SongLibrary] ForceStartWithDifficulty({difficultyLevel}) 호출 - 곡: {pendingSong.Title}");
            StartGameWithDifficulty(difficultyLevel);
        }

        /// <summary>
        /// 에디터 테스트용: StreamingAssets의 특정 파일명으로 바로 게임 시작
        /// </summary>
        public void ForceStartWithFile(string audioFileName, int difficultyLevel)
        {
            Application.runInBackground = true;
            var record = new SongRecord
            {
                Title = System.IO.Path.GetFileNameWithoutExtension(audioFileName),
                Artist = "Test",
                AudioFileName = audioFileName,
                DifficultyLevel = difficultyLevel,
            };
            Debug.Log($"[SongLibrary] ForceStartWithFile({audioFileName}, difficulty={difficultyLevel})");
            StartCoroutine(LoadAudioAndStartGame(record));
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
            var rt = go.AddComponent<RectTransform>();
            // 고정 높이로 RectTransform 설정 - LayoutElement 없이 직접 크기 지정
            rt.sizeDelta = new Vector2(0, fontSize * 1.5f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            var korFont = KoreanFontManager.KoreanFont;
            if (korFont != null) tmp.font = korFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;

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

        /// <summary>
        /// 커버 이미지를 비동기 로드하여 앨범 아트 Image에 적용.
        /// "id3:<path>"   → MP3 내부 ID3 태그에서 추출 (파일 경로 직접 접근)
        /// "id3ms:<id>"   → Android MediaStore URI로 스트림 열어 ID3 추출 (API 33+ _data null 대응)
        /// 그 외           → 파일 시스템 이미지를 UnityWebRequest로 로드.
        /// 로드 실패 시 기존 기본 이미지 유지.
        /// </summary>
        private IEnumerator LoadCoverImage(Image targetImage, string imagePath)
        {
            if (targetImage == null || string.IsNullOrEmpty(imagePath)) yield break;

            // ID3 임베드 앨범 아트 (파일 경로로 직접 접근)
            if (imagePath.StartsWith("id3:"))
            {
                string mp3Path = imagePath.Substring(4);
                var tex = AIBeat.Core.Mp3CoverExtractor.ExtractCover(mp3Path);
                if (tex != null && targetImage != null)
                {
                    var sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
                    targetImage.sprite = sprite;
                    targetImage.preserveAspect = true;
                }
                yield break;
            }

            // ID3 임베드 앨범 아트 (Android MediaStore URI로 스트림 접근, API 33+ 대응)
            if (imagePath.StartsWith("id3ms:"))
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                string idStr = imagePath.Substring(6);
                if (long.TryParse(idStr, out long mediaStoreId))
                {
                    Texture2D tex = null;
                    try
                    {
                        tex = ExtractCoverFromMediaStore(mediaStoreId);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[SongLibrary] MediaStore 커버 추출 실패: id={mediaStoreId} - {e.Message}");
                    }
                    if (tex != null && targetImage != null)
                    {
                        var sprite = Sprite.Create(tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f));
                        targetImage.sprite = sprite;
                        targetImage.preserveAspect = true;
                    }
                }
#endif
                yield break;
            }

            // 파일 시스템 이미지 (jpg/png 등)
            string url = "file://" + imagePath.Replace("\\", "/");
            using (var www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success && targetImage != null)
                {
                    var tex = DownloadHandlerTexture.GetContent(www);
                    if (tex != null)
                    {
                        var sprite = Sprite.Create(tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f));
                        targetImage.sprite = sprite;
                        targetImage.preserveAspect = true;
                    }
                }
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Android MediaStore URI에서 앨범 아트 추출.
        /// 1차: Android 10+ loadThumbnail API (가장 빠르고 안정적)
        /// 2차: InputStream으로 ID3 파싱 (폴백)
        /// </summary>
        private static Texture2D ExtractCoverFromMediaStore(long mediaStoreId)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (var mediaStoreClass = new AndroidJavaClass("android.provider.MediaStore$Audio$Media"))
            {
                var baseUri = mediaStoreClass.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
                using (var contentUrisClass = new AndroidJavaClass("android.content.ContentUris"))
                using (var itemUri = contentUrisClass.CallStatic<AndroidJavaObject>(
                    "withAppendedId", baseUri, mediaStoreId))
                {
                    // 1차 시도: Android 10+ loadThumbnail API
                    int apiLevel = GetApiLevel();
                    if (apiLevel >= 29)
                    {
                        try
                        {
                            Texture2D thumbTex = LoadThumbnailFromContentResolver(contentResolver, itemUri);
                            if (thumbTex != null) return thumbTex;
                        }
                        catch (System.Exception) { }
                    }

                    // 2차 시도: InputStream으로 ID3 파싱
                    AndroidJavaObject inputStream = null;
                    try
                    {
                        inputStream = contentResolver.Call<AndroidJavaObject>("openInputStream", itemUri);
                    }
                    catch (System.Exception) { }
                    if (inputStream == null) return null;

                    try
                    {
                        byte[] buffer = ReadJavaInputStream(inputStream);
                        if (buffer == null || buffer.Length < 10) return null;

                        using (var ms = new System.IO.MemoryStream(buffer))
                        {
                            return AIBeat.Core.Mp3CoverExtractor.ExtractCoverFromStream(ms,
                                $"id={mediaStoreId}");
                        }
                    }
                    finally
                    {
                        try { inputStream.Call("close"); } catch { }
                        inputStream.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Android 10+ ContentResolver.loadThumbnail() API로 앨범 아트 로드.
        /// OS가 자체적으로 캐싱하므로 가장 빠르고 안정적.
        /// </summary>
        private static Texture2D LoadThumbnailFromContentResolver(
            AndroidJavaObject contentResolver, AndroidJavaObject uri)
        {
            {
                var size = new AndroidJavaObject("android.util.Size", 512, 512);
                // Bitmap bitmap = contentResolver.loadThumbnail(uri, size, null)
                var bitmap = contentResolver.Call<AndroidJavaObject>(
                    "loadThumbnail", uri, size, (AndroidJavaObject)null);
                if (bitmap == null) return null;

                try
                {
                    int w = bitmap.Call<int>("getWidth");
                    int h = bitmap.Call<int>("getHeight");
                    if (w <= 0 || h <= 0) return null;

                    // Bitmap → ARGB byte[] → Texture2D
                    int pixelCount = w * h;
                    int[] pixels = new int[pixelCount];
                    bitmap.Call("getPixels", pixels, 0, w, 0, 0, w, h);

                    // Android ARGB → Unity RGBA32 (Y축 반전: Android top-down → Unity bottom-up)
                    byte[] rgba = new byte[pixelCount * 4];
                    for (int y = 0; y < h; y++)
                    {
                        int srcRow = y * w;
                        int dstRow = (h - 1 - y) * w;
                        for (int x = 0; x < w; x++)
                        {
                            int argb = pixels[srcRow + x];
                            int di = (dstRow + x) * 4;
                            rgba[di + 0] = (byte)((argb >> 16) & 0xFF); // R
                            rgba[di + 1] = (byte)((argb >> 8) & 0xFF);  // G
                            rgba[di + 2] = (byte)(argb & 0xFF);          // B
                            rgba[di + 3] = (byte)((argb >> 24) & 0xFF); // A
                        }
                    }

                    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.LoadRawTextureData(rgba);
                    tex.Apply();
                    return tex;
                }
                finally
                {
                    bitmap.Call("recycle");
                    bitmap.Dispose();
                }
            }
        }

        private static int GetApiLevel()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }

        /// <summary>
        /// Java InputStream의 첫 512KB를 읽어 byte[]로 반환 (ID3 태그는 파일 앞 부분에 있음).
        /// 큰 앨범 아트(300KB+)가 임베드된 MP3도 처리 가능.
        /// </summary>
        private static byte[] ReadJavaInputStream(AndroidJavaObject inputStream, int maxBytes = 512 * 1024)
        {
            using (var ms = new System.IO.MemoryStream(maxBytes))
            {
                byte[] chunk = new byte[16384];
                int totalRead = 0;
                while (totalRead < maxBytes)
                {
                    int read = inputStream.Call<int>("read", chunk);
                    if (read <= 0) break;
                    ms.Write(chunk, 0, read);
                    totalRead += read;
                }
                return ms.ToArray();
            }
        }
#endif

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
