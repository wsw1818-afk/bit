#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AIBeat.Editor
{
    public static class SetGameViewNow
    {
        [MenuItem("AIBeat/Set 9x16 NOW")]
        public static void SetTo9x16()
        {
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);

                var currentSizeGroupTypeProperty = gameViewType.GetProperty("currentSizeGroupType",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var currentGroupType = currentSizeGroupTypeProperty.GetValue(gameView, null);

                var gameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
                var singletonProperty = gameViewSizesType.GetProperty("instance");
                var gameViewSizesInstance = singletonProperty.GetValue(null, null);

                var getGroupMethod = gameViewSizesType.GetMethod("GetGroup");
                var group = getGroupMethod.Invoke(gameViewSizesInstance, new object[] { currentGroupType });

                // Find existing 9:16 or create new
                var getTotalCountMethod = group.GetType().GetMethod("GetTotalCount");
                var totalCount = (int)getTotalCountMethod.Invoke(group, null);

                var getGameViewSizeMethod = group.GetType().GetMethod("GetGameViewSize");
                int targetIndex = -1;

                for (int i = 0; i < totalCount; i++)
                {
                    var size = getGameViewSizeMethod.Invoke(group, new object[] { i });
                    var baseTextMethod = size.GetType().GetMethod("get_baseText");
                    var baseText = (string)baseTextMethod.Invoke(size, null);

                    if (baseText.Contains("9:16") || baseText.Contains("1080") && baseText.Contains("1920"))
                    {
                        targetIndex = i;
                        Debug.Log($"[SetGameViewNow] Found existing 9:16 at index {i}: {baseText}");
                        break;
                    }
                }

                // If not found, create new
                if (targetIndex == -1)
                {
                    var gameViewSizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                    var gameViewSizeTypeType = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
                    var fixedResolution = (int)Enum.Parse(gameViewSizeTypeType, "FixedResolution");

                    var ctor = gameViewSizeType.GetConstructor(new Type[]
                    {
                        gameViewSizeTypeType,
                        typeof(int),
                        typeof(int),
                        typeof(string)
                    });

                    var newSize = ctor.Invoke(new object[] { fixedResolution, 1080, 1920, "9:16 Mobile" });

                    var addCustomSizeMethod = group.GetType().GetMethod("AddCustomSize");
                    addCustomSizeMethod.Invoke(group, new object[] { newSize });

                    targetIndex = totalCount;
                    Debug.Log($"[SetGameViewNow] Created new 9:16 Mobile at index {targetIndex}");
                }

                // Set selected size
                var selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                selectedSizeIndexProperty.SetValue(gameView, targetIndex, null);

                Debug.Log($"[SetGameViewNow] ✅ Game View set to 9:16 Mobile (index {targetIndex})");
                gameView.Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SetGameViewNow] Failed to set Game View: {e.Message}");
            }
        }
    }
}
#endif
