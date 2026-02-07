using UnityEngine;
using System;
using AIBeat.UI;

namespace AIBeat.Core
{
    /// <summary>
    /// 튜토리얼 진행 상태를 관리하는 싱글톤 매니저
    /// PlayerPrefs로 완료 여부를 저장하여 첫 실행 시에만 튜토리얼 표시
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        // 튜토리얼 단계 정의
        public enum TutorialStep
        {
            Welcome,     // 환영 메시지 + 게임 설명
            Controls,    // 키 배치 안내 (시각적 키보드 다이어그램)
            NoteTypes,   // 노트 종류 설명 (Tap/Long/Scratch)
            Judgement,   // 판정 설명 (PERFECT~MISS)
            Practice,    // 연습 안내
            Complete     // 튜토리얼 완료
        }

        private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
        private const int TOTAL_STEPS = 6;

        private TutorialStep currentStep = TutorialStep.Welcome;
        private TutorialUI tutorialUI;
        private bool isActive;

        // 튜토리얼 완료 이벤트
        public event Action OnTutorialCompleted;

        public TutorialStep CurrentStep => currentStep;
        public bool IsActive => isActive;
        public int CurrentStepIndex => (int)currentStep;
        public int TotalSteps => TOTAL_STEPS;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 튜토리얼 완료 여부 확인
        /// </summary>
        public static bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        }

        /// <summary>
        /// 튜토리얼 완료 상태 초기화 (디버그용)
        /// </summary>
        public static void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(TUTORIAL_COMPLETED_KEY);
            PlayerPrefs.Save();
#if UNITY_EDITOR
            Debug.Log("[TutorialManager] Tutorial reset complete");
#endif
        }

        /// <summary>
        /// 튜토리얼 시작
        /// </summary>
        public void StartTutorial()
        {
            isActive = true;
            currentStep = TutorialStep.Welcome;

            // TutorialUI 동적 생성 (Canvas에 추가)
            CreateTutorialUI();

            if (tutorialUI != null)
            {
                tutorialUI.Show(currentStep);
            }

#if UNITY_EDITOR
            Debug.Log("[TutorialManager] Tutorial started");
#endif
        }

        /// <summary>
        /// 다음 단계로 진행
        /// </summary>
        public void NextStep()
        {
            if (!isActive) return;

            int nextIndex = (int)currentStep + 1;

            if (nextIndex >= TOTAL_STEPS)
            {
                // 마지막 단계 이후 → 완료 처리
                CompleteTutorial();
                return;
            }

            currentStep = (TutorialStep)nextIndex;

            if (tutorialUI != null)
            {
                tutorialUI.Show(currentStep);
            }

#if UNITY_EDITOR
            Debug.Log($"[TutorialManager] Step progress: {currentStep} ({nextIndex + 1}/{TOTAL_STEPS})");
#endif
        }

        /// <summary>
        /// 튜토리얼 건너뛰기
        /// </summary>
        public void SkipTutorial()
        {
            CompleteTutorial();
#if UNITY_EDITOR
            Debug.Log("[TutorialManager] Tutorial skipped");
#endif
        }

        /// <summary>
        /// 튜토리얼 완료 처리
        /// </summary>
        private void CompleteTutorial()
        {
            isActive = false;

            // PlayerPrefs에 완료 저장
            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
            PlayerPrefs.Save();

            // UI 숨기기
            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }

            OnTutorialCompleted?.Invoke();
#if UNITY_EDITOR
            Debug.Log("[TutorialManager] Tutorial completed!");
#endif
        }

        /// <summary>
        /// TutorialUI 동적 생성
        /// </summary>
        private void CreateTutorialUI()
        {
            // 씬에 이미 있으면 재사용
            tutorialUI = FindFirstObjectByType<TutorialUI>();
            if (tutorialUI != null) return;

            // Canvas 찾기
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[TutorialManager] Canvas not found");
                return;
            }

            // TutorialUI 게임오브젝트 생성
            var tutorialGo = new GameObject("TutorialUI");
            tutorialGo.transform.SetParent(canvas.transform, false);

            // RectTransform 설정 (화면 전체 덮기)
            var rect = tutorialGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            tutorialUI = tutorialGo.AddComponent<TutorialUI>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
