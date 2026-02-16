using UnityEditor;
using UnityEngine;

namespace AIBeat.Editor
{
    /// <summary>
    /// MCP에서 Play Mode 테스트를 실행하기 위한 에디터 스크립트
    /// </summary>
    public static class PlayTestRunner
    {
        [MenuItem("Tools/A.I. BEAT/Start Play Test")]
        public static void StartPlayTest()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[PlayTest] Already in Play Mode");
                return;
            }

            // Gameplay 씬 열기
            string scenePath = "Assets/Scenes/Gameplay.unity";
            if (System.IO.File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log("[PlayTest] Gameplay scene loaded");
            }

            // 다음 프레임에서 Play Mode 진입 (MCP 응답 후)
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = true;
                Debug.Log("[PlayTest] Entering Play Mode...");
            };
        }

        [MenuItem("Tools/A.I. BEAT/Stop Play Test")]
        public static void StopPlayTest()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[PlayTest] Stopping Play Mode...");
            }
        }
    }
}
