using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// 전체 UI의 통일된 색상 팔레트 (네온 사이버펑크 스타일)
    /// </summary>
    public static class UIColorPalette
    {
        // ===== 배경 =====
        public static readonly Color BG_DARK = new Color(0.01f, 0.01f, 0.05f, 0.98f);  // 메인 배경
        public static readonly Color BG_CARD = new Color(0.03f, 0.03f, 0.12f, 0.95f);  // 카드 배경
        public static readonly Color BG_BUTTON = new Color(0.05f, 0.05f, 0.15f, 0.95f);  // 버튼 배경

        // ===== 네온 효과 =====
        public static readonly Color NEON_CYAN = new Color(0f, 0.85f, 1f, 1f);  // 주 네온 색상
        public static readonly Color NEON_CYAN_BRIGHT = new Color(0.4f, 0.95f, 1f, 1f);  // 밝은 시안
        public static readonly Color NEON_BLUE = new Color(0f, 0.6f, 1f, 0.8f);  // 네온 블루
        public static readonly Color NEON_PURPLE = new Color(0.6f, 0.3f, 0.9f, 1f);  // 네온 퍼플
        public static readonly Color NEON_PINK = new Color(1f, 0.2f, 0.6f, 1f);  // 네온 핑크
        public static readonly Color NEON_ORANGE = new Color(1f, 0.6f, 0f, 1f);  // 네온 오렌지

        // ===== 텍스트 =====
        public static readonly Color TEXT_WHITE = Color.white;
        public static readonly Color TEXT_CYAN_BRIGHT = NEON_CYAN_BRIGHT;
        public static readonly Color TEXT_GRAY = new Color(0.7f, 0.7f, 0.8f, 1f);
        public static readonly Color TEXT_GRAY_DIM = new Color(0.5f, 0.5f, 0.6f, 1f);

        // ===== 테두리 =====
        public static readonly Color BORDER_CYAN = new Color(0f, 0.9f, 1f, 0.6f);
        public static readonly Color BORDER_PURPLE = new Color(0.6f, 0.3f, 0.9f, 0.5f);
        public static readonly Color BORDER_ORANGE = new Color(0.8f, 0.6f, 0f, 0.6f);
        public static readonly Color BORDER_DIM = new Color(0.3f, 0.3f, 0.6f, 0.5f);

        // ===== 상태 색상 =====
        public static readonly Color STATE_SELECTED = new Color(0f, 0.4f, 0.6f, 0.9f);
        public static readonly Color STATE_HOVER = new Color(0.08f, 0.08f, 0.2f, 0.95f);
        public static readonly Color STATE_PRESSED = new Color(0f, 0.4f, 0.6f, 1f);
        public static readonly Color STATE_DISABLED = new Color(0.2f, 0.2f, 0.3f, 0.5f);

        // ===== 슬라이더 =====
        public static readonly Color SLIDER_BG = new Color(0.08f, 0.08f, 0.18f, 0.9f);
        public static readonly Color SLIDER_FILL = NEON_CYAN;
        public static readonly Color SLIDER_HANDLE = Color.white;

        // ===== 특수 용도 =====
        public static readonly Color ERROR_RED = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color SUCCESS_GREEN = new Color(0.2f, 1f, 0.4f, 1f);
        public static readonly Color WARNING_YELLOW = new Color(1f, 0.9f, 0.2f, 1f);

        // ===== 그라데이션 헬퍼 =====
        public static Color Lerp(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }

        /// <summary>
        /// 네온 효과 강도 조절
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// 색상 밝기 조절
        /// </summary>
        public static Color Brighten(this Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a
            );
        }
    }
}
