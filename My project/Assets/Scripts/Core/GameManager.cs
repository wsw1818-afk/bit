using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using AIBeat.Data;
using AIBeat.Network;

namespace AIBeat.Core
{
    /// <summary>
    /// 게임 전체 상태를 관리하는 싱글톤 매니저
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        public GameState CurrentState => currentState;

        public event Action<GameState> OnStateChanged;

        private bool isLoadingScene = false;

        public enum GameState
        {
            MainMenu,
            SongSelect,
            Loading,
            Gameplay,
            Paused,
            Result
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 첫 실행 시 튜토리얼 체크
            CheckTutorial();
        }

        /// <summary>
        /// 앱 시작 시 가장 먼저 실행 — 씬 로드 전에 Portrait 강제
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ForcePortrait()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        private void Initialize()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 세로 모드 강제 (Initialize에서도 재확인)
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        /// <summary>
        /// 튜토리얼 완료 여부 확인 → 미완료 시 자동 시작
        /// </summary>
        private void CheckTutorial()
        {
            // 튜토리얼은 MainMenu 씬에서만 시작
            if (SceneManager.GetActiveScene().name != "MainMenu")
                return;

            if (TutorialManager.IsTutorialCompleted()) return;

            // TutorialManager가 없으면 생성
            var tutorialManager = FindFirstObjectByType<TutorialManager>();
            if (tutorialManager == null)
            {
                var tutorialGo = new GameObject("TutorialManager");
                tutorialManager = tutorialGo.AddComponent<TutorialManager>();
            }

            // 튜토리얼 완료 시 첫 곡 자동 플레이
            tutorialManager.OnTutorialCompleted += OnTutorialFinished;
            tutorialManager.StartTutorial();
        }

        /// <summary>
        /// 튜토리얼 완료 후 첫 곡 자동 플레이 (FakeSongGenerator 활용)
        /// </summary>
        private void OnTutorialFinished()
        {
            // 이벤트 구독 해제
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnTutorialCompleted -= OnTutorialFinished;

            // FakeSongGenerator에서 첫 번째 데모 곡 가져오기
            var fakeGenerator = FindFirstObjectByType<FakeSongGenerator>();
            if (fakeGenerator != null)
            {
                var demoSongs = fakeGenerator.GetAllDemoSongs();
                if (demoSongs != null && demoSongs.Count > 0)
                {
                    // 첫 번째 곡 "Neon Rush" 자동 시작
                    StartGame(demoSongs[0]);
#if UNITY_EDITOR
                    Debug.Log($"[GameManager] 튜토리얼 완료 → 첫 곡 자동 시작: {demoSongs[0].Title}");
#endif
                    return;
                }
            }

#if UNITY_EDITOR
            Debug.Log("[GameManager] 튜토리얼 완료 (FakeSongGenerator 없음, 메뉴로 돌아감)");
#endif
        }

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        public void LoadScene(string sceneName)
        {
            if (isLoadingScene) return;
            isLoadingScene = true;

            ChangeState(GameState.Loading);
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
                op.completed += (asyncOp) => isLoadingScene = false;
            else
                isLoadingScene = false;
        }

        public void StartGame(SongData songData)
        {
            // 곡 데이터를 저장하고 게임 씬으로 이동
            CurrentSongData = songData;
            LoadScene("Gameplay");
        }

        public SongData CurrentSongData { get; private set; }

        /// <summary>
        /// SongSelect 씬 로드 시 Library 탭을 자동으로 열지 여부
        /// MainMenuUI의 Library 버튼에서 설정
        /// </summary>
        public bool OpenLibraryOnSongSelect { get; set; }

        public void PauseGame()
        {
            if (currentState == GameState.Gameplay)
            {
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                Time.timeScale = 1f;
                ChangeState(GameState.Gameplay);
            }
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            LoadScene("MainMenu");
            ChangeState(GameState.MainMenu);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentState == GameState.Gameplay)
            {
                PauseGame();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this)
                Instance = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && currentState != GameState.Gameplay && currentState != GameState.Paused)
            {
                if (Time.timeScale < 0.01f)
                {
                    Time.timeScale = 1f;
                    Debug.LogWarning($"[GameManager] timeScale was 0 in state {currentState}, restored to 1");
                }
            }
        }
    }
}
