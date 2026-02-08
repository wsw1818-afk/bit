using UnityEngine;

namespace AIBeat.UI
{
    /// <summary>
    /// Bitmania Style Color Palette (Neon Cyberpunk)
    /// High Contrast, Vivid Neons on Dark Backgrounds
    /// </summary>
    public static class UIColorPalette
    {
        // ===== Backgrounds (Deep Space / Cyberpunk City) =====
        public static readonly Color BG_DEEP = new Color(0.02f, 0.01f, 0.05f, 1f);       // Almost Black Purple
        public static readonly Color BG_DARK = new Color(0.05f, 0.03f, 0.10f, 0.98f);    // Dark Violet
        public static readonly Color BG_CARD = new Color(0.08f, 0.05f, 0.15f, 0.90f);    // Card Background
        public static readonly Color BG_BUTTON = new Color(0.10f, 0.08f, 0.20f, 1f);     // Button Fill
        public static readonly Color BG_TOPBAR = new Color(0.02f, 0.01f, 0.05f, 0.95f);  // Top HUD

        // ===== Neon Accents =====
        public static readonly Color NEON_CYAN = new Color(0.0f, 1.0f, 1.0f, 1f);        // #00FFFF
        public static readonly Color NEON_MAGENTA = new Color(1.0f, 0.0f, 0.8f, 1f);     // #FF00CC
        public static readonly Color NEON_YELLOW = new Color(1.0f, 0.9f, 0.0f, 1f);      // #FFE600
        public static readonly Color NEON_GREEN = new Color(0.0f, 1.0f, 0.4f, 1f);       // #00FF66
        public static readonly Color NEON_BLUE = new Color(0.0f, 0.4f, 1.0f, 1f);        // #0066FF
        public static readonly Color NEON_PURPLE = new Color(0.7f, 0.0f, 1.0f, 1f);      // #B200FF
        public static readonly Color NEON_GOLD = new Color(1.0f, 0.8f, 0.0f, 1f);        // #FFCC00 (Score/Perfect)

        // ===== UI Elements =====
        public static readonly Color TEXT_WHITE = Color.white;
        public static readonly Color TEXT_CYAN = NEON_CYAN;
        public static readonly Color TEXT_GOLD = NEON_GOLD;
        public static readonly Color TEXT_GRAY = new Color(0.7f, 0.7f, 0.8f, 1f);

        // ===== Equalizer Gradient =====
        public static readonly Color EQ_START = NEON_MAGENTA;
        public static readonly Color EQ_END = NEON_CYAN;

        // ===== Judgement Colors =====
        public static readonly Color JUDGE_PERFECT = NEON_GOLD;   // Gold
        public static readonly Color JUDGE_GREAT = NEON_CYAN;     // Cyan
        public static readonly Color JUDGE_GOOD = NEON_GREEN;     // Green
        public static readonly Color JUDGE_BAD = NEON_MAGENTA;    // Magenta
        public static readonly Color JUDGE_MISS = new Color(0.8f, 0.1f, 0.1f, 1f); // Red

        // ===== Combo Colors =====
        public static readonly Color COMBO_NORMAL = NEON_CYAN;
        public static readonly Color COMBO_HIGH = NEON_GOLD;

        // ===== Stats Backgrounds (Darkened Versions) =====
        public static readonly Color STATS_BG_PERFECT = new Color(0.5f, 0.4f, 0.0f, 0.8f);
        public static readonly Color STATS_BG_GREAT = new Color(0.0f, 0.5f, 0.5f, 0.8f);
        public static readonly Color STATS_BG_GOOD = new Color(0.0f, 0.5f, 0.2f, 0.8f);
        public static readonly Color STATS_BG_BAD = new Color(0.5f, 0.0f, 0.4f, 0.8f);
        public static readonly Color STATS_BG_MISS = new Color(0.5f, 0.0f, 0.0f, 0.8f);
        
        // ===== Utility =====
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
