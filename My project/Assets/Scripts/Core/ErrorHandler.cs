using System;
using UnityEngine;

namespace AIBeat.Core
{
    /// <summary>
    /// 안전한 호출 래퍼 - 예외 발생 시 크래시 대신 로그 출력
    /// </summary>
    public static class ErrorHandler
    {
        public static void SafeCall(Action action, string context = "")
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[{context}] Error: {e.Message}\n{e.StackTrace}");
#else
                Debug.LogError($"[{context}] Error: {e.Message}");
#endif
            }
        }

        public static T SafeCall<T>(Func<T> func, T defaultValue, string context = "")
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[{context}] Error: {e.Message}\n{e.StackTrace}");
#else
                Debug.LogError($"[{context}] Error: {e.Message}");
#endif
                return defaultValue;
            }
        }

        /// <summary>
        /// 코루틴 내부에서 사용할 수 있는 안전한 호출
        /// </summary>
        public static void SafeInvoke(Action action, string context = "")
        {
            if (action == null) return;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[{context}] SafeInvoke failed: {e.Message}");
#endif
            }
        }
    }
}
