#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AIBeat.Editor
{
    public static class GameViewSizeHelper
    {
        [InitializeOnLoadMethod]
        private static void AutoSetGameViewOnLoad()
        {
            // Automatically set Game View to 9:16 when Unity loads
            EditorApplication.delayCall += () =>
            {
                SetGameViewTo9x16();
            };
        }

        [MenuItem("AIBeat/Set Game View to 9x16 Mobile")]
        public static void SetGameViewTo9x16()
        {
            SetGameViewSize(1080, 1920, "9:16 Mobile");
        }

        public static void SetGameViewSize(int width, int height, string displayName)
        {
            // Get GameView type
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
            {
                Debug.LogError("GameView type not found");
                return;
            }

            // Get EditorWindow
            var gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView == null)
            {
                Debug.LogError("GameView window not found");
                return;
            }

            // Get currentSizeGroupType property
            var currentSizeGroupTypeProperty = gameViewType.GetProperty("currentSizeGroupType",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (currentSizeGroupTypeProperty == null)
            {
                Debug.LogError("currentSizeGroupType property not found");
                return;
            }

            // Get GameViewSizeGroupType enum
            var gameViewSizeGroupTypeType = Type.GetType("UnityEditor.GameViewSizeGroupType,UnityEditor");
            if (gameViewSizeGroupTypeType == null)
            {
                Debug.LogError("GameViewSizeGroupType not found");
                return;
            }

            // Get current platform (Standalone = 0)
            var currentGroupType = currentSizeGroupTypeProperty.GetValue(gameView, null);

            // Get GameViewSizes singleton
            var gameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var singletonProperty = gameViewSizesType.GetProperty("instance");
            var gameViewSizesInstance = singletonProperty.GetValue(null, null);

            // Get group for current platform
            var getGroupMethod = gameViewSizesType.GetMethod("GetGroup");
            var group = getGroupMethod.Invoke(gameViewSizesInstance, new object[] { currentGroupType });

            // Get GameViewSize type
            var gameViewSizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var gameViewSizeTypeType = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            // Create new GameViewSize (FixedResolution, width, height, displayName)
            var fixedResolution = (int)Enum.Parse(gameViewSizeTypeType, "FixedResolution");
            var gameViewSizeConstructor = gameViewSizeType.GetConstructor(new Type[]
            {
                gameViewSizeTypeType,
                typeof(int),
                typeof(int),
                typeof(string)
            });

            var newSize = gameViewSizeConstructor.Invoke(new object[]
            {
                fixedResolution,
                width,
                height,
                displayName
            });

            // Check if size already exists
            var getTotalCountMethod = group.GetType().GetMethod("GetTotalCount");
            var totalCount = (int)getTotalCountMethod.Invoke(group, null);

            var getGameViewSizeMethod = group.GetType().GetMethod("GetGameViewSize");
            int existingIndex = -1;

            for (int i = 0; i < totalCount; i++)
            {
                var size = getGameViewSizeMethod.Invoke(group, new object[] { i });
                var baseTextMethod = size.GetType().GetMethod("get_baseText");
                var baseText = (string)baseTextMethod.Invoke(size, null);

                if (baseText == displayName)
                {
                    existingIndex = i;
                    break;
                }
            }

            // Add size if it doesn't exist
            if (existingIndex == -1)
            {
                var addCustomSizeMethod = group.GetType().GetMethod("AddCustomSize");
                addCustomSizeMethod.Invoke(group, new object[] { newSize });
                existingIndex = totalCount; // New size is at the end
                Debug.Log($"[GameViewSizeHelper] Added new Game View size: {displayName} ({width}x{height})");
            }
            else
            {
                Debug.Log($"[GameViewSizeHelper] Game View size already exists: {displayName}");
            }

            // Set selected size index
            var selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            selectedSizeIndexProperty.SetValue(gameView, existingIndex, null);

            Debug.Log($"[GameViewSizeHelper] Game View set to {displayName} ({width}x{height})");
        }
    }
}
#endif
