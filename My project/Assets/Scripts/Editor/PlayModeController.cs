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

            // settingsPanel 필드에 접근
            var settingsPanelField = typeof(UI.MainMenuUI).GetField("settingsPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (settingsPanelField != null)
            {
                var settingsPanel = settingsPanelField.GetValue(mainMenuUI) as GameObject;
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(true);

                    // SettingsUI 컴포넌트 추가/확인
                    if (settingsPanel.GetComponent<UI.SettingsUI>() == null)
                    {
                        settingsPanel.AddComponent<UI.SettingsUI>();
                    }

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
