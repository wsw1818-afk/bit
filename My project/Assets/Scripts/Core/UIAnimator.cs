using UnityEngine;
using System;
using System.Collections;

namespace AIBeat.Core
{
    /// <summary>
    /// LeanTween 대체용 간단한 UI 애니메이션 유틸리티
    /// </summary>
    public static class UIAnimator
    {
        public static Coroutine ScaleTo(MonoBehaviour host, GameObject target, Vector3 to, float duration, Action onComplete = null)
        {
            return host.StartCoroutine(ScaleCoroutine(target.transform, to, duration, onComplete));
        }

        public static Coroutine ScaleTo(MonoBehaviour host, Transform target, Vector3 to, float duration, Action onComplete = null)
        {
            return host.StartCoroutine(ScaleCoroutine(target, to, duration, onComplete));
        }

        private static IEnumerator ScaleCoroutine(Transform target, Vector3 to, float duration, Action onComplete)
        {
            Vector3 from = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack 효과
                t = EaseOutBack(t);
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            target.localScale = to;
            onComplete?.Invoke();
        }

        public static Coroutine ScaleFrom(MonoBehaviour host, GameObject target, Vector3 from, float duration)
        {
            Vector3 to = target.transform.localScale;
            target.transform.localScale = from;
            return host.StartCoroutine(ScaleCoroutine(target.transform, to, duration, null));
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static Coroutine FadeCanvasGroup(MonoBehaviour host, CanvasGroup cg, float to, float duration, Action onComplete = null)
        {
            return host.StartCoroutine(FadeCoroutine(cg, to, duration, onComplete));
        }

        private static IEnumerator FadeCoroutine(CanvasGroup cg, float to, float duration, Action onComplete)
        {
            float from = cg.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            cg.alpha = to;
            onComplete?.Invoke();
        }
    }
}
