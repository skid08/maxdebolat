using System.Drawing.Drawing2D;

namespace MaxDebloat;

/// <summary>Flat, owner-drawn button with a sharp 1px border and hover/press states.</summary>
internal sealed class FlatButton : Button
{
    private bool _hover, _down;

    public Color BaseColor { get; set; } = Theme.Panel2;
    public Color HoverColor { get; set; } = Theme.Panel3;
    public Color BorderColor { get; set; } = Theme.Border;
    public Color TextColor { get; set; } = Theme.Text;
    public bool Accent { get; set; }

    public FlatButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.UIBold;
        Cursor = Cursors.Hand;
        ForeColor = Theme.Text;
        UseVisualStyleBackColor = false;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        var r = new Rectangle(0, 0, Width - 1, Height - 1);

        Color fill = BaseColor;
        if (!Enabled) fill = Theme.Panel;
        else if (_down) fill = ControlPaint.Dark(HoverColor, 0.05f);
        else if (_hover) fill = HoverColor;

        using (var b = new SolidBrush(fill)) g.FillRectangle(b, r);
        using (var pen = new Pen(Enabled ? BorderColor : Theme.BorderDim, 1f)) g.DrawRectangle(pen, r);

        if (Accent && Enabled)
        {
            using var bar = new SolidBrush(_hover ? Theme.Accent : Theme.Border);
            g.FillRectangle(bar, new Rectangle(0, 0, 3, Height));
        }

        var textColor = Enabled ? TextColor : Theme.TextFaint;
        TextRenderer.DrawText(g, Text, Font, r, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
