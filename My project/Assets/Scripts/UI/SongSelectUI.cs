using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Core;
using AIBeat.Data;

namespace AIBeat.UI
{
    /// <summary>
    /// 곡 선택 UI - 로컬 MP3 라이브러리 목록 표시
    /// </summary>
    public class SongSelectUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button backButton;

        // 라이브러리 UI
        private SongLibraryUI songLibraryUI;

        private void Start()
        {
            AutoSetupReferences();
            Initialize();
            EnsureSiblingOrder();
        }

        private void AutoSetupReferences()
        {
            SetupBackground();

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

            // 뒤로가기 버튼
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
                var backRect = backButton.GetComponent<RectTransform>();
                if (backRect != null)
                {
                    var size = backRect.sizeDelta;
                    if (size.y < 50f) { size.y = 50f; backRect.sizeDelta = size; }
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
        /// 렌더링 순서 보장: Background(0) → LibraryPanel(1) → TitleBar(2) → BackButton(3)
        /// 숫자가 클수록 위에 그려짐
        /// </summary>
        private void EnsureSiblingOrder()
        {
            var bg = transform.Find("BackgroundImage");
            if (bg != null) bg.SetAsFirstSibling(); // index 0 (맨 뒤)

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
            rect.sizeDelta = new Vector2(0, 56);

            var bg = titleBar.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.08f, 1f);

            // 타이틀 텍스트
            var textGo = new GameObject("TitleText");
            textGo.transform.SetParent(titleBar.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "내 라이브러리";
            tmp.fontSize = 24;
            tmp.color = new Color(0.4f, 0.95f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
        }

        private void OnBackClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        /// <summary>
        /// 배경 이미지 설정 (SongSelectBG.jpg)
        /// </summary>
        private void SetupBackground()
        {
            var existing = transform.Find("BackgroundImage");
            if (existing != null) return;

            var bgGo = new GameObject("BackgroundImage");
            bgGo.transform.SetParent(transform, false);
            bgGo.transform.SetAsFirstSibling();

            var rect = bgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = bgGo.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.3f);

            var tex = Resources.Load<Texture2D>("UI/SongSelectBG");
            if (tex == null)
            {
                img.color = new Color(0.02f, 0.02f, 0.08f, 0.95f);
                return;
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }
    }
}
