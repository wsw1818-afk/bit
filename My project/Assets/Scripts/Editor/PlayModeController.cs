using UnityEditor;
using UnityEngine;

namespace AIBeat.Editor
{
    /// <summary>
    /// MCP 안전한 Play 모드 제어.
    /// EditorApplication.update 콜백으로 Play 모드 전환 (포커스 없어도 실행됨).
    /// delayCall은 에디터 포커스 없을 때 실행 안 되므로 사용 금지.
    /// </summary>
    public static class PlayModeController
    {
        private static bool _pendingPlay;
        private static bool _pendingStop;

        [MenuItem("Tools/A.I. BEAT/Toggle Play Mode _F5")]
        public static void TogglePlayMode()
        {
            if (EditorApplication.isPlaying)
                StopPlayMode();
            else
                StartPlayMode();
        }

        [MenuItem("Tools/A.I. BEAT/Start Play Mode")]
        public static void StartPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[PlayModeController] Already in Play Mode");
                return;
            }
            Debug.Log("[PlayModeController] Starting Play Mode (next update)...");
            _pendingPlay = true;
            EditorApplication.update += OnUpdate;
        }

        [MenuItem("Tools/A.I. BEAT/Stop Play Mode")]
        public static void StopPlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[PlayModeController] Not in Play Mode");
                return;
            }
            Debug.Log("[PlayModeController] Stopping Play Mode (next update)...");
            _pendingStop = true;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            EditorApplication.update -= OnUpdate;
            if (_pendingPlay)
            {
                _pendingPlay = false;
                EditorApplication.isPlaying = true;
            }
            if (_pendingStop)
            {
                _pendingStop = false;
                EditorApplication.isPlaying = false;
            }
        }

        [MenuItem("Tools/A.I. BEAT/Open Settings Panel")]
        public static void OpenSettingsPanel()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PlayModeController] Play 모드에서만 사용 가능합니다.");
                return;
            }

            var mainMenuUI = Object.FindFirstObjectByType<UI.MainMenuUI>();
            if (mainMenuUI == null)
            {
                Debug.LogWarning("[PlayModeController] MainMenuUI를 찾을 수 없습니다.");
                return;
            }

            var settingsPanelField = typeof(UI.MainMenuUI).GetField("settingsPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (settingsPanelField != null)
            {
                var settingsPanel = settingsPanelField.GetValue(mainMenuUI) as GameObject;
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(true);
                    if (settingsPanel.GetComponent<UI.SettingsUI>() == null)
                        settingsPanel.AddComponent<UI.SettingsUI>();
                    Debug.Log("[PlayModeController] Settings Panel 열림");
                }
            }
        }

        [MenuItem("Tools/A.I. BEAT/Close Settings Panel")]
        public static void CloseSettingsPanel()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PlayModeController] Play 모드에서만 사용 가능합니다.");
                return;
            }

            var mainMenuUI = Object.FindFirstObjectByType<UI.MainMenuUI>();
            if (mainMenuUI == null)
            {
                Debug.LogWarning("[PlayModeController] MainMenuUI를 찾을 수 없습니다.");
                return;
            }

            var settingsPanelField = typeof(UI.MainMenuUI).GetField("settingsPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (settingsPanelField != null)
            {
                var settingsPanel = settingsPanelField.GetValue(mainMenuUI) as GameObject;
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(false);
                    Debug.Log("[PlayModeController] Settings Panel 닫힘");
                }
            }
        }
    }
}
