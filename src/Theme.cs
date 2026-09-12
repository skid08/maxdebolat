using System.Drawing;

namespace MaxDebloat;

/// <summary>
/// Central dark palette + fonts. Sharp, dense, utilitarian. No gradients, no rounding by default.
/// </summary>
internal static class Theme
{
    // Base surfaces
    public static readonly Color Bg        = Color.FromArgb(0x0B, 0x0D, 0x10); // near-black window
    public static readonly Color Panel     = Color.FromArgb(0x12, 0x15, 0x1A); // side panels
    public static readonly Color Panel2    = Color.FromArgb(0x17, 0x1B, 0x21); // rows / cards
    public static readonly Color Panel3    = Color.FromArgb(0x1E, 0x23, 0x2B); // hover
    public static readonly Color Border    = Color.FromArgb(0x2A, 0x30, 0x3A);
    public static readonly Color BorderDim = Color.FromArgb(0x1D, 0x22, 0x2A);

    // Text
    public static readonly Color Text      = Color.FromArgb(0xDD, 0xE2, 0xE8);
    public static readonly Color TextDim   = Color.FromArgb(0x82, 0x8B, 0x98);
    public static readonly Color TextFaint = Color.FromArgb(0x55, 0x5D, 0x69);

    // Accents
    public static readonly Color Danger    = Color.FromArgb(0xE0, 0x37, 0x37); // nuke / defender red
    public static readonly Color DangerDim = Color.FromArgb(0x8A, 0x22, 0x22);
    public static readonly Color Accent    = Color.FromArgb(0x35, 0xB5, 0xE8); // cyan highlight
    public static readonly Color On        = Color.FromArgb(0x3F, 0xD1, 0x7A); // toggle on (green)
    public static readonly Color OnDim     = Color.FromArgb(0x25, 0x6E, 0x45);
    public static readonly Color Warn      = Color.FromArgb(0xE8, 0xB4, 0x39);

    // Log colors
    public static readonly Color LogInfo   = Color.FromArgb(0xB9, 0xC1, 0xCC);
    public static readonly Color LogOk     = Color.FromArgb(0x5A, 0xD6, 0x8C);
    public static readonly Color LogWarn   = Color.FromArgb(0xE8, 0xB4, 0x39);
    public static readonly Color LogErr    = Color.FromArgb(0xF0, 0x5A, 0x5A);
    public static readonly Color LogCmd    = Color.FromArgb(0x5E, 0xA9, 0xD6);

    // Fonts
    public static Font UI       { get; } = new("Segoe UI", 9f, FontStyle.Regular);
    public static Font UIBold   { get; } = new("Segoe UI", 9f, FontStyle.Bold);
    public static Font UISmall  { get; } = new("Segoe UI", 8f, FontStyle.Regular);
    public static Font Title    { get; } = new("Segoe UI Semibold", 11f, FontStyle.Bold);
    public static Font Big      { get; } = new("Segoe UI Black", 14f, FontStyle.Bold);
    public static Font Mono     { get; } = new("Consolas", 9f, FontStyle.Regular);
    public static Font Nav      { get; } = new("Segoe UI", 9.5f, FontStyle.Bold);
}
