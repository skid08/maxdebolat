namespace MaxDebloat;

/// <summary>Single dark confirmation modal. One prompt, then it runs — no repeated nagging.</summary>
internal sealed class ConfirmDialog : Form
{
    private ConfirmDialog(string title, string body, string confirmText, bool danger)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Panel;
        Size = new Size(460, 220);
        ShowInTaskbar = false;

        var border = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel, Padding = new Padding(2) };
        Controls.Add(border);

        var head = new Label
        {
            Text = title,
            ForeColor = danger ? Theme.Danger : Theme.Text,
            Font = Theme.Title,
            AutoSize = false,
            Height = 44,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            BackColor = Theme.Panel2
        };

        var msg = new Label
        {
            Text = body,
            ForeColor = Theme.TextDim,
            Font = Theme.UI,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(18, 16, 18, 8)
        };

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Theme.Panel, Padding = new Padding(14) };

        var ok = new FlatButton
        {
            Text = confirmText,
            Dock = DockStyle.Right,
            Width = 150,
            BaseColor = danger ? Theme.DangerDim : Theme.OnDim,
            HoverColor = danger ? Theme.Danger : Theme.On,
            BorderColor = danger ? Theme.Danger : Theme.On,
            TextColor = Color.White
        };
        ok.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        var spacer = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Theme.Panel };

        var cancel = new FlatButton { Text = "CANCEL", Dock = DockStyle.Right, Width = 110 };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        bar.Controls.Add(ok);
        bar.Controls.Add(spacer);
        bar.Controls.Add(cancel);

        border.Controls.Add(msg);
        border.Controls.Add(bar);
        border.Controls.Add(head);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border, 1f);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    public static bool Ask(IWin32Window owner, string title, string body, string confirmText = "EXECUTE", bool danger = true)
    {
        using var d = new ConfirmDialog(title, body, confirmText, danger);
        return d.ShowDialog(owner) == DialogResult.OK;
    }
}
