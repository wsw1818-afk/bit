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
            Initialize();
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

            // 타이틀 애니메이션
            AnimateTitle();
        }

        /// <summary>
        /// 버튼 최소 높이/폰트 크기 보장 (모바일 터치 최적화)
        /// </summary>
        private void EnsureButtonMobileSize()
        {
            Button[] buttons = { playButton, libraryButton, settingsButton, exitButton };
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                var rect = btn.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var size = rect.sizeDelta;
                    if (size.y < 56f) { size.y = 56f; rect.sizeDelta = size; }
                }
                var tmp = btn.GetComponentInChildren<TMP_Text>();
                if (tmp != null && tmp.fontSize < 22) tmp.fontSize = 22;
            }
        }

        private void AnimateTitle()
        {
            if (titleText == null) return;
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
