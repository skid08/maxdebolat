using System.Runtime.InteropServices;
using System.Text;

namespace MaxDebloat;

internal sealed class MainForm : Form
{
    // ---- Win32 for borderless drag ----
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private readonly Runner _runner = new();
    private readonly HashSet<string> _selected = new();

    private RichTextBox _log = null!;
    private FlowLayoutPanel _content = null!;
    private Panel _navPanel = null!;
    private Label _catTitle = null!, _catBlurb = null!;
    private Label _statSvc = null!, _statProc = null!;
    private readonly List<FlatButton> _navButtons = new();
    private readonly List<Control> _lockWhileBusy = new();
    private Category _active = Category.Apps;
    private volatile bool _busy;

    public MainForm()
    {
        // Seed the "selected" set with the recommended (nuke) profile.
        foreach (var a in ActionRegistry.NukeProfile) _selected.Add(a.Id);

        _runner.Sink = (msg, lvl) => AppendLog(msg, lvl);

        Text = "MAX DEBLOAT";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        Size = new Size(1120, 760);
        MinimumSize = new Size(940, 620);
        DoubleBuffered = true;
        KeyPreview = true;

        BuildUi();
        SelectCategory(Category.Apps);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!Runner.IsAdmin())
            AppendLog("! NOT running as administrator - actions will fail. Relaunch elevated.", LogLevel.Err);
        else
            AppendLog("Ready. Elevated. This is one-way: there is no undo. VM only.", LogLevel.Ok);
        RefreshStatsAsync();
    }

    // =================================================================== UI BUILD

    private void BuildUi()
    {
        // outer border container
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(1) };
        Controls.Add(outer);

        // --- title bar ---
        var belowTitle = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        var titleBar = BuildTitleBar();
        outer.Controls.Add(belowTitle);   // Fill first
        outer.Controls.Add(titleBar);     // edge last

        // --- command bar ---
        var belowCmd = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        var cmdBar = BuildCommandBar();
        belowTitle.Controls.Add(belowCmd);
        belowTitle.Controls.Add(cmdBar);

        // --- log at bottom ---
        var mid = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        var logPanel = BuildLogPanel();
        belowCmd.Controls.Add(mid);
        belowCmd.Controls.Add(logPanel);

        // --- nav + content ---
        var content = BuildContentArea();
        _navPanel = BuildNav();
        mid.Controls.Add(content);        // Fill first
        mid.Controls.Add(_navPanel);      // edge last
    }

    private Panel BuildTitleBar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Panel2 };
        bar.MouseDown += DragMove;

        var title = new Label
        {
            Text = "☠  MAX DEBLOAT",
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 260,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            BackColor = Theme.Panel2
        };
        title.MouseDown += DragMove;

        var sub = new Label
        {
            Text = "WINDOWS 11 GAMING STRIPPER  •  ONE-WAY  •  VM ONLY",
            ForeColor = Theme.TextFaint,
            Font = Theme.UISmall,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Theme.Panel2
        };
        sub.MouseDown += DragMove;

        var close = new FlatButton { Text = "✕", Dock = DockStyle.Right, Width = 46, BaseColor = Theme.Panel2, HoverColor = Theme.Danger, BorderColor = Theme.Panel2, Font = Theme.UIBold };
        close.Click += (_, _) => Close();
        var min = new FlatButton { Text = "–", Dock = DockStyle.Right, Width = 46, BaseColor = Theme.Panel2, HoverColor = Theme.Panel3, BorderColor = Theme.Panel2, Font = Theme.UIBold };
        min.Click += (_, _) => WindowState = FormWindowState.Minimized;

        bar.Controls.Add(sub);
        bar.Controls.Add(title);
        bar.Controls.Add(min);
        bar.Controls.Add(close);
        return bar;
    }

    private Panel BuildCommandBar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.Panel, Padding = new Padding(14, 12, 14, 12) };

        // Big NUKE button (left)
        var nuke = new FlatButton
        {
            Text = "☠  ONE-CLICK NUKE",
            Dock = DockStyle.Left,
            Width = 320,
            BaseColor = Theme.DangerDim,
            HoverColor = Theme.Danger,
            BorderColor = Theme.Danger,
            TextColor = Color.White,
            Font = new Font("Segoe UI Black", 15f, FontStyle.Bold)
        };
        nuke.Click += (_, _) => OnNuke();

        // right-side stack
        var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel };

        var applySel = new FlatButton { Text = "APPLY SELECTED", Width = 150, Height = 32, Left = 0, Top = 0, BaseColor = Theme.Panel2, HoverColor = Theme.Panel3, BorderColor = Theme.Border, Accent = true };
        applySel.Click += (_, _) => OnApplySelected();

        var selAllCat = new FlatButton { Text = "SELECT CATEGORY", Width = 150, Height = 32, Left = 158, Top = 0, BaseColor = Theme.Panel2, HoverColor = Theme.Panel3, BorderColor = Theme.Border };
        selAllCat.Click += (_, _) => SetCategorySelection(true);

        var clearCat = new FlatButton { Text = "CLEAR CATEGORY", Width = 150, Height = 32, Left = 316, Top = 0, BaseColor = Theme.Panel2, HoverColor = Theme.Panel3, BorderColor = Theme.Border };
        clearCat.Click += (_, _) => SetCategorySelection(false);

        // stats
        _statSvc = new Label { AutoSize = false, Width = 220, Height = 20, Left = 0, Top = 42, ForeColor = Theme.TextDim, Font = Theme.UIBold, TextAlign = ContentAlignment.MiddleLeft, Text = "RUNNING SERVICES:  …" };
        _statProc = new Label { AutoSize = false, Width = 220, Height = 20, Left = 0, Top = 62, ForeColor = Theme.TextDim, Font = Theme.UIBold, TextAlign = ContentAlignment.MiddleLeft, Text = "PROCESSES:  …" };

        var refresh = new FlatButton { Text = "REFRESH SNAPSHOT", Width = 150, Height = 26, Left = 236, Top = 50, BaseColor = Theme.Panel2, HoverColor = Theme.Panel3, BorderColor = Theme.Border, Font = Theme.UISmall };
        refresh.Click += (_, _) => RefreshStatsAsync();

        right.Controls.AddRange(new Control[] { applySel, selAllCat, clearCat, _statSvc, _statProc, refresh });

        bar.Controls.Add(right);
        bar.Controls.Add(nuke);

        _lockWhileBusy.AddRange(new Control[] { nuke, applySel, selAllCat, clearCat });
        return bar;
    }

    private Panel BuildNav()
    {
        var nav = new Panel { Dock = DockStyle.Left, Width = 194, BackColor = Theme.Panel };
        var pad = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Panel, Padding = new Padding(8, 8, 8, 8) };

        // add buttons bottom-up so first category ends on top
        foreach (var c in ((Category[])CategoryInfo.Order.Clone()))
        {
            var cat = c;
            int count = ActionRegistry.InCategory(cat).Count();
            var btn = new FlatButton
            {
                Text = $"  {CategoryInfo.Label(cat)}   ({count})",
                Dock = DockStyle.Top,
                Height = 42,
                Margin = new Padding(0),
                BaseColor = Theme.Panel,
                HoverColor = Theme.Panel2,
                BorderColor = Theme.Panel,
                TextColor = Theme.TextDim,
                Font = Theme.Nav,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Tag = cat;
            btn.Click += (_, _) => SelectCategory(cat);
            _navButtons.Add(btn);
        }
        // dock order: reverse so index 0 sits at top
        for (int i = _navButtons.Count - 1; i >= 0; i--)
        {
            _navButtons[i].Dock = DockStyle.Top;
            pad.Controls.Add(_navButtons[i]);
        }

        var footer = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            Text = "v1.0  ·  no backups\nno undo",
            ForeColor = Theme.TextFaint,
            Font = Theme.UISmall,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Theme.Panel
        };

        nav.Controls.Add(pad);
        nav.Controls.Add(footer);
        return nav;
    }

    private Panel BuildContentArea()
    {
        var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };

        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.Bg, Padding = new Padding(16, 8, 16, 0) };
        _catTitle = new Label { Dock = DockStyle.Top, Height = 26, ForeColor = Theme.Text, Font = new Font("Segoe UI", 13f, FontStyle.Bold), Text = "APPS" };
        _catBlurb = new Label { Dock = DockStyle.Top, Height = 20, ForeColor = Theme.TextDim, Font = Theme.UISmall, Text = "" };
        header.Controls.Add(_catBlurb);
        header.Controls.Add(_catTitle);

        _content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Bg,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12, 4, 8, 12)
        };
        _content.Resize += (_, _) => ResizeRows();

        wrap.Controls.Add(_content);
        wrap.Controls.Add(header);
        return wrap;
    }

    private Panel BuildLogPanel()
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 196, BackColor = Theme.Panel, Padding = new Padding(1) };

        var head = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Theme.Panel2 };
        var lbl = new Label { Dock = DockStyle.Left, Width = 200, Text = "  OUTPUT LOG", ForeColor = Theme.TextDim, Font = Theme.UIBold, TextAlign = ContentAlignment.MiddleLeft, BackColor = Theme.Panel2 };
        var clear = new FlatButton { Text = "CLEAR", Dock = DockStyle.Right, Width = 70, Height = 26, BaseColor = Theme.Panel2, HoverColor = Theme.Panel3, BorderColor = Theme.Panel2, Font = Theme.UISmall };
        clear.Click += (_, _) => _log.Clear();
        head.Controls.Add(lbl);
        head.Controls.Add(clear);

        _log = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(0x08, 0x0A, 0x0D),
            ForeColor = Theme.LogInfo,
            Font = Theme.Mono,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            DetectUrls = false
        };

        panel.Controls.Add(_log);
        panel.Controls.Add(head);
        return panel;
    }

    // =================================================================== ROWS

    private void SelectCategory(Category c)
    {
        _active = c;
        _catTitle.Text = CategoryInfo.Label(c);
        _catBlurb.Text = CategoryInfo.Blurb(c);

        foreach (var b in _navButtons)
        {
            bool on = (Category)b.Tag! == c;
            b.TextColor = on ? Theme.Text : Theme.TextDim;
            b.BaseColor = on ? Theme.Panel2 : Theme.Panel;
            b.BorderColor = on ? Theme.Border : Theme.Panel;
            b.Accent = on;
            b.Invalidate();
        }

        _content.SuspendLayout();
        _content.Controls.Clear();
        foreach (var a in ActionRegistry.InCategory(c))
            _content.Controls.Add(BuildRow(a));
        _content.ResumeLayout();
        ResizeRows();
    }

    private Control BuildRow(TweakAction action)
    {
        var row = new Panel { Height = 58, BackColor = Theme.Panel2, Margin = new Padding(0, 0, 0, 6) };

        var toggle = new ToggleSwitch { Left = 14, Top = 19 };
        toggle.SetSilently(_selected.Contains(action.Id));
        toggle.CheckedChanged += (_, _) =>
        {
            if (toggle.Checked) _selected.Add(action.Id);
            else _selected.Remove(action.Id);
        };

        var name = new Label
        {
            Text = action.Name,
            ForeColor = Theme.Text,
            Font = Theme.UIBold,
            AutoSize = false,
            Left = 66, Top = 8, Width = 520, Height = 18,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Theme.Panel2
        };
        var desc = new Label
        {
            Text = action.Desc,
            ForeColor = Theme.TextDim,
            Font = Theme.UISmall,
            AutoSize = false,
            Left = 66, Top = 28, Width = 620, Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Theme.Panel2
        };

        var runBtn = new FlatButton
        {
            Text = "RUN",
            Width = 68, Height = 30,
            Top = 14,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BaseColor = Theme.Panel3,
            HoverColor = Theme.Accent,
            BorderColor = Theme.Border,
            Font = Theme.UIBold
        };
        runBtn.Click += (_, _) => OnRunSingle(action);

        var tag = new Label
        {
            Text = action.InNuke ? "NUKE" : "OPT",
            ForeColor = action.InNuke ? Theme.Danger : Theme.TextFaint,
            Font = new Font("Consolas", 7.5f, FontStyle.Bold),
            AutoSize = false,
            Width = 42, Height = 16, Top = 22,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Theme.Panel2
        };

        row.Controls.Add(name);
        row.Controls.Add(desc);
        row.Controls.Add(toggle);
        row.Controls.Add(runBtn);
        row.Controls.Add(tag);

        row.Resize += (_, _) =>
        {
            runBtn.Left = row.Width - runBtn.Width - 14;
            tag.Left = row.Width - runBtn.Width - 14 - tag.Width - 10;
            name.Width = tag.Left - name.Left - 8;
            desc.Width = tag.Left - desc.Left - 8;
        };
        return row;
    }

    private void ResizeRows()
    {
        int w = _content.ClientSize.Width - _content.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
        if (w < 200) w = 200;
        foreach (Control c in _content.Controls)
            c.Width = w;
    }

    // =================================================================== ACTIONS

    private void SetCategorySelection(bool on)
    {
        foreach (Control row in _content.Controls)
            foreach (Control child in row.Controls)
                if (child is ToggleSwitch t) t.Checked = on; // fires CheckedChanged -> updates _selected
    }

    private void OnNuke()
    {
        if (_busy) { AppendLog("Busy — wait for the current run to finish.", LogLevel.Warn); return; }
        int n = ActionRegistry.NukeProfile.Count();
        bool ok = ConfirmDialog.Ask(this, "☠  ONE-CLICK NUKE",
            $"This applies the full extreme profile: {n} actions across every category.\n\n" +
            "Windows Defender, telemetry, bloatware, and dozens of services will be removed or disabled.\n\n" +
            "There is NO backup and NO undo. Continue?",
            "NUKE IT", danger: true);
        if (!ok) return;
        RunBatch(ActionRegistry.NukeProfile.ToList(), "ONE-CLICK NUKE");
    }

    private void OnApplySelected()
    {
        if (_busy) { AppendLog("Busy — wait for the current run to finish.", LogLevel.Warn); return; }
        var list = ActionRegistry.All.Where(a => _selected.Contains(a.Id)).ToList();
        if (list.Count == 0) { AppendLog("Nothing selected.", LogLevel.Warn); return; }
        bool ok = ConfirmDialog.Ask(this, "APPLY SELECTED",
            $"Apply {list.Count} selected action(s)?\n\nThere is no undo.", "APPLY", danger: true);
        if (!ok) return;
        RunBatch(list, "APPLY SELECTED");
    }

    private void OnRunSingle(TweakAction action)
    {
        if (_busy) { AppendLog("Busy — wait for the current run to finish.", LogLevel.Warn); return; }
        RunBatch(new List<TweakAction> { action }, "RUN " + action.Name);
    }

    private void RunBatch(List<TweakAction> actions, string label)
    {
        _busy = true;
        SetBusyUi(true);
        AppendLog("", LogLevel.Info);
        AppendLog($"==== {label}  ({actions.Count} action{(actions.Count == 1 ? "" : "s")}) ====", LogLevel.Cmd);

        Task.Run(() =>
        {
            int done = 0, failed = 0;
            foreach (var a in actions)
            {
                AppendLog($"▶ {CategoryInfo.Label(a.Category)} / {a.Name}", LogLevel.Info);
                try { a.Run(_runner); done++; }
                catch (Exception ex) { failed++; AppendLog("  ! " + ex.Message, LogLevel.Err); }
            }
            AppendLog($"==== DONE: {done} ok, {failed} failed ====", failed == 0 ? LogLevel.Ok : LogLevel.Warn);

            UiPost(() =>
            {
                _busy = false;
                SetBusyUi(false);
                RefreshStatsAsync();
            });
        });
    }

    private void SetBusyUi(bool busy)
    {
        UiPost(() =>
        {
            foreach (var c in _lockWhileBusy) c.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        });
    }

    /// <summary>Marshal an action onto the UI thread; no-op if the handle is gone.</summary>
    private void UiPost(Action a)
    {
        try
        {
            if (IsDisposed || !IsHandleCreated) { if (!InvokeRequired) a(); return; }
            if (InvokeRequired) BeginInvoke(a); else a();
        }
        catch { }
    }

    // =================================================================== STATS

    private void RefreshStatsAsync()
    {
        Task.Run(() =>
        {
            const string script = "$s=(Get-Service|Where-Object{$_.Status -eq 'Running'}).Count;$p=(Get-Process).Count;\"$s|$p\"";
            string enc = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            string outp = _runner.Capture("powershell.exe", "-NoProfile -NonInteractive -EncodedCommand " + enc);
            int svc = -1, proc = -1;
            var parts = outp.Trim().Split('|');
            if (parts.Length == 2) { int.TryParse(parts[0].Trim(), out svc); int.TryParse(parts[1].Trim(), out proc); }

            UiPost(() =>
            {
                _statSvc.Text = "RUNNING SERVICES:  " + (svc >= 0 ? svc.ToString() : "?");
                _statSvc.ForeColor = svc >= 0 && svc < 30 ? Theme.On : (svc >= 0 ? Theme.Warn : Theme.TextDim);
                _statProc.Text = "PROCESSES:  " + (proc >= 0 ? proc.ToString() : "?");
            });
        });
    }

    // =================================================================== LOG

    private void AppendLog(string msg, LogLevel lvl)
    {
        if (_log.IsDisposed) return;
        void write()
        {
            if (_log.TextLength > 200_000) _log.Clear();
            Color col = lvl switch
            {
                LogLevel.Ok => Theme.LogOk,
                LogLevel.Warn => Theme.LogWarn,
                LogLevel.Err => Theme.LogErr,
                LogLevel.Cmd => Theme.LogCmd,
                _ => Theme.LogInfo
            };
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = col;
            _log.AppendText(msg + "\n");
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
        UiPost(write);
    }

    // =================================================================== WINDOW CHROME

    private void DragMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border, 1f);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTCLIENT = 1, HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
                  HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if ((int)m.Result == HTCLIENT)
            {
                var p = PointToClient(Cursor.Position);
                const int g = 6;
                bool l = p.X <= g, r = p.X >= ClientSize.Width - g, t = p.Y <= g, b = p.Y >= ClientSize.Height - g;
                if (t && l) m.Result = (IntPtr)HTTOPLEFT;
                else if (t && r) m.Result = (IntPtr)HTTOPRIGHT;
                else if (b && l) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (b && r) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (l) m.Result = (IntPtr)HTLEFT;
                else if (r) m.Result = (IntPtr)HTRIGHT;
                else if (t) m.Result = (IntPtr)HTTOP;
                else if (b) m.Result = (IntPtr)HTBOTTOM;
            }
            return;
        }
        base.WndProc(ref m);
    }
}
