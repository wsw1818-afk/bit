using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBeat.UI
{
    public class SplashController : MonoBehaviour
    {
        [SerializeField] private float autoTransitionDelay = 2f;
        private float timer;

        private void Start()
        {
            Application.runInBackground = true;
            Debug.Log("[SplashController] Start() - 자동 전환 대기 중");
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // 자동 전환 또는 아무 키/터치로 메인 메뉴 이동
            if (timer >= autoTransitionDelay || Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                Debug.Log("[SplashController] MainMenuScene으로 전환");
                SceneManager.LoadScene("MainMenuScene");
            }
        }
    }
}
