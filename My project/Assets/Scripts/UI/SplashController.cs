using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBeat.UI
{
    public class SplashController : MonoBehaviour
    {
        private void Update()
        {
            // 아무 키나 터치하면 메인 메뉴로 이동
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                SceneManager.LoadScene("MainMenuScene");
            }
        }
    }
}
