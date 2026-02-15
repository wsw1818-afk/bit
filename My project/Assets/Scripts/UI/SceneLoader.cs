using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIBeat.UI
{
    public class SceneLoader : MonoBehaviour
    {
        public void LoadSplash()
        {
            SceneManager.LoadScene("SplashScene");
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenuScene");
        }

        public void LoadSongSelect()
        {
            SceneManager.LoadScene("SongSelectScene");
        }

        public void LoadGame()
        {
            SceneManager.LoadScene("Gameplay");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
