using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MxHmi
{
    public class WindowHostForm : Form
    {
        private const string AppVersion = "0.2.1";
        private const int ToggleTargetHotKeyId = 1001;
        private const int ShowHideHotKeyId = 1002;
        private const int ClearAllHotKeyId = 1003;

        private static readonly Color ShellBack = Color.FromArgb(8, 14, 22);
        private static readonly Color PanelBack = Color.FromArgb(14, 24, 36);
        private static readonly Color HeaderBack = Color.FromArgb(18, 34, 48);
        private static readonly Color RowBack = Color.FromArgb(12, 21, 31);
        private static readonly Color RowAltBack = Color.FromArgb(15, 28, 40);
        private static readonly Color SelectedBack = Color.FromArgb(22, 91, 120);
        private static readonly Color Accent = Color.FromArgb(0, 210, 255);
        private static readonly Color TextMain = Color.FromArgb(226, 241, 247);
        private static readonly Color TextMuted = Color.FromArgb(132, 156, 170);

        private readonly bool startInTray;
        private readonly ListView windowListView;
        private readonly ImageList windowIcons;
        private readonly Button toggleButton;
        private readonly Button clearAllButton;
        private readonly Button settingsButton;
        private readonly Label statusLabel;
        private readonly Timer refreshTimer;
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;

        private AppSettings settings;
        private bool exiting;
        private bool toggleTargetHotKeyRegistered;
        private bool showHideHotKeyRegistered;
        private bool clearAllHotKeyRegistered;

        public WindowHostForm(bool startInTray)
        {
            this.startInTray = startInTray;
            settings = AppSettings.Load();

            Text = "MX HMI TopMost v" + AppVersion;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(880, 430);
            MinimumSize = new Size(660, 320);
            ShowInTaskbar = true;
            TopMost = true;
            KeyPreview = true;
            BackColor = ShellBack;
            ForeColor = TextMain;

            Icon appIcon = LoadAppIcon();
            Icon = appIcon;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(10);
            root.BackColor = ShellBack;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            Controls.Add(root);

            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Fill;
            topBar.BackColor = ShellBack;
            root.Controls.Add(topBar, 0, 0);

            Label titleLabel = new Label();
            titleLabel.Dock = DockStyle.Left;
            titleLabel.Width = 245;
            titleLabel.Text = "MX HMI  TOPMOST";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Accent;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            topBar.Controls.Add(titleLabel);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Right;
            buttons.Width = 330;
            buttons.WrapContents = false;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.BackColor = ShellBack;
            topBar.Controls.Add(buttons);

            toggleButton = CreateButton("置顶", OnToggleClick, 96);
            clearAllButton = CreateButton("取消全部", OnClearAllClick, 96);
            settingsButton = CreateButton("设置", OnSettingsClick, 82);
            buttons.Controls.Add(toggleButton);
            buttons.Controls.Add(clearAllButton);
            buttons.Controls.Add(settingsButton);

            Label hintLabel = new Label();
            hintLabel.Dock = DockStyle.Fill;
            hintLabel.Text = "双击列表切换置顶，关闭窗口会进入托盘";
            hintLabel.ForeColor = TextMuted;
            hintLabel.TextAlign = ContentAlignment.MiddleLeft;
            topBar.Controls.Add(hintLabel);
            hintLabel.BringToFront();

            windowIcons = new ImageList();
            windowIcons.ColorDepth = ColorDepth.Depth32Bit;
            windowIcons.ImageSize = new Size(16, 16);

            windowListView = new ListView();
            windowListView.Dock = DockStyle.Fill;
            windowListView.View = View.Details;
            windowListView.FullRowSelect = true;
            windowListView.MultiSelect = false;
            windowListView.HideSelection = false;
            windowListView.SmallImageList = windowIcons;
            windowListView.BackColor = PanelBack;
            windowListView.ForeColor = TextMain;
            windowListView.BorderStyle = BorderStyle.FixedSingle;
            windowListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            windowListView.OwnerDraw = true;
            windowListView.Columns.Add("应用", 150);
            windowListView.Columns.Add("状态", 76);
            windowListView.Columns.Add("窗口标题", 560);
            windowListView.SelectedIndexChanged += delegate { SetToggleButtonState(); };
            windowListView.DoubleClick += OnToggleClick;
            windowListView.DrawColumnHeader += OnDrawColumnHeader;
            windowListView.DrawSubItem += OnDrawSubItem;
            root.Controls.Add(windowListView, 0, 1);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Text = GetHotKeyStatusText();
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.AutoEllipsis = true;
            statusLabel.ForeColor = TextMuted;
            statusLabel.BackColor = ShellBack;
            statusLabel.Font = new Font("Segoe UI", 9F);
            root.Controls.Add(statusLabel, 0, 2);

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示", null, delegate { ShowFromTray(); });
            trayMenu.Items.Add("取消全部置顶", null, delegate { ClearAllTopMost(); });
            trayMenu.Items.Add("设置", null, delegate { ShowSettingsDialog(); });
            trayMenu.Items.Add("退出", null, delegate { ExitApplication(); });

            trayIcon = new NotifyIcon();
            trayIcon.Icon = appIcon;
            trayIcon.Text = "MX HMI TopMost";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };

            refreshTimer = new Timer();
            refreshTimer.Interval = 1800;
            refreshTimer.Tick += delegate
            {
                if (Visible)
                {
                    RefreshWindowList();
                }
            };

            Shown += OnShown;
            Resize += delegate { ResizeColumns(); };
            KeyDown += OnKeyDown;
            FormClosing += OnFormClosing;
            FormClosed += OnFormClosed;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterGlobalHotKeys();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterGlobalHotKeys();
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == ToggleTargetHotKeyId)
                {
                    ToggleWindowUnderCursor();
                    return;
                }
                if (id == ShowHideHotKeyId)
                {
                    ToggleToolVisibility();
                    return;
                }
                if (id == ClearAllHotKeyId)
                {
                    ClearAllTopMost();
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private void OnShown(object sender, EventArgs e)
        {
            RefreshWindowList();
            refreshTimer.Start();

            if (startInTray)
            {
                Hide();
                ShowInTaskbar = false;
            }
        }

        private Icon LoadAppIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    return icon;
                }
            }
            catch
            {
            }

            return SystemIcons.Application;
        }

        private Button CreateButton(string text, EventHandler clickHandler, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = width;
            button.Height = 34;
            button.Margin = new Padding(8, 8, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Accent;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(23, 73, 92);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 120, 150);
            button.BackColor = Color.FromArgb(12, 24, 34);
            button.ForeColor = TextMain;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.Click += clickHandler;
            return button;
        }

        private void RegisterGlobalHotKeys()
        {
            UnregisterGlobalHotKeys();

            if (Handle == IntPtr.Zero)
            {
                return;
            }

            toggleTargetHotKeyRegistered = RegisterHotKey(ToggleTargetHotKeyId, settings.ToggleTargetHotKey);
            showHideHotKeyRegistered = RegisterHotKey(ShowHideHotKeyId, settings.ShowHideHotKey);
            clearAllHotKeyRegistered = RegisterHotKey(ClearAllHotKeyId, settings.ClearAllHotKey);

            if (!toggleTargetHotKeyRegistered || !showHideHotKeyRegistered || !clearAllHotKeyRegistered)
            {
                SetStatus("部分快捷键注册失败，可能被其它程序占用。");
            }
        }

        private bool RegisterHotKey(int id, HotKeySetting hotKey)
        {
            if (!hotKey.IsValid)
            {
                return false;
            }

            return NativeMethods.RegisterHotKey(Handle, id, hotKey.Modifiers, (uint)hotKey.Key);
        }

        private void UnregisterGlobalHotKeys()
        {
            if (Handle == IntPtr.Zero)
            {
                return;
            }

            if (toggleTargetHotKeyRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, ToggleTargetHotKeyId);
                toggleTargetHotKeyRegistered = false;
            }
            if (showHideHotKeyRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, ShowHideHotKeyId);
                showHideHotKeyRegistered = false;
            }
            if (clearAllHotKeyRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, ClearAllHotKeyId);
                clearAllHotKeyRegistered = false;
            }
        }

        private void OnToggleClick(object sender, EventArgs e)
        {
            WindowItem item = GetSelectedWindow();
            if (item == null)
            {
                SetStatus("请选择一个窗口");
                return;
            }

            ToggleTopMost(item.Handle, item.Title, item.IsTopMost);
        }

        private void OnClearAllClick(object sender, EventArgs e)
        {
            ClearAllTopMost();
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            ShowSettingsDialog();
        }

        private void ShowSettingsDialog()
        {
            ShowFromTray();

            using (SettingsForm form = new SettingsForm(settings))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                settings = form.Settings;
                settings.Save();
                RegisterGlobalHotKeys();
                SetStatus("设置已保存。" + GetHotKeyStatusText());
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                RefreshWindowList();
                e.Handled = true;
            }
        }

        private void ToggleWindowUnderCursor()
        {
            NativeMethods.POINT point;
            if (!NativeMethods.GetCursorPos(out point))
            {
                SetStatus("无法读取鼠标位置");
                return;
            }

            IntPtr hWnd = NativeMethods.WindowFromPoint(point);
            if (hWnd == IntPtr.Zero)
            {
                SetStatus("鼠标下没有可用窗口");
                return;
            }

            hWnd = NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT);
            uint ownProcessId = (uint)Process.GetCurrentProcess().Id;
            if (!IsCandidateWindow(hWnd, ownProcessId, NativeMethods.GetShellWindow()))
            {
                SetStatus("鼠标指向的窗口不可置顶");
                return;
            }

            WindowItem item = CreateWindowItem(hWnd);
            ToggleTopMost(item.Handle, item.Title, item.IsTopMost);
        }

        private void ToggleTopMost(IntPtr hWnd, string title, bool currentTopMost)
        {
            bool nextTopMost = !currentTopMost;
            SetTargetTopMost(hWnd, nextTopMost);
            SetStatus((nextTopMost ? "已置顶：" : "已取消置顶：") + title);
            RefreshWindowList(hWnd);
        }

        private void ClearAllTopMost()
        {
            uint ownProcessId = (uint)Process.GetCurrentProcess().Id;
            IntPtr shellWindow = NativeMethods.GetShellWindow();
            int changed = 0;

            NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsCandidateWindow(hWnd, ownProcessId, shellWindow))
                {
                    return true;
                }

                bool isTopMost = (NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE).ToInt64() & NativeMethods.WS_EX_TOPMOST) == NativeMethods.WS_EX_TOPMOST;
                if (isTopMost)
                {
                    SetTargetTopMost(hWnd, false);
                    changed++;
                }

                return true;
            }, IntPtr.Zero);

            SetStatus("已取消 " + changed + " 个置顶窗口");
            RefreshWindowList();
        }

        private void RefreshWindowList()
        {
            WindowItem selected = GetSelectedWindow();
            RefreshWindowList(selected == null ? IntPtr.Zero : selected.Handle);
        }

        private void RefreshWindowList(IntPtr selectedHandle)
        {
            windowListView.BeginUpdate();
            windowListView.Items.Clear();
            windowIcons.Images.Clear();

            uint ownProcessId = (uint)Process.GetCurrentProcess().Id;
            IntPtr shellWindow = NativeMethods.GetShellWindow();
            int count = 0;

            NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsCandidateWindow(hWnd, ownProcessId, shellWindow))
                {
                    return true;
                }

                WindowItem item = CreateWindowItem(hWnd);
                windowIcons.Images.Add(GetWindowIcon(hWnd));

                ListViewItem row = new ListViewItem(item.ProcessName, count);
                row.Tag = item;
                row.SubItems.Add(item.IsTopMost ? "已置顶" : "");
                row.SubItems.Add(item.Title);
                windowListView.Items.Add(row);

                if (item.Handle == selectedHandle)
                {
                    row.Selected = true;
                    row.Focused = true;
                }

                count++;
                return true;
            }, IntPtr.Zero);

            if (windowListView.SelectedItems.Count == 0 && windowListView.Items.Count > 0)
            {
                windowListView.Items[0].Selected = true;
                windowListView.Items[0].Focused = true;
            }

            windowListView.EndUpdate();
            ResizeColumns();
            SetToggleButtonState();

            if (windowListView.Items.Count == 0)
            {
                SetStatus("未找到可置顶窗口");
            }
            else
            {
                SetStatus("窗口数：" + windowListView.Items.Count + "。双击或按钮切换置顶。");
            }
        }

        private bool IsCandidateWindow(IntPtr hWnd, uint ownProcessId, IntPtr shellWindow)
        {
            if (hWnd == IntPtr.Zero || hWnd == Handle || hWnd == shellWindow)
            {
                return false;
            }

            if (!NativeMethods.IsWindow(hWnd) || !NativeMethods.IsWindowVisible(hWnd))
            {
                return false;
            }

            uint processId;
            NativeMethods.GetWindowThreadProcessId(hWnd, out processId);
            if (processId == ownProcessId)
            {
                return false;
            }

            string title = NativeMethods.GetWindowText(hWnd);
            if (String.IsNullOrWhiteSpace(title))
            {
                return false;
            }

            IntPtr stylePtr = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_STYLE);
            long style = stylePtr.ToInt64();
            return (style & NativeMethods.WS_CHILD) != NativeMethods.WS_CHILD;
        }

        private WindowItem CreateWindowItem(IntPtr hWnd)
        {
            uint processId;
            NativeMethods.GetWindowThreadProcessId(hWnd, out processId);

            string title = NativeMethods.GetWindowText(hWnd);
            string processName;
            try
            {
                processName = Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                processName = "pid " + processId;
            }

            bool isTopMost = (NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE).ToInt64() & NativeMethods.WS_EX_TOPMOST) == NativeMethods.WS_EX_TOPMOST;
            return new WindowItem(hWnd, title, processName, isTopMost);
        }

        private Icon GetWindowIcon(IntPtr hWnd)
        {
            IntPtr iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL2), IntPtr.Zero);
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL), IntPtr.Zero);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_BIG), IntPtr.Zero);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.GetClassLongPtr(hWnd, NativeMethods.GCLP_HICONSM);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.GetClassLongPtr(hWnd, NativeMethods.GCLP_HICON);
            }

            if (iconHandle == IntPtr.Zero)
            {
                return SystemIcons.Application;
            }

            try
            {
                Icon icon = Icon.FromHandle(iconHandle);
                return (Icon)icon.Clone();
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void SetTargetTopMost(IntPtr hWnd, bool topMost)
        {
            if (hWnd == IntPtr.Zero || !NativeMethods.IsWindow(hWnd))
            {
                SetStatus("窗口无效，请刷新后重试");
                return;
            }

            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            NativeMethods.SetWindowPos(
                hWnd,
                topMost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST,
                0,
                0,
                0,
                0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
            NativeMethods.SetForegroundWindow(hWnd);
        }

        private WindowItem GetSelectedWindow()
        {
            if (windowListView.SelectedItems.Count == 0)
            {
                return null;
            }

            return windowListView.SelectedItems[0].Tag as WindowItem;
        }

        private void ResizeColumns()
        {
            if (windowListView.Columns.Count < 3)
            {
                return;
            }

            int titleWidth = windowListView.ClientSize.Width - windowListView.Columns[0].Width - windowListView.Columns[1].Width - 10;
            windowListView.Columns[2].Width = Math.Max(180, titleWidth);
        }

        private void SetToggleButtonState()
        {
            WindowItem item = GetSelectedWindow();
            toggleButton.Enabled = item != null;
            toggleButton.Text = item != null && item.IsTopMost ? "取消置顶" : "置顶";
        }

        private string GetHotKeyStatusText()
        {
            return "指向窗口：" + settings.ToggleTargetHotKey.DisplayText
                + "；显示/隐藏：" + settings.ShowHideHotKey.DisplayText
                + "；取消全部：" + settings.ClearAllHotKey.DisplayText;
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        private void ToggleToolVisibility()
        {
            if (Visible && WindowState != FormWindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
            }
            else
            {
                ShowFromTray();
            }
        }

        private void ExitApplication()
        {
            exiting = true;
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                return;
            }

            refreshTimer.Stop();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            refreshTimer.Dispose();
        }

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush backBrush = new SolidBrush(HeaderBack))
            using (Pen linePen = new Pen(Color.FromArgb(32, 72, 90)))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
                e.Graphics.DrawLine(linePen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            Rectangle textBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), textBounds, Accent, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            Color backColor = selected ? SelectedBack : (e.ItemIndex % 2 == 0 ? RowBack : RowAltBack);
            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            Color foreColor = e.ColumnIndex == 1 && e.SubItem.Text.Length > 0 ? Accent : TextMain;
            Rectangle textBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height);

            if (e.ColumnIndex == 0 && e.Item.ImageIndex >= 0 && e.Item.ImageIndex < windowIcons.Images.Count)
            {
                Image image = windowIcons.Images[e.Item.ImageIndex];
                int iconTop = e.Bounds.Top + (e.Bounds.Height - 16) / 2;
                e.Graphics.DrawImage(image, e.Bounds.Left + 8, iconTop, 16, 16);
                textBounds.X += 24;
                textBounds.Width -= 24;
            }

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, new Font("Microsoft YaHei UI", 9F), textBounds, foreColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private sealed class WindowItem
        {
            public WindowItem(IntPtr handle, string title, string processName, bool isTopMost)
            {
                Handle = handle;
                Title = title;
                ProcessName = processName;
                IsTopMost = isTopMost;
            }

            public IntPtr Handle { get; private set; }
            public string Title { get; private set; }
            public string ProcessName { get; private set; }
            public bool IsTopMost { get; private set; }
        }
    }
}
