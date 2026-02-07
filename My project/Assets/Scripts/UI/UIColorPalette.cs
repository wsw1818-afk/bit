using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// BIT.jpg 디자인 기반 통일 색상 팔레트
    /// 네온 사이버펑크 + 이퀄라이저 바 스타일
    /// </summary>
    public static class UIColorPalette
    {
        // ===== 배경 (매우 어두운 남색~보라) =====
        public static readonly Color BG_DEEP = new Color(0.012f, 0.008f, 0.04f, 1f);     // 최심부 배경
        public static readonly Color BG_DARK = new Color(0.02f, 0.015f, 0.06f, 0.98f);   // 메인 배경
        public static readonly Color BG_CARD = new Color(0.04f, 0.03f, 0.12f, 0.92f);    // 카드 배경
        public static readonly Color BG_BUTTON = new Color(0.06f, 0.04f, 0.18f, 0.95f);  // 버튼 배경
        public static readonly Color BG_TOPBAR = new Color(0.015f, 0.01f, 0.05f, 0.9f);  // 상단 바

        // ===== 네온 주요 색상 (BIT.jpg 기반) =====
        public static readonly Color NEON_CYAN = new Color(0f, 0.85f, 1f, 1f);           // 주 네온 시안
        public static readonly Color NEON_CYAN_BRIGHT = new Color(0.3f, 0.95f, 1f, 1f);  // 밝은 시안
        public static readonly Color NEON_MAGENTA = new Color(1f, 0.15f, 0.65f, 1f);     // 마젠타/핑크
        public static readonly Color NEON_GREEN = new Color(0.2f, 1f, 0.4f, 1f);         // 네온 그린
        public static readonly Color NEON_YELLOW = new Color(1f, 0.92f, 0.15f, 1f);      // 네온 옐로우
        public static readonly Color NEON_ORANGE = new Color(1f, 0.55f, 0f, 1f);         // 네온 오렌지
        public static readonly Color NEON_BLUE = new Color(0.15f, 0.4f, 1f, 0.9f);       // 딥 블루
        public static readonly Color NEON_PURPLE = new Color(0.6f, 0.2f, 1f, 1f);        // 네온 퍼플
        public static readonly Color NEON_PINK = new Color(1f, 0.2f, 0.6f, 1f);          // 네온 핑크

        // ===== 이퀄라이저 바 그라데이션 (하단 바 색상) =====
        public static readonly Color EQ_ORANGE = new Color(1f, 0.5f, 0f, 1f);            // 이퀄라이저 오렌지
        public static readonly Color EQ_YELLOW = new Color(1f, 0.85f, 0f, 1f);           // 이퀄라이저 옐로우
        public static readonly Color EQ_GREEN = new Color(0.4f, 1f, 0.2f, 1f);           // 이퀄라이저 그린

        // ===== 텍스트 =====
        public static readonly Color TEXT_WHITE = Color.white;
        public static readonly Color TEXT_CYAN = new Color(0.3f, 0.95f, 1f, 1f);
        public static readonly Color TEXT_GRAY = new Color(0.65f, 0.65f, 0.8f, 1f);
        public static readonly Color TEXT_DIM = new Color(0.45f, 0.45f, 0.6f, 1f);

        // ===== 테두리 (글로우 느낌) =====
        public static readonly Color BORDER_CYAN = new Color(0f, 0.85f, 1f, 0.5f);
        public static readonly Color BORDER_MAGENTA = new Color(1f, 0.15f, 0.65f, 0.4f);
        public static readonly Color BORDER_PURPLE = new Color(0.6f, 0.2f, 1f, 0.4f);
        public static readonly Color BORDER_ORANGE = new Color(1f, 0.55f, 0f, 0.5f);

        // ===== 상태 색상 =====
        public static readonly Color STATE_SELECTED = new Color(0f, 0.35f, 0.6f, 0.9f);
        public static readonly Color STATE_HOVER = new Color(0.08f, 0.06f, 0.22f, 0.95f);
        public static readonly Color STATE_PRESSED = new Color(0.2f, 0f, 0.5f, 1f);
        public static readonly Color STATE_DISABLED = new Color(0.15f, 0.15f, 0.25f, 0.5f);

        // ===== 슬라이더 =====
        public static readonly Color SLIDER_BG = new Color(0.06f, 0.04f, 0.15f, 0.9f);
        public static readonly Color SLIDER_FILL = NEON_CYAN;
        public static readonly Color SLIDER_HANDLE = Color.white;

        // ===== 판정 색상 (게임플레이) =====
        public static readonly Color JUDGE_PERFECT = new Color(1f, 0.85f, 0.1f, 1f);    // 골드
        public static readonly Color JUDGE_GREAT = NEON_CYAN;                             // 시안
        public static readonly Color JUDGE_GOOD = NEON_GREEN;                             // 그린
        public static readonly Color JUDGE_BAD = NEON_MAGENTA;                            // 마젠타
        public static readonly Color JUDGE_MISS = new Color(0.35f, 0.35f, 0.45f, 1f);   // 그레이

        // ===== 콤보 색상 =====
        public static readonly Color COMBO_LOW = new Color(0.7f, 0.8f, 1f, 0.9f);        // 기본 (라이트블루)
        public static readonly Color COMBO_10 = NEON_GREEN;                                // 10+
        public static readonly Color COMBO_25 = NEON_CYAN;                                 // 25+
        public static readonly Color COMBO_50 = NEON_YELLOW;                               // 50+
        public static readonly Color COMBO_100 = NEON_MAGENTA;                             // 100+

        // ===== 특수 용도 =====
        public static readonly Color ERROR_RED = new Color(1f, 0.15f, 0.15f, 1f);
        public static readonly Color SUCCESS_GREEN = NEON_GREEN;
        public static readonly Color WARNING_YELLOW = NEON_YELLOW;

        // ===== 유틸리티 =====
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

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
