#if UNITY_EDITOR
using UnityEditor;

namespace AIBeat.Editor
{
    public static class PlayModeHelper
    {
        [MenuItem("AIBeat/Enter Play Mode")]
        public static void EnterPlayMode()
        {
            EditorApplication.isPlaying = true;
        }

        [MenuItem("AIBeat/Exit Play Mode")]
        public static void ExitPlayMode()
        {
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
