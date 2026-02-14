using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// Music Theme Color Palette (Gold / Purple / Teal)
    /// Warm, elegant tones on Deep Navy backgrounds
    /// </summary>
    public static class UIColorPalette
    {
        // ===== Backgrounds (Deep Navy) =====
        public static readonly Color BG_DEEP = new Color(0.03f, 0.05f, 0.10f, 1f);        // Deep Navy
        public static readonly Color BG_DARK = new Color(0.05f, 0.08f, 0.15f, 0.98f);     // Dark Navy
        public static readonly Color BG_CARD = new Color(0.08f, 0.10f, 0.18f, 0.90f);     // Card Background
        public static readonly Color BG_BUTTON = new Color(0.10f, 0.12f, 0.22f, 1f);      // Button Fill
        public static readonly Color BG_TOPBAR = new Color(0.03f, 0.05f, 0.10f, 0.95f);   // Top HUD

        // ===== Music Theme Accents =====
        public static readonly Color NEON_CYAN = new Color(0.0f, 0.8f, 0.82f, 1f);        // Teal
        public static readonly Color NEON_MAGENTA = new Color(0.58f, 0.29f, 0.98f, 1f);   // Purple
        public static readonly Color NEON_YELLOW = new Color(1.0f, 0.84f, 0.0f, 1f);      // Gold
        public static readonly Color NEON_GREEN = new Color(0.0f, 0.8f, 0.82f, 1f);       // Teal (alias)
        public static readonly Color NEON_BLUE = new Color(0.3f, 0.4f, 0.9f, 1f);         // Soft Blue
        public static readonly Color NEON_PURPLE = new Color(0.58f, 0.29f, 0.98f, 1f);    // Purple
        public static readonly Color NEON_GOLD = new Color(1.0f, 0.84f, 0.0f, 1f);        // #FFD600 (Score/Perfect)

        // ===== UI Elements =====
        public static readonly Color TEXT_WHITE = Color.white;
        public static readonly Color TEXT_CYAN = NEON_CYAN;
        public static readonly Color TEXT_GOLD = NEON_GOLD;
        public static readonly Color TEXT_GRAY = new Color(0.7f, 0.7f, 0.8f, 1f);

        // ===== Equalizer Gradient =====
        public static readonly Color EQ_START = NEON_PURPLE;
        public static readonly Color EQ_END = NEON_GOLD;

        // ===== Judgement Colors =====
        public static readonly Color JUDGE_PERFECT = NEON_GOLD;                             // Gold
        public static readonly Color JUDGE_GREAT = NEON_CYAN;                               // Teal
        public static readonly Color JUDGE_GOOD = new Color(0.4f, 0.8f, 0.2f, 1f);         // Warm Green
        public static readonly Color JUDGE_BAD = new Color(1f, 0.55f, 0f, 1f);              // Orange
        public static readonly Color JUDGE_MISS = new Color(0.8f, 0.1f, 0.1f, 1f);          // Red

        // ===== Additional Colors =====
        public static readonly Color NEON_ORANGE = new Color(1.0f, 0.55f, 0.0f, 1f);       // Music Orange
        public static readonly Color NEON_CYAN_BRIGHT = new Color(0.0f, 0.85f, 0.87f, 1f); // Bright Teal

        // ===== Borders =====
        public static readonly Color BORDER_CYAN = new Color(1.0f, 0.84f, 0.0f, 0.4f);     // Gold Border
        public static readonly Color BORDER_MAGENTA = new Color(0.58f, 0.29f, 0.98f, 0.5f); // Purple Border

        // ===== Button States =====
        public static readonly Color STATE_HOVER = new Color(0.15f, 0.12f, 0.25f, 1f);
        public static readonly Color STATE_DISABLED = new Color(0.08f, 0.08f, 0.15f, 0.6f);
        public static readonly Color PAUSE_BTN_BG = new Color(0.58f, 0.29f, 0.98f, 0.85f);  // Purple Pause

        // ===== Error =====
        public static readonly Color ERROR_RED = new Color(0.9f, 0.1f, 0.1f, 1f);

        // ===== Combo Colors =====
        public static readonly Color COMBO_NORMAL = NEON_GOLD;
        public static readonly Color COMBO_HIGH = NEON_GOLD;
        public static readonly Color COMBO_10 = new Color(0.0f, 0.8f, 0.82f, 1f);   // Teal
        public static readonly Color COMBO_25 = new Color(0.58f, 0.29f, 0.98f, 1f); // Purple
        public static readonly Color COMBO_50 = NEON_GOLD;                            // Gold
        public static readonly Color COMBO_100 = new Color(1f, 0.55f, 0f, 1f);       // Orange

        // ===== Equalizer Extra =====
        public static readonly Color EQ_ORANGE = new Color(1.0f, 0.55f, 0.0f, 1f);
        public static readonly Color EQ_YELLOW = new Color(1.0f, 0.84f, 0.0f, 1f);

        // ===== Text Extra =====
        public static readonly Color TEXT_DIM = new Color(0.35f, 0.35f, 0.45f, 1f);

        // ===== Stats Backgrounds (Music Theme Darkened) =====
        public static readonly Color STATS_BG_PERFECT = new Color(0.5f, 0.42f, 0.0f, 0.8f);  // Dark Gold
        public static readonly Color STATS_BG_GREAT = new Color(0.0f, 0.4f, 0.41f, 0.8f);    // Dark Teal
        public static readonly Color STATS_BG_GOOD = new Color(0.2f, 0.4f, 0.1f, 0.8f);      // Dark Green
        public static readonly Color STATS_BG_BAD = new Color(0.5f, 0.27f, 0.0f, 0.8f);      // Dark Orange
        public static readonly Color STATS_BG_MISS = new Color(0.5f, 0.0f, 0.0f, 0.8f);      // Dark Red
        
        // ===== Utility =====
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
