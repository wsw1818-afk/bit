using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBeat.Core;

namespace AIBeat.UI
{
    /// <summary>
    /// 전체 UI에서 공용으로 사용하는 버튼 스타일 헬퍼
    /// 디자인 에셋(Btn_Normal/Hover/Pressed) 기반 SpriteSwap 버튼 스타일 적용
    /// </summary>
    public static class UIButtonStyleHelper
    {
        // 버튼 스프라이트 캐시
        private static Sprite _normalSprite;
        private static Sprite _hoverSprite;
        private static Sprite _pressedSprite;
        private static bool _spritesLoaded = false;

        /// <summary>
        /// 버튼 스프라이트 로드 (최초 1회만 로드)
        /// </summary>
        public static void EnsureSpritesLoaded()
        {
            if (_spritesLoaded) return;

            _normalSprite = Resources.Load<Sprite>("AIBeat_Design/UI/Buttons/Btn_Normal");
            _hoverSprite = Resources.Load<Sprite>("AIBeat_Design/UI/Buttons/Btn_Hover");
            _pressedSprite = Resources.Load<Sprite>("AIBeat_Design/UI/Buttons/Btn_Pressed");

            _spritesLoaded = true;

            if (_normalSprite == null)
                Debug.LogWarning("[UIButtonStyleHelper] Btn_Normal 스프라이트를 찾을 수 없습니다.");
        }

        public static Sprite NormalSprite => _normalSprite;
        public static Sprite HoverSprite => _hoverSprite;
        public static Sprite PressedSprite => _pressedSprite;
        public static bool HasSprites => _normalSprite != null;

        /// <summary>
        /// 기존 버튼에 디자인 에셋 스타일 적용
        /// </summary>
        public static void ApplyDesignStyle(Button button, string text = null, float fontSize = 24f)
        {
            if (button == null) return;
            EnsureSpritesLoaded();

            var img = button.GetComponent<Image>();
            if (img == null)
                img = button.gameObject.AddComponent<Image>();

            // 스프라이트가 있으면 SpriteSwap 사용
            if (_normalSprite != null)
            {
                img.sprite = _normalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                button.transition = Button.Transition.SpriteSwap;

                var spriteState = new SpriteState();
                spriteState.highlightedSprite = _hoverSprite;
                spriteState.pressedSprite = _pressedSprite;
                spriteState.selectedSprite = _hoverSprite;
                spriteState.disabledSprite = _normalSprite;
                button.spriteState = spriteState;

                // Outline 제거 (스프라이트가 이미 디자인 포함)
                var outline = button.GetComponent<Outline>();
                if (outline != null)
                    Object.Destroy(outline);
            }
            else
            {
                // 스프라이트 없으면 기존 ColorTint 방식 유지
                img.color = UIColorPalette.BG_BUTTON;
                button.transition = Button.Transition.ColorTint;

                var colors = button.colors;
                colors.normalColor = UIColorPalette.BG_BUTTON;
                colors.highlightedColor = new Color(0.08f, 0.08f, 0.2f, 0.95f);
                colors.pressedColor = new Color(0f, 0.4f, 0.6f, 1f);
                button.colors = colors;
            }

            // 텍스트 업데이트
            if (!string.IsNullOrEmpty(text))
            {
                var tmp = button.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = text;
                    tmp.fontSize = fontSize;
                    tmp.color = UIColorPalette.NEON_CYAN_BRIGHT;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        /// <summary>
        /// 새 버튼 GameObject 생성 + 디자인 스타일 적용
        /// </summary>
        public static Button CreateStyledButton(Transform parent, string name, string text,
            float preferredHeight = 80f, float fontSize = 28f)
        {
            EnsureSpritesLoaded();

            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            var rect = btnGo.AddComponent<RectTransform>();

            // LayoutElement 추가
            var layoutElem = btnGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = preferredHeight;
            layoutElem.minHeight = 50f;

            // Image 컴포넌트
            var img = btnGo.AddComponent<Image>();

            // Button 컴포넌트
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            // 스프라이트 적용
            if (_normalSprite != null)
            {
                img.sprite = _normalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                btn.transition = Button.Transition.SpriteSwap;

                var spriteState = new SpriteState();
                spriteState.highlightedSprite = _hoverSprite;
                spriteState.pressedSprite = _pressedSprite;
                spriteState.selectedSprite = _hoverSprite;
                spriteState.disabledSprite = _normalSprite;
                btn.spriteState = spriteState;
            }
            else
            {
                // 폴백: 기존 색상 스타일
                img.color = UIColorPalette.BG_BUTTON;
                btn.transition = Button.Transition.ColorTint;

                var outline = btnGo.AddComponent<Outline>();
                outline.effectColor = UIColorPalette.BORDER_CYAN;
                outline.effectDistance = new Vector2(2, -2);

                var colors = btn.colors;
                colors.normalColor = UIColorPalette.BG_BUTTON;
                colors.highlightedColor = new Color(0.08f, 0.08f, 0.2f, 0.95f);
                colors.pressedColor = new Color(0f, 0.4f, 0.6f, 1f);
                btn.colors = colors;
            }

            // 텍스트 생성
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = UIColorPalette.NEON_CYAN_BRIGHT;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // 한국어 폰트 적용
            var korFont = KoreanFontManager.KoreanFont;
            if (korFont != null)
                tmp.font = korFont;

            return btn;
        }

        /// <summary>
        /// 작은 인라인 버튼 생성 (캘리브레이션 등)
        /// </summary>
        public static Button CreateInlineButton(Transform parent, string name, string text,
            Color textColor, Color bgColor, float fontSize = 16f)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = bgColor * 1.5f;
            btn.colors = colors;

            // 텍스트
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btn;
        }
    }
}
