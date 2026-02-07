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
    /// 곡 라이브러리 UI - SongSelect 씬 내 "내 라이브러리" 탭
    /// ScrollRect 기반 곡 목록 + 필터/정렬 + 사이버펑크 네온 카드
    /// </summary>
    public class SongLibraryUI : MonoBehaviour
    {
        // 색상 상수 (사이버펑크 네온 스타일)
        private static readonly Color BG_COLOR = new Color(0.02f, 0.02f, 0.08f, 0.9f);
        private static readonly Color NEON_CYAN = new Color(0f, 0.8f, 1f, 0.5f);
        private static readonly Color NEON_CYAN_BRIGHT = new Color(0f, 0.9f, 1f, 0.8f);
        private static readonly Color CARD_BG = new Color(0.05f, 0.05f, 0.15f, 0.85f);
        private static readonly Color CARD_HOVER = new Color(0.08f, 0.08f, 0.2f, 0.9f);
        private static readonly Color BUTTON_NORMAL = new Color(0.1f, 0.1f, 0.25f, 0.8f);
        private static readonly Color BUTTON_SELECTED = new Color(0f, 0.4f, 0.6f, 0.9f);
        private static readonly Color DELETE_COLOR = new Color(0.8f, 0.1f, 0.1f, 0.8f);

        // UI 참조
        private GameObject rootPanel;
        private Transform contentContainer;
        private ScrollRect scrollRect;
        private TextMeshProUGUI emptyText;
        private TextMeshProUGUI songCountText;

        // 필터/정렬 버튼
        private List<Button> filterButtons = new List<Button>();
        private List<Button> sortButtons = new List<Button>();

        // 곡 아이템 오브젝트 리스트
        private List<GameObject> songItems = new List<GameObject>();

        // 현재 상태
        private string currentFilter = "All";
        private SortMode currentSort = SortMode.Date;
        private List<SongRecord> displayedSongs = new List<SongRecord>();

        // 삭제 확인 상태
        private int deleteConfirmIndex = -1;

        private enum SortMode { Date, Score, PlayCount }

        /// <summary>
        /// 라이브러리 UI 초기화 (SongSelectUI에서 호출)
        /// </summary>
        public void Initialize(RectTransform parentPanel)
        {
            if (rootPanel != null) return; // 이미 초기화됨

            CreateLibraryUI(parentPanel);

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(rootPanel);

            RefreshSongList();

            // 라이브러리 변경 이벤트 구독
            if (SongLibraryManager.Instance != null)
                SongLibraryManager.Instance.OnLibraryChanged += RefreshSongList;
        }

        /// <summary>
        /// 라이브러리 UI 전체 구조 생성
        /// </summary>
        private void CreateLibraryUI(RectTransform parent)
        {
            // 루트 패널
            rootPanel = new GameObject("LibraryPanel");
            rootPanel.transform.SetParent(parent, false);
            var rootRect = rootPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 세로 레이아웃
            var rootLayout = rootPanel.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(15, 15, 10, 10);
            rootLayout.spacing = 8;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            // 1. 필터 영역
            CreateFilterBar(rootPanel.transform);

            // 2. 정렬 영역
            CreateSortBar(rootPanel.transform);

            // 3. 곡 수 표시
            CreateSongCountBar(rootPanel.transform);

            // 4. ScrollRect 곡 목록
            CreateScrollArea(rootPanel.transform);
        }

        /// <summary>
        /// 장르 필터 바 생성
        /// </summary>
        private void CreateFilterBar(Transform parent)
        {
            var filterBar = new GameObject("FilterBar");
            filterBar.transform.SetParent(parent, false);
            var filterRect = filterBar.AddComponent<RectTransform>();
            filterRect.sizeDelta = new Vector2(0, 40);
            var filterLayout = filterBar.AddComponent<LayoutElement>();
            filterLayout.preferredHeight = 40;

            var hLayout = filterBar.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;
            hLayout.padding = new RectOffset(5, 5, 2, 2);

            // 필터 라벨
            CreateLabel(filterBar.transform, "필터:", 14, NEON_CYAN_BRIGHT, false, 60);

            // 필터 버튼들 (내부 키 + 표시명)
            string[] filters = { "All", "EDM", "House", "Cyberpunk", "Synthwave", "Dubstep" };
            foreach (string filter in filters)
            {
                string displayName = filter == "All" ? "전체" : PromptOptions.GetGenreDisplay(filter);
                var btn = CreateFilterButton(filterBar.transform, filter, displayName);
                filterButtons.Add(btn);
            }

            // 첫 번째(All) 선택
            if (filterButtons.Count > 0)
                HighlightButton(filterButtons[0], filterButtons);
        }

        /// <summary>
        /// 정렬 바 생성
        /// </summary>
        private void CreateSortBar(Transform parent)
        {
            var sortBar = new GameObject("SortBar");
            sortBar.transform.SetParent(parent, false);
            var sortRect = sortBar.AddComponent<RectTransform>();
            sortRect.sizeDelta = new Vector2(0, 35);
            var sortLayout = sortBar.AddComponent<LayoutElement>();
            sortLayout.preferredHeight = 35;

            var hLayout = sortBar.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.padding = new RectOffset(5, 5, 2, 2);

            // 정렬 라벨
            CreateLabel(sortBar.transform, "정렬:", 14, NEON_CYAN_BRIGHT, false, 55);

            // 정렬 버튼
            var dateBtn = CreateSortButton(sortBar.transform, "최신순", SortMode.Date);
            var scoreBtn = CreateSortButton(sortBar.transform, "점수순", SortMode.Score);
            var playBtn = CreateSortButton(sortBar.transform, "플레이순", SortMode.PlayCount);
            sortButtons.Add(dateBtn);
            sortButtons.Add(scoreBtn);
            sortButtons.Add(playBtn);

            // 첫 번째 선택
            if (sortButtons.Count > 0)
                HighlightButton(sortButtons[0], sortButtons);

            // 스페이서
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(sortBar.transform, false);
            spacer.AddComponent<RectTransform>();
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;
        }

        /// <summary>
        /// 곡 수 표시 바
        /// </summary>
        private void CreateSongCountBar(Transform parent)
        {
            var countBar = new GameObject("CountBar");
            countBar.transform.SetParent(parent, false);
            var countRect = countBar.AddComponent<RectTransform>();
            countRect.sizeDelta = new Vector2(0, 25);
            var countLayout = countBar.AddComponent<LayoutElement>();
            countLayout.preferredHeight = 25;

            songCountText = CreateTMPText(countBar, "SongCount", "0곡", 14,
                new Color(0.6f, 0.6f, 0.7f), TextAlignmentOptions.MidlineRight);
        }

        /// <summary>
        /// ScrollRect 곡 목록 영역 생성
        /// </summary>
        private void CreateScrollArea(Transform parent)
        {
            // ScrollView 컨테이너
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.sizeDelta = new Vector2(0, 0);
            var scrollViewLayout = scrollView.AddComponent<LayoutElement>();
            scrollViewLayout.flexibleHeight = 1;

            // ScrollRect 컴포넌트
            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;

            // Viewport (마스크 영역)
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            scrollRect.viewport = viewportRect;

            // Content (스크롤되는 실제 내용)
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, 0);

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            contentContainer = content.transform;
            scrollRect.content = contentRect;

            // 빈 목록 텍스트
            var emptyGo = new GameObject("EmptyText");
            emptyGo.transform.SetParent(content.transform, false);
            var emptyRect = emptyGo.AddComponent<RectTransform>();
            emptyRect.sizeDelta = new Vector2(0, 200);
            var emptyLayout = emptyGo.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 200;

            emptyText = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyText.text = "아직 곡이 없습니다!\nMP3 파일을 추가해주세요.";
            emptyText.fontSize = 22;
            emptyText.color = new Color(0.4f, 0.4f, 0.5f);
            emptyText.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// 곡 목록 새로고침
        /// </summary>
        public void RefreshSongList()
        {
            // 기존 아이템 제거
            foreach (var item in songItems)
            {
                if (item != null) Destroy(item);
            }
            songItems.Clear();
            deleteConfirmIndex = -1;

            // 필터링된 곡 목록 가져오기
            var manager = SongLibraryManager.Instance;
            if (manager == null)
            {
                UpdateEmptyState(true);
                return;
            }

            // 정렬 적용
            List<SongRecord> songs;
            switch (currentSort)
            {
                case SortMode.Score:
                    songs = manager.GetSongsSortedByScore();
                    break;
                case SortMode.PlayCount:
                    songs = manager.GetSongsSortedByPlayCount();
                    break;
                default:
                    songs = manager.GetSongsSortedByDate();
                    break;
            }

            // 필터 적용
            if (currentFilter != "All")
            {
                songs = songs.FindAll(s =>
                    s.Genre.Equals(currentFilter, System.StringComparison.OrdinalIgnoreCase));
            }

            displayedSongs = songs;

            // 곡 수 표시
            if (songCountText != null)
                songCountText.text = $"{songs.Count} / {manager.SongCount}곡";

            // 빈 목록 처리
            UpdateEmptyState(songs.Count == 0);

            // 곡 카드 생성
            for (int i = 0; i < songs.Count; i++)
            {
                CreateSongCard(songs[i], i);
            }

            // 한국어 폰트 적용 (동적 생성된 카드에도 적용)
            KoreanFontManager.ApplyFontToAll(rootPanel);
        }

        /// <summary>
        /// 개별 곡 카드 생성 (네온 사이버펑크 스타일)
        /// </summary>
        private void CreateSongCard(SongRecord song, int index)
        {
            // 카드 루트
            var card = new GameObject($"SongCard_{index}");
            card.transform.SetParent(contentContainer, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0, 100);
            var cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 100;

            // 카드 배경
            var cardBg = card.AddComponent<Image>();
            cardBg.color = CARD_BG;

            // 네온 테두리
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = NEON_CYAN;
            cardOutline.effectDistance = new Vector2(1, -1);

            // 카드 버튼 (클릭 시 곡 선택)
            var cardButton = card.AddComponent<Button>();
            var cardColors = cardButton.colors;
            cardColors.normalColor = Color.white;
            cardColors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            cardColors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            cardButton.colors = cardColors;
            int capturedIndex = index;
            cardButton.onClick.AddListener(() => OnSongCardClicked(capturedIndex));

            // 카드 내부 레이아웃 (좌: 곡 정보, 우: 랭크/점수)
            var hLayout = card.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(15, 15, 10, 10);
            hLayout.spacing = 10;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // 좌측: 곡 정보
            var infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(card.transform, false);
            infoPanel.AddComponent<RectTransform>();
            var infoLayout = infoPanel.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;

            var infoVLayout = infoPanel.AddComponent<VerticalLayoutGroup>();
            infoVLayout.spacing = 2;
            infoVLayout.childControlWidth = true;
            infoVLayout.childControlHeight = true;
            infoVLayout.childForceExpandWidth = true;
            infoVLayout.childForceExpandHeight = false;

            // 곡 제목
            CreateTMPText(infoPanel, "Title", song.Title, 20, Color.white,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            // 아티스트 + 장르
            CreateTMPText(infoPanel, "Artist", $"{song.Artist}  |  {song.Genre}", 14,
                new Color(0.6f, 0.6f, 0.7f), TextAlignmentOptions.MidlineLeft);

            // BPM + 난이도
            string diffStars = new string('\u2605', Mathf.Clamp(song.DifficultyLevel, 0, 10));
            CreateTMPText(infoPanel, "BPM", $"{song.BPM} BPM  |  {diffStars}", 13,
                NEON_CYAN_BRIGHT, TextAlignmentOptions.MidlineLeft);

            // 플레이 횟수
            CreateTMPText(infoPanel, "Plays",
                song.PlayCount > 0 ? $"{song.PlayCount}회 플레이" : "아직 플레이 안 함", 12,
                new Color(0.5f, 0.5f, 0.6f), TextAlignmentOptions.MidlineLeft);

            // 우측: 랭크 + 점수 + 삭제
            var scorePanel = new GameObject("ScorePanel");
            scorePanel.transform.SetParent(card.transform, false);
            scorePanel.AddComponent<RectTransform>();
            var scoreLayout = scorePanel.AddComponent<LayoutElement>();
            scoreLayout.preferredWidth = 100;

            var scoreVLayout = scorePanel.AddComponent<VerticalLayoutGroup>();
            scoreVLayout.spacing = 3;
            scoreVLayout.childControlWidth = true;
            scoreVLayout.childControlHeight = true;
            scoreVLayout.childForceExpandWidth = true;
            scoreVLayout.childForceExpandHeight = false;
            scoreVLayout.childAlignment = TextAnchor.MiddleCenter;

            // 랭크 표시
            string rankDisplay = string.IsNullOrEmpty(song.BestRank) ? "-" : song.BestRank;
            var rankText = CreateTMPText(scorePanel, "Rank", rankDisplay, 32,
                GetRankColor(song.BestRank), TextAlignmentOptions.Center, FontStyles.Bold);

            // 최고 점수
            string scoreDisplay = song.BestScore > 0 ? song.BestScore.ToString("N0") : "--";
            CreateTMPText(scorePanel, "Score", scoreDisplay, 14,
                Color.white, TextAlignmentOptions.Center);

            // 최고 콤보
            if (song.BestCombo > 0)
            {
                CreateTMPText(scorePanel, "Combo", $"{song.BestCombo}x", 12,
                    new Color(0.7f, 0.7f, 0.8f), TextAlignmentOptions.Center);
            }

            // 삭제 버튼
            CreateDeleteButton(scorePanel.transform, capturedIndex);

            songItems.Add(card);
        }

        /// <summary>
        /// 삭제 버튼 생성
        /// </summary>
        private void CreateDeleteButton(Transform parent, int index)
        {
            var delGo = new GameObject("DeleteBtn");
            delGo.transform.SetParent(parent, false);
            var delRect = delGo.AddComponent<RectTransform>();
            delRect.sizeDelta = new Vector2(0, 22);
            var delLayout = delGo.AddComponent<LayoutElement>();
            delLayout.preferredHeight = 22;

            var delBg = delGo.AddComponent<Image>();
            delBg.color = new Color(0.3f, 0.05f, 0.05f, 0.6f);

            var delBtn = delGo.AddComponent<Button>();
            var delColors = delBtn.colors;
            delColors.normalColor = Color.white;
            delColors.highlightedColor = new Color(1.3f, 1f, 1f);
            delColors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            delBtn.colors = delColors;

            var delText = CreateTMPText(delGo, "DelText", "삭제", 11,
                DELETE_COLOR, TextAlignmentOptions.Center, FontStyles.Bold);

            int capturedIndex = index;
            delBtn.onClick.AddListener(() => OnDeleteClicked(capturedIndex));
        }

        /// <summary>
        /// 곡 카드 클릭 시 해당 곡으로 게임 시작
        /// </summary>
        private void OnSongCardClicked(int index)
        {
            if (index < 0 || index >= displayedSongs.Count) return;

            var song = displayedSongs[index];
#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Song selected: {song.Title}");
#endif

            // SongData 생성 (FakeSongGenerator 활용)
            var generator = FindFirstObjectByType<FakeSongGenerator>();
            if (generator == null)
            {
                var go = new GameObject("FakeSongGenerator");
                generator = go.AddComponent<FakeSongGenerator>();
            }

            // 해당 곡과 매칭되는 데모 곡으로 SongData 생성
            var options = new PromptOptions
            {
                Genre = song.Genre,
                BPM = song.BPM,
                Mood = song.Mood,
                Duration = (int)song.Duration,
                Structure = "intro-build-drop-outro"
            };

            // 씨드 기반 재생성을 위해 시드 설정
            if (song.Seed > 0)
                UnityEngine.Random.InitState(song.Seed);

            // 게임 시작
            generator.GenerateSong(options);
        }

        /// <summary>
        /// 삭제 버튼 클릭 처리 (2번 클릭 확인)
        /// </summary>
        private void OnDeleteClicked(int index)
        {
            if (index < 0 || index >= displayedSongs.Count) return;

            if (deleteConfirmIndex == index)
            {
                // 두 번째 클릭 → 실제 삭제
                var song = displayedSongs[index];
                SongLibraryManager.Instance?.DeleteSong(song.Title);
                deleteConfirmIndex = -1;
                RefreshSongList();
            }
            else
            {
                // 첫 번째 클릭 → 확인 상태로 전환
                deleteConfirmIndex = index;

                // 삭제 버튼 텍스트 변경
                if (index < songItems.Count)
                {
                    var delText = songItems[index]?.transform
                        .Find("ScorePanel/DeleteBtn/DelText")
                        ?.GetComponent<TextMeshProUGUI>();
                    if (delText != null)
                    {
                        delText.text = "확인?";
                        delText.color = Color.red;
                    }
                }
            }
        }

        /// <summary>
        /// 필터 버튼 생성
        /// </summary>
        private Button CreateFilterButton(Transform parent, string filterKey, string displayName)
        {
            var go = new GameObject($"Filter_{filterKey}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var bg = go.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            btn.colors = colors;

            var tmpText = CreateTMPText(go, "Text", displayName, 12, Color.white, TextAlignmentOptions.Center);

            string capturedKey = filterKey;
            btn.onClick.AddListener(() =>
            {
                currentFilter = capturedKey;
                HighlightButton(btn, filterButtons);
                RefreshSongList();
            });

            return btn;
        }

        /// <summary>
        /// 정렬 버튼 생성
        /// </summary>
        private Button CreateSortButton(Transform parent, string text, SortMode mode)
        {
            var go = new GameObject($"Sort_{text}");
            go.transform.SetParent(parent, false);
            var goRect = go.AddComponent<RectTransform>();
            var goLayout = go.AddComponent<LayoutElement>();
            goLayout.preferredWidth = 80;

            var bg = go.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            btn.colors = colors;

            CreateTMPText(go, "Text", text, 13, Color.white, TextAlignmentOptions.Center);

            SortMode capturedMode = mode;
            btn.onClick.AddListener(() =>
            {
                currentSort = capturedMode;
                HighlightButton(btn, sortButtons);
                RefreshSongList();
            });

            return btn;
        }

        /// <summary>
        /// 버튼 하이라이트 (선택된 버튼의 배경색 변경)
        /// </summary>
        private void HighlightButton(Button selected, List<Button> allButtons)
        {
            foreach (var btn in allButtons)
            {
                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = (btn == selected) ? BUTTON_SELECTED : BUTTON_NORMAL;
            }
        }

        /// <summary>
        /// 빈 목록 상태 표시/숨기기
        /// </summary>
        private void UpdateEmptyState(bool isEmpty)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(isEmpty);
        }

        /// <summary>
        /// 랭크별 색상 반환
        /// </summary>
        private Color GetRankColor(string rank)
        {
            if (string.IsNullOrEmpty(rank)) return Color.gray;

            return rank switch
            {
                "S+" => new Color(1f, 0.85f, 0f),   // Gold
                "S" => Color.yellow,
                "A" => Color.green,
                "B" => Color.cyan,
                "C" => Color.white,
                "D" => Color.gray,
                _ => Color.gray
            };
        }

        // === 유틸리티 메서드 ===

        /// <summary>
        /// TMP 텍스트 생성 헬퍼
        /// </summary>
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

        /// <summary>
        /// 라벨 텍스트 생성 (고정 너비)
        /// </summary>
        private void CreateLabel(Transform parent, string text, float fontSize, Color color,
            bool flexWidth = false, float preferredWidth = 0)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            if (preferredWidth > 0 || !flexWidth)
            {
                var layout = go.AddComponent<LayoutElement>();
                if (preferredWidth > 0) layout.preferredWidth = preferredWidth;
                if (!flexWidth) layout.flexibleWidth = 0;
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontStyle = FontStyles.Bold;
        }

        /// <summary>
        /// UI 표시/숨기기
        /// </summary>
        public void Show(bool visible)
        {
            if (rootPanel != null)
                rootPanel.SetActive(visible);
        }

        public bool IsVisible => rootPanel != null && rootPanel.activeSelf;

        private void OnDestroy()
        {
            if (SongLibraryManager.Instance != null)
                SongLibraryManager.Instance.OnLibraryChanged -= RefreshSongList;

            foreach (var btn in filterButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();

            foreach (var btn in sortButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();

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
