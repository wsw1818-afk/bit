using UnityEditor;
using UnityEngine;

namespace AIBeat.Editor
{
    public static class PlayModeController
    {
        [MenuItem("Tools/A.I. BEAT/Toggle Play Mode _F5")]
        public static void TogglePlayMode()
        {
            EditorApplication.isPlaying = !EditorApplication.isPlaying;
            Debug.Log($"[PlayModeController] Play Mode: {(EditorApplication.isPlaying ? "Started" : "Stopped")}");
        }

        [MenuItem("Tools/A.I. BEAT/Start Play Mode")]
        public static void StartPlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[PlayModeController] Starting Play Mode...");
                EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.Log("[PlayModeController] Already in Play Mode");
            }
        }

        [MenuItem("Tools/A.I. BEAT/Stop Play Mode")]
        public static void StopPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[PlayModeController] Stopping Play Mode...");
                EditorApplication.isPlaying = false;
            }
            else
            {
                Debug.Log("[PlayModeController] Not in Play Mode");
            }
        }
    }
}
