using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using AIBeat.Core;
using AIBeat.Data;

namespace AIBeat.UI
{
    /// <summary>
    /// 곡 라이브러리 UI - 로컬 MP3 곡 목록 표시
    /// ScrollRect 기반 곡 카드 목록 + 사이버펑크 네온 스타일
    /// </summary>
    public class SongLibraryUI : MonoBehaviour
    {
        // 색상 (UIColorPalette 기반)
        private static readonly Color NEON_CYAN = UIColorPalette.BORDER_CYAN;
        private static readonly Color NEON_CYAN_BRIGHT = UIColorPalette.NEON_CYAN_BRIGHT;
        private static readonly Color CARD_BG = UIColorPalette.BG_CARD;
        private static readonly Color DELETE_COLOR = UIColorPalette.ERROR_RED;

        // UI 참조
        private GameObject rootPanel;
        private Transform contentContainer;
        private ScrollRect scrollRect;
        private TextMeshProUGUI emptyText;
        private TextMeshProUGUI songCountText;

        // 곡 아이템 오브젝트 리스트
        private List<GameObject> songItems = new List<GameObject>();

        // 현재 표시 중인 곡 목록
        private List<SongRecord> displayedSongs = new List<SongRecord>();

        // 삭제 확인 상태
        private int deleteConfirmIndex = -1;

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
            rootRect.offsetMin = new Vector2(0, 0);
            rootRect.offsetMax = new Vector2(0, -100); // 타이틀 바(100px) 아래부터

            // 반투명 배경 (BIT.jpg 배경이 살짝 비침)
            var rootBg = rootPanel.AddComponent<Image>();
            rootBg.color = new Color(0.02f, 0.015f, 0.06f, 0.88f);

            // VerticalLayoutGroup 대신 앵커 기반 수동 배치
            // 1. 곡 수 표시 (상단 60px)
            CreateSongCountBar(rootPanel.transform);

            // 2. ScrollRect 곡 목록 (CountBar 아래 전체 영역)
            CreateScrollArea(rootPanel.transform);
        }

        /// <summary>
        /// 곡 수 표시 바
        /// </summary>
        private void CreateSongCountBar(Transform parent)
        {
            var countBar = new GameObject("CountBar");
            countBar.transform.SetParent(parent, false);
            var countRect = countBar.AddComponent<RectTransform>();
            // 상단에 고정, 높이 60px
            countRect.anchorMin = new Vector2(0, 1);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.pivot = new Vector2(0.5f, 1);
            countRect.anchoredPosition = new Vector2(0, -10); // 상단 padding 10px
            countRect.sizeDelta = new Vector2(-30, 60); // 좌우 padding 15px씩

            songCountText = CreateTMPText(countBar, "SongCount", "0곡", 36,
                new Color(0.6f, 0.6f, 0.7f), TextAlignmentOptions.MidlineRight);
        }

        /// <summary>
        /// ScrollRect 곡 목록 영역 생성
        /// </summary>
        private void CreateScrollArea(Transform parent)
        {
            // ScrollView 컨테이너 — CountBar(60px) + padding(10px) 아래부터 하단까지
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            scrollViewRect.offsetMin = new Vector2(15, 10);    // 좌, 하 padding
            scrollViewRect.offsetMax = new Vector2(-15, -78);  // 우 padding, 상단 = -(CountBar 60 + padding 10 + spacing 8)

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

            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.clear; // 투명 — 스크롤 터치 감지용
            viewport.AddComponent<RectMask2D>(); // Mask 대신 RectMask2D 사용 (stencil 문제 방지)

            scrollRect.viewport = viewportRect;

            // Content (스크롤되는 실제 내용)
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0); // width=부모 stretch, height=ContentSizeFitter가 관리

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8;
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
            emptyRect.sizeDelta = new Vector2(0, 200);
            var emptyLayout = emptyGo.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 200;

            emptyText = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyText.text = "아직 곡이 없습니다!\nMP3 파일을 Music 또는\nDownloads 폴더에 넣어주세요.";
            emptyText.fontSize = 52;
            emptyText.color = new Color(0.5f, 0.5f, 0.6f);
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

            var manager = SongLibraryManager.Instance;
            if (manager == null)
            {
                UpdateEmptyState(true);
                return;
            }

            List<SongRecord> songs = manager.GetSongsSortedByDate();
            displayedSongs = songs;

            // 곡 수 표시
            if (songCountText != null)
                songCountText.text = $"{songs.Count}곡";

            // 빈 목록 처리
            UpdateEmptyState(songs.Count == 0);

            // 곡 카드 생성
            for (int i = 0; i < songs.Count; i++)
            {
                CreateSongCard(songs[i], i);
            }

            // 한국어 폰트 적용 (동적 생성된 카드에도 적용)
            KoreanFontManager.ApplyFontToAll(rootPanel);

            // 레이아웃 강제 재계산 (동적 UI 생성 직후 필수)
            Canvas.ForceUpdateCanvases();
            if (contentContainer != null)
            {
                var contentRect = contentContainer.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            #if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Loaded {songs.Count} songs");
            #endif
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
            cardRect.sizeDelta = new Vector2(0, 160);
            var cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 160;

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
            hLayout.padding = new RectOffset(20, 15, 12, 12);
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
            infoVLayout.spacing = 4;
            infoVLayout.childControlWidth = true;
            infoVLayout.childControlHeight = true;
            infoVLayout.childForceExpandWidth = true;
            infoVLayout.childForceExpandHeight = false;
            infoVLayout.childAlignment = TextAnchor.MiddleLeft;

            // 곡 제목 (첫 글자 대문자 처리)
            string displayTitle = FormatTitle(song.Title);
            CreateTMPText(infoPanel, "Title", displayTitle, 40, Color.white,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            // BPM + 난이도 별
            string diffStars = new string('\u2605', Mathf.Clamp(song.DifficultyLevel, 0, 10));
            string bpmInfo = song.BPM > 0 ? $"{song.BPM} BPM  |  {diffStars}" : diffStars;
            CreateTMPText(infoPanel, "Info", bpmInfo, 28,
                NEON_CYAN_BRIGHT, TextAlignmentOptions.MidlineLeft);

            // 플레이 횟수
            CreateTMPText(infoPanel, "Plays",
                song.PlayCount > 0 ? $"{song.PlayCount}회 플레이" : "아직 플레이 안 함", 24,
                new Color(0.5f, 0.5f, 0.6f), TextAlignmentOptions.MidlineLeft);

            // 우측: 랭크 + 점수 + 삭제
            var scorePanel = new GameObject("ScorePanel");
            scorePanel.transform.SetParent(card.transform, false);
            scorePanel.AddComponent<RectTransform>();
            var scoreLayout = scorePanel.AddComponent<LayoutElement>();
            scoreLayout.preferredWidth = 150;

            var scoreVLayout = scorePanel.AddComponent<VerticalLayoutGroup>();
            scoreVLayout.spacing = 2;
            scoreVLayout.childControlWidth = true;
            scoreVLayout.childControlHeight = true;
            scoreVLayout.childForceExpandWidth = true;
            scoreVLayout.childForceExpandHeight = false;
            scoreVLayout.childAlignment = TextAnchor.MiddleCenter;

            // 랭크 표시
            string rankDisplay = string.IsNullOrEmpty(song.BestRank) ? "-" : song.BestRank;
            CreateTMPText(scorePanel, "Rank", rankDisplay, 48,
                GetRankColor(song.BestRank), TextAlignmentOptions.Center, FontStyles.Bold);

            // 최고 점수
            string scoreDisplay = song.BestScore > 0 ? song.BestScore.ToString("N0") : "--";
            CreateTMPText(scorePanel, "Score", scoreDisplay, 24,
                Color.white, TextAlignmentOptions.Center);

            // 삭제 버튼
            CreateDeleteButton(scorePanel.transform, capturedIndex);

            songItems.Add(card);
        }

        /// <summary>
        /// 파일명 기반 제목을 보기 좋게 포맷 (각 단어 첫 글자 대문자)
        /// </summary>
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

        /// <summary>
        /// 삭제 버튼 생성
        /// </summary>
        private void CreateDeleteButton(Transform parent, int index)
        {
            var delGo = new GameObject("DeleteBtn");
            delGo.transform.SetParent(parent, false);
            var delRect = delGo.AddComponent<RectTransform>();
            delRect.sizeDelta = new Vector2(0, 30);
            var delLayout = delGo.AddComponent<LayoutElement>();
            delLayout.preferredHeight = 30;

            var delBg = delGo.AddComponent<Image>();
            delBg.color = new Color(0.15f, 0.02f, 0.02f, 0.4f);

            var delBtn = delGo.AddComponent<Button>();
            var delColors = delBtn.colors;
            delColors.normalColor = Color.white;
            delColors.highlightedColor = new Color(1.3f, 1f, 1f);
            delColors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            delBtn.colors = delColors;

            CreateTMPText(delGo, "DelText", "삭제", 20,
                new Color(0.6f, 0.15f, 0.15f, 0.7f), TextAlignmentOptions.Center);

            int capturedIndex = index;
            delBtn.onClick.AddListener(() => OnDeleteClicked(capturedIndex));
        }

        /// <summary>
        /// 곡 카드 클릭 시 해당 곡으로 게임 시작
        /// StreamingAssets에서 MP3 로드 → SongData 생성 → GameManager.StartGame
        /// </summary>
        private void OnSongCardClicked(int index)
        {
            if (index < 0 || index >= displayedSongs.Count) return;

            var song = displayedSongs[index];
#if UNITY_EDITOR
            Debug.Log($"[SongLibrary] Song selected: {song.Title} (audio: {song.AudioFileName})");
#endif

            // AudioFileName이 있으면 StreamingAssets에서 MP3 로드 후 게임 시작
            if (!string.IsNullOrEmpty(song.AudioFileName))
            {
                StartCoroutine(LoadAudioAndStartGame(song));
            }
            else
            {
                // AudioFileName 없으면 디버그 모드로 시작 (BPM 기반 노트 생성)
                var songData = CreateSongDataFromRecord(song, null);
                GameManager.Instance?.StartGame(songData);
            }
        }

        /// <summary>
        /// 오디오 파일 로드 후 게임 시작
        /// AudioFileName이 "music:" 접두사면 persistentDataPath/Music 폴더에서 로드
        /// 그 외에는 StreamingAssets에서 로드
        /// </summary>
        private IEnumerator LoadAudioAndStartGame(SongRecord song)
        {
            // 로딩 표시
            if (songCountText != null)
                songCountText.text = "로딩 중...";

            string audioFileName = song.AudioFileName;
            string url;

            if (audioFileName.StartsWith("music:"))
            {
                // persistentDataPath/Music 폴더
                string realName = audioFileName.Substring(6); // "music:" 제거
                string fullPath = System.IO.Path.Combine(Application.persistentDataPath, "Music", realName);
                url = "file://" + fullPath;
                audioFileName = realName;
            }
            else if (audioFileName.StartsWith("ext:"))
            {
                // 외부 저장소 (전체 경로 저장됨)
                string fullPath = audioFileName.Substring(4); // "ext:" 제거
                url = "file://" + fullPath;
                audioFileName = System.IO.Path.GetFileName(fullPath);
            }
            else
            {
                // StreamingAssets (Android는 jar:file:// 형식, 그대로 사용)
                string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, audioFileName);
#if UNITY_ANDROID && !UNITY_EDITOR
                url = fullPath; // Android StreamingAssets는 이미 jar:file:// 포함
#else
                url = "file://" + fullPath;
#endif
            }

            AudioType audioType = AudioType.MPEG;
            if (audioFileName.EndsWith(".wav")) audioType = AudioType.WAV;
            else if (audioFileName.EndsWith(".ogg")) audioType = AudioType.OGGVORBIS;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    var songData = CreateSongDataFromRecord(song, clip);

#if UNITY_EDITOR
                    Debug.Log($"[SongLibrary] Audio loaded: {song.AudioFileName} ({clip.length:F1}s)");
#endif

                    GameManager.Instance?.StartGame(songData);
                }
                else
                {
                    Debug.LogError($"[SongLibrary] Audio load failed: {www.error} (URL: {url})");
                    if (songCountText != null)
                        songCountText.text = "로드 실패!";
                    yield return new WaitForSeconds(2f);
                    RefreshSongList();
                }
            }
        }

        /// <summary>
        /// SongRecord → SongData 변환 (AudioClip 포함)
        /// </summary>
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
            // Notes는 null → GameplayController가 SmartBeatMapper로 자동 생성
            return songData;
        }

        /// <summary>
        /// 삭제 버튼 클릭 처리 (2번 클릭 확인)
        /// </summary>
        private void OnDeleteClicked(int index)
        {
            if (index < 0 || index >= displayedSongs.Count) return;

            if (deleteConfirmIndex == index)
            {
                var song = displayedSongs[index];
                SongLibraryManager.Instance?.DeleteSong(song.Title);
                deleteConfirmIndex = -1;
                RefreshSongList();
            }
            else
            {
                deleteConfirmIndex = index;

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
                "S+" => UIColorPalette.NEON_YELLOW,
                "S" => UIColorPalette.EQ_YELLOW,
                "A" => UIColorPalette.NEON_GREEN,
                "B" => UIColorPalette.NEON_CYAN,
                "C" => UIColorPalette.TEXT_WHITE,
                "D" => UIColorPalette.TEXT_DIM,
                _ => UIColorPalette.TEXT_DIM
            };
        }

        // === 유틸리티 메서드 ===

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
