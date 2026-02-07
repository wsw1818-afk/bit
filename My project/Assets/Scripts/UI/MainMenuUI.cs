using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Core;

namespace AIBeat.UI
{
    /// <summary>
    /// 메인 메뉴 UI 관리
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button libraryButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            AutoSetupReferences();
            Initialize();
        }

        /// <summary>
        /// Inspector 미연결 시 씬 오브젝트 이름으로 자동 탐색
        /// </summary>
        private void AutoSetupReferences()
        {
            if (playButton == null)
                playButton = transform.Find("PlayButton")?.GetComponent<Button>();
            if (libraryButton == null)
                libraryButton = transform.Find("LibraryButton")?.GetComponent<Button>();
            if (settingsButton == null)
                settingsButton = transform.Find("SettingsButton")?.GetComponent<Button>();
            if (exitButton == null)
                exitButton = transform.Find("ExitButton")?.GetComponent<Button>();
            if (titleText == null)
                titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            if (versionText == null)
                versionText = transform.Find("VersionText")?.GetComponent<TextMeshProUGUI>();

            // Settings 패널이 없으면 동적 생성
            if (settingsPanel == null)
            {
                var existing = transform.Find("SettingsPanel");
                if (existing != null)
                {
                    settingsPanel = existing.gameObject;
                }
                else
                {
                    settingsPanel = new GameObject("SettingsPanel");
                    settingsPanel.transform.SetParent(transform, false);
                    var rect = settingsPanel.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
            }
        }

        private void Initialize()
        {
            // 버튼 이벤트 연결 (중복 등록 방지)
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (libraryButton != null)
            {
                libraryButton.onClick.RemoveAllListeners();
                libraryButton.onClick.AddListener(OnLibraryClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
            }

            // 버전 표시
            if (versionText != null)
                versionText.text = $"v{Application.version}";

            // 게임 상태 설정
            GameManager.Instance?.ChangeState(GameManager.GameState.MainMenu);

            // 설정 패널에 SettingsUI 컴포넌트 자동 연결
            if (settingsPanel != null)
            {
                if (settingsPanel.GetComponent<SettingsUI>() == null)
                    settingsPanel.AddComponent<SettingsUI>();
                settingsPanel.SetActive(false);
            }

            // 모바일 버튼 최소 크기 보장
            EnsureButtonMobileSize();

            // 한국어 폰트 적용 (□□□ 방지)
            KoreanFontManager.ApplyFontToAll(gameObject);

            // 버튼 텍스트 한국어화
            SetButtonTextsKorean();

            // 타이틀 애니메이션
            AnimateTitle();
        }

        /// <summary>
        /// 버튼 스타일 개선 (모바일 터치 최적화 + 네온 효과)
        /// </summary>
        private void EnsureButtonMobileSize()
        {
            Button[] buttons = { playButton, libraryButton, settingsButton, exitButton };
            foreach (var btn in buttons)
            {
                if (btn == null) continue;

                // 크기 보장 (최소 60px 높이)
                var rect = btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var size = rect.sizeDelta;
                    if (size.y < 60f) { size.y = 60f; rect.sizeDelta = size; }
                }

                // 배경 이미지 색상
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);
                }

                // 네온 테두리 추가/강화
                var outline = btn.GetComponent<Outline>();
                if (outline == null)
                    outline = btn.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0.9f, 1f, 0.6f);
                outline.effectDistance = new Vector2(2, -2);

                // 버튼 색상 전환 설정
                var colors = btn.colors;
                colors.normalColor = new Color(0.05f, 0.05f, 0.15f, 0.95f);
                colors.highlightedColor = new Color(0.08f, 0.08f, 0.2f, 0.95f);
                colors.pressedColor = new Color(0f, 0.4f, 0.6f, 1f);
                colors.disabledColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
                btn.colors = colors;

                // 텍스트 스타일
                var tmp = btn.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    if (tmp.fontSize < 24) tmp.fontSize = 24;  // 22→24
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.4f, 0.95f, 1f, 1f);
                }
            }
        }

        /// <summary>
        /// 버튼 텍스트 한국어화
        /// </summary>
        private void SetButtonTextsKorean()
        {
            if (playButton != null)
            {
                var tmp = playButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "플레이";
            }
            if (libraryButton != null)
            {
                var tmp = libraryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "라이브러리";
            }
            if (settingsButton != null)
            {
                var tmp = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "설정";
            }
            if (exitButton != null)
            {
                var tmp = exitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "종료";
            }
        }

        private void AnimateTitle()
        {
            if (titleText == null) return;

            // 타이틀 스타일 강화
            titleText.fontSize = Mathf.Max(titleText.fontSize, 48);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0f, 0.85f, 1f, 1f);

            // 네온 효과
            var outline = titleText.GetComponent<Outline>();
            if (outline == null)
                outline = titleText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.6f, 1f, 0.8f);
            outline.effectDistance = new Vector2(3, -3);

            // 애니메이션
            titleText.transform.localScale = Vector3.zero;
            UIAnimator.ScaleTo(this, titleText.transform, Vector3.one, 0.5f);
        }

        private void OnPlayClicked()
        {
            // 곡 선택/생성 화면으로 이동
            GameManager.Instance?.LoadScene("SongSelect");
        }

        private void OnLibraryClicked()
        {
            // SongSelect 씬을 Library 탭이 열린 상태로 로드
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenLibraryOnSongSelect = true;
                GameManager.Instance.LoadScene("SongSelect");
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                settingsPanel.transform.localScale = Vector3.zero;
                UIAnimator.ScaleTo(this, settingsPanel, Vector3.one, 0.3f);
            }
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                UIAnimator.ScaleTo(this, settingsPanel, Vector3.zero, 0.2f, () => settingsPanel.SetActive(false));
            }
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (libraryButton != null) libraryButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        }
    }
}
