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

        // 삭제 확인 상태
        private int deleteConfirmIndex = -1;

        public void Initialize(RectTransform parentPanel)
        {
            if (rootPanel != null) return;

            CreateLibraryUI(parentPanel);
            KoreanFontManager.ApplyFontToAll(rootPanel);
            RefreshSongList();

            if (SongLibraryManager.Instance != null)
                SongLibraryManager.Instance.OnLibraryChanged += RefreshSongList;
        }

        private void CreateLibraryUI(RectTransform parent)
        {
            // 루트 패널 (타이틀바 90px 아래 ~ 이퀄라이저 120px 위)
            rootPanel = new GameObject("LibraryPanel");
            rootPanel.transform.SetParent(parent, false);
            var rootRect = rootPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(0, 120);  // 이퀄라이저 바(120px) 위부터
            rootRect.offsetMax = new Vector2(0, -90);  // 타이틀 바(90px) 아래부터

            // 반투명 배경
            var rootBg = rootPanel.AddComponent<Image>();
            rootBg.color = new Color(0.02f, 0.015f, 0.06f, 0.85f);
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
            countRect.sizeDelta = new Vector2(0, 60);

            var bgImg = countBar.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.02f, 0.08f, 0.95f);
            bgImg.raycastTarget = false;

            var textGo = new GameObject("SongCount");
            textGo.transform.SetParent(countBar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            songCountText = textGo.AddComponent<TextMeshProUGUI>();
            songCountText.text = "0곡";
            songCountText.fontSize = 36;
            songCountText.color = NEON_CYAN_BRIGHT;
            songCountText.alignment = TextAlignmentOptions.Center;
            songCountText.fontStyle = FontStyles.Bold;
        }

        private void CreateScrollArea(Transform parent)
        {
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.offsetMin = new Vector2(12, 8);
            scrollViewRect.offsetMax = new Vector2(-12, -65);

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
            emptyText.color = new Color(0.5f, 0.5f, 0.6f);
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
        /// 곡 카드 생성 - 컴팩트 2행 구조
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

            // Row 2: BPM · 난이도 · 플레이 횟수
            string diffStars = "Lv." + Mathf.Clamp(song.DifficultyLevel, 0, 10);
            string playsStr = song.PlayCount > 0 ? $"{song.PlayCount}회" : "NEW";
            string row2 = song.BPM > 0
                ? $"{song.BPM} BPM · {diffStars} · {playsStr}"
                : $"{diffStars} · {playsStr}";
            CreateTMPText(infoPanel, "Info", row2, 24,
                NEON_CYAN_BRIGHT, TextAlignmentOptions.MidlineLeft);

            // Row 3: 랭크 + 점수
            string rankDisplay = string.IsNullOrEmpty(song.BestRank) ? "-" : song.BestRank;
            string scoreDisplay = song.BestScore > 0 ? song.BestScore.ToString("N0") : "--";
            var rankTmp = CreateTMPText(infoPanel, "RankScore",
                $"Best: <color=#{ColorUtility.ToHtmlStringRGB(GetRankColor(song.BestRank))}>{rankDisplay}</color> · {scoreDisplay}",
                22, new Color(0.55f, 0.55f, 0.65f), TextAlignmentOptions.MidlineLeft);
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

            if (!string.IsNullOrEmpty(song.AudioFileName))
            {
                StartCoroutine(LoadAudioAndStartGame(song));
            }
            else
            {
                var songData = CreateSongDataFromRecord(song, null);
                GameManager.Instance?.StartGame(songData);
            }
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
                    GameManager.Instance?.StartGame(songData);
                }
                else
                {
                    Debug.LogError($"[SongLibrary] 오디오 로드 실패: {www.error}");
                    if (songCountText != null)
                        songCountText.text = "로드 실패!";
                    yield return new WaitForSeconds(2f);
                    RefreshSongList();
                }
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

            float duration = 0.2f;
            float elapsed = 0f;

            Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float overshoot = 1.3f;
                float eased = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);

                rect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                cg.alpha = Mathf.Clamp01(t * 2.5f);
                yield return null;
            }

            rect.localScale = endScale;
            cg.alpha = 1f;
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
