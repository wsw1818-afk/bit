using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AIBeat.Core;

namespace AIBeat.UI
{
    /// <summary>
    /// 버튼 호버/클릭 애니메이션 컴포넌트
    /// - 호버 시 부드러운 크기 변화
    /// - 클릭 시 플래시 효과
    /// - 리플 효과 (클릭 위치에서 퍼지는 애니메이션)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.03f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private float clickScale = 0.97f;
        [SerializeField] private float clickDuration = 0.1f;
        
        [Header("Flash Effect")]
        [SerializeField] private bool enableFlash = true;
        [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private float flashDuration = 0.2f;
        
        [Header("Ripple Effect")]
        [SerializeField] private bool enableRipple = true;
        [SerializeField] private Color rippleColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private float rippleDuration = 0.5f;
        
        [Header("Glow Effect")]
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private float glowIntensity = 1.5f;
        [SerializeField] private float glowDuration = 0.2f;

        private Button button;
        private Image buttonImage;
        private Outline buttonOutline;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Color originalOutlineColor;
        private bool isHovering = false;
        private Coroutine currentAnimation;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonOutline = GetComponent<Outline>();
            rectTransform = GetComponent<RectTransform>();
            originalScale = transform.localScale;
            
            if (buttonOutline != null)
                originalOutlineColor = buttonOutline.effectColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;
            
            isHovering = true;
            
            // 호버 스케일 애니메이션
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = UIAnimator.ScaleTo(this, transform, originalScale * hoverScale, hoverDuration);
            
            // 글로우 효과
            if (enableGlow && buttonOutline != null)
            {
                Color glowColor = originalOutlineColor * glowIntensity;
                glowColor.a = Mathf.Min(glowColor.a, 1f);
                UIAnimator.ButtonGlow(this, buttonOutline, glowColor, glowDuration);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;
            
            isHovering = false;
            
            // 원래 스케일로 복귀
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = UIAnimator.ScaleTo(this, transform, originalScale, hoverDuration);
            
            // 글로우 원래대로
            if (enableGlow && buttonOutline != null)
            {
                UIAnimator.ButtonGlow(this, buttonOutline, originalOutlineColor, glowDuration);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;
            
            // 클릭 스케일 애니메이션
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = UIAnimator.ScaleTo(this, transform, originalScale * clickScale, clickDuration);
            
            // 플래시 효과
            if (enableFlash && buttonImage != null)
            {
                UIAnimator.ButtonFlash(this, buttonImage, flashColor, flashDuration);
            }
            
            // 리플 효과
            if (enableRipple && rectTransform != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
                UIAnimator.RippleEffect(this, transform, localPoint, rippleColor, 10f, 200f, rippleDuration);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;
            
            // 호버 상태에 따라 스케일 복귀
            Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = UIAnimator.ScaleTo(this, transform, targetScale, clickDuration);
        }

        /// <summary>
        /// 버튼 활성화/비활성화 시 애니메이션
        /// </summary>
        public void SetInteractable(bool interactable, bool animate = true)
        {
            if (button != null)
                button.interactable = interactable;
            
            if (animate)
            {
                float targetAlpha = interactable ? 1f : 0.5f;
                var canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    UIAnimator.FadeCanvasGroup(this, canvasGroup, targetAlpha, 0.2f);
                }
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 원래 스케일로 복귀
            transform.localScale = originalScale;
            isHovering = false;
        }
    }
}
