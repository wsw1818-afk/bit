using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using AIBeat.UI;

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

        #region Button Animation Extensions

        /// <summary>
        /// 버튼 호버 시 부드러운 크기 변화 애니메이션
        /// </summary>
        public static Coroutine ButtonHover(MonoBehaviour host, Transform target, bool isHovering, float duration = 0.15f)
        {
            Vector3 targetScale = isHovering ? Vector3.one * 1.03f : Vector3.one;
            return host.StartCoroutine(ScaleCoroutine(target, targetScale, duration, null));
        }

        /// <summary>
        /// 버튼 클릭 시 플래시 효과
        /// </summary>
        public static Coroutine ButtonFlash(MonoBehaviour host, Image targetImage, Color flashColor, float duration = 0.2f)
        {
            return host.StartCoroutine(FlashCoroutine(targetImage, flashColor, duration));
        }

        private static IEnumerator FlashCoroutine(Image targetImage, Color flashColor, float duration)
        {
            if (targetImage == null) yield break;
            
            Color originalColor = targetImage.color;
            float elapsed = 0f;
            float halfDuration = duration / 2f;

            // 플래시 인
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                targetImage.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }

            // 플래시 아웃
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                targetImage.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }

            targetImage.color = originalColor;
        }

        /// <summary>
        /// 버튼 글로우 효과 (Outline 컴포넌트 사용)
        /// </summary>
        public static Coroutine ButtonGlow(MonoBehaviour host, Outline outline, Color targetColor, float duration = 0.2f)
        {
            if (outline == null) return null;
            return host.StartCoroutine(GlowCoroutine(outline, targetColor, duration));
        }

        private static IEnumerator GlowCoroutine(Outline outline, Color targetColor, float duration)
        {
            Color originalColor = outline.effectColor;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutQuad(t);
                outline.effectColor = Color.Lerp(originalColor, targetColor, t);
                yield return null;
            }

            outline.effectColor = targetColor;
        }

        /// <summary>
        /// 리플 효과 (버튼 클릭 시 퍼지는 원형 애니메이션)
        /// </summary>
        public static Coroutine RippleEffect(MonoBehaviour host, Transform parent, Vector2 localPosition, Color rippleColor, float startSize = 10f, float endSize = 200f, float duration = 0.5f)
        {
            return host.StartCoroutine(RippleCoroutine(parent, localPosition, rippleColor, startSize, endSize, duration));
        }

        private static IEnumerator RippleCoroutine(Transform parent, Vector2 localPosition, Color rippleColor, float startSize, float endSize, float duration)
        {
            // 리플 오브젝트 생성
            var rippleGO = new GameObject("RippleEffect");
            rippleGO.transform.SetParent(parent, false);
            
            var rect = rippleGO.AddComponent<RectTransform>();
            rect.anchoredPosition = localPosition;
            rect.sizeDelta = new Vector2(startSize, startSize);
            
            var img = rippleGO.AddComponent<Image>();
            img.color = rippleColor;
            img.raycastTarget = false;
            
            // 원형 스프라이트 생성
            img.sprite = CreateCircleSprite();
            
            float elapsed = 0f;
            Color startColor = rippleColor;
            Color endColor = rippleColor.WithAlpha(0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutQuad(t);
                
                // 크기 증가
                float currentSize = Mathf.Lerp(startSize, endSize, t);
                rect.sizeDelta = new Vector2(currentSize, currentSize);
                
                // 투명도 감소
                img.color = Color.Lerp(startColor, endColor, t);
                
                yield return null;
            }

            // 정리
            UnityEngine.Object.Destroy(rippleGO);
        }

        private static Sprite CreateCircleSprite()
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - (dist - radius + 1.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// 부드러운 색상 전환
        /// </summary>
        public static Coroutine ColorTransition(MonoBehaviour host, Graphic target, Color to, float duration, Action onComplete = null)
        {
            return host.StartCoroutine(ColorCoroutine(target, to, duration, onComplete));
        }

        private static IEnumerator ColorCoroutine(Graphic target, Color to, float duration, Action onComplete)
        {
            if (target == null) yield break;
            
            Color from = target.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = EaseOutQuad(t);
                target.color = Color.Lerp(from, to, t);
                yield return null;
            }

            target.color = to;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 이징 함수 - EaseOutQuad
        /// </summary>
        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// 이징 함수 - EaseInOutQuad
        /// </summary>
        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        /// <summary>
        /// 이징 함수 - EaseOutElastic (버튼 클릭 시 사용)
        /// </summary>
        private static float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            return t == 0f ? 0f : t == 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        #endregion
    }
}
