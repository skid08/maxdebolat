using System.Drawing.Drawing2D;

namespace MaxDebloat;

/// <summary>Flat, owner-drawn on/off switch. Sharp corners, no theme dependency.</summary>
internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private bool _hover;

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(40, 20);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Set state without firing CheckedChanged (used when rehydrating from selection state).</summary>
    public void SetSilently(bool value)
    {
        _checked = value;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int h = Height - 2;
        var track = new Rectangle(0, 1, Width - 1, h);
        Color trackColor = _checked ? Theme.OnDim : Theme.Panel3;
        Color knobColor  = _checked ? Theme.On : (_hover ? Theme.TextDim : Theme.TextFaint);
        Color borderCol  = _checked ? Theme.On : Theme.Border;

        using (var path = Rounded(track, h / 2))
        {
            using var b = new SolidBrush(trackColor);
            g.FillPath(b, path);
            using var pen = new Pen(borderCol, 1f);
            g.DrawPath(pen, path);
        }

        int d = h - 6;
        int x = _checked ? Width - d - 4 : 4;
        var knob = new Rectangle(x, 1 + (h - d) / 2, d, d);
        using (var kb = new SolidBrush(knobColor))
            g.FillEllipse(kb, knob);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var p = new GraphicsPath();
        int d = radius * 2;
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 90, 180);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 180);
        p.CloseFigure();
        return p;
    }
}
