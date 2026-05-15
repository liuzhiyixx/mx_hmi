using System;
using System.Drawing;
using System.Windows.Forms;

namespace MxHmi
{
    internal sealed class SettingsForm : Form
    {
        private static readonly Color ShellBack = Color.FromArgb(10, 16, 24);
        private static readonly Color PanelBack = Color.FromArgb(16, 26, 38);
        private static readonly Color Accent = Color.FromArgb(0, 210, 255);
        private static readonly Color TextMain = Color.FromArgb(226, 241, 247);
        private static readonly Color TextMuted = Color.FromArgb(132, 156, 170);

        private readonly CheckBox startupCheckBox;
        private readonly HotKeyTextBox toggleHotKeyBox;
        private readonly HotKeyTextBox showHideHotKeyBox;
        private readonly HotKeyTextBox clearAllHotKeyBox;

        public SettingsForm(AppSettings settings)
        {
            Settings = settings.Clone();

            Text = "设置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(450, 255);
            BackColor = ShellBack;
            ForeColor = TextMain;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.ColumnCount = 2;
            root.RowCount = 6;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            startupCheckBox = new CheckBox();
            startupCheckBox.Text = "开机自启";
            startupCheckBox.Checked = Settings.StartWithWindows;
            startupCheckBox.AutoSize = true;
            startupCheckBox.ForeColor = TextMain;
            startupCheckBox.Dock = DockStyle.Fill;
            root.Controls.Add(startupCheckBox, 0, 0);
            root.SetColumnSpan(startupCheckBox, 2);

            AddLabel(root, "鼠标指向窗口切换", 0, 1);
            toggleHotKeyBox = new HotKeyTextBox();
            toggleHotKeyBox.HotKey = Settings.ToggleTargetHotKey;
            root.Controls.Add(toggleHotKeyBox, 1, 1);

            AddLabel(root, "显示/隐藏本工具", 0, 2);
            showHideHotKeyBox = new HotKeyTextBox();
            showHideHotKeyBox.HotKey = Settings.ShowHideHotKey;
            root.Controls.Add(showHideHotKeyBox, 1, 2);

            AddLabel(root, "取消全部置顶", 0, 3);
            clearAllHotKeyBox = new HotKeyTextBox();
            clearAllHotKeyBox.HotKey = Settings.ClearAllHotKey;
            root.Controls.Add(clearAllHotKeyBox, 1, 3);

            Label hint = new Label();
            hint.Text = "点击输入框后直接按快捷键，例如 Ctrl+Alt+T";
            hint.ForeColor = TextMuted;
            hint.Dock = DockStyle.Fill;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(hint, 0, 4);
            root.SetColumnSpan(hint, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.BackColor = ShellBack;
            root.Controls.Add(buttons, 0, 5);
            root.SetColumnSpan(buttons, 2);

            Button okButton = CreateButton("保存");
            okButton.Click += OnSaveClick;
            buttons.Controls.Add(okButton);

            Button cancelButton = CreateButton("取消");
            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(cancelButton);
        }

        public AppSettings Settings { get; private set; }

        private void AddLabel(TableLayoutPanel root, string text, int column, int row)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = TextMuted;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(label, column, row);
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 76;
            button.Height = 30;
            button.Margin = new Padding(8, 4, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Accent;
            button.BackColor = Color.FromArgb(12, 24, 34);
            button.ForeColor = TextMain;
            return button;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!toggleHotKeyBox.HotKey.IsValid || !showHideHotKeyBox.HotKey.IsValid || !clearAllHotKeyBox.HotKey.IsValid)
            {
                MessageBox.Show(this, "快捷键必须包含 Ctrl、Alt 或 Shift，并带一个普通按键。", "设置无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (toggleHotKeyBox.HotKey.Equals(showHideHotKeyBox.HotKey)
                || toggleHotKeyBox.HotKey.Equals(clearAllHotKeyBox.HotKey)
                || showHideHotKeyBox.HotKey.Equals(clearAllHotKeyBox.HotKey))
            {
                MessageBox.Show(this, "三个快捷键不能相同。", "设置无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Settings.StartWithWindows = startupCheckBox.Checked;
            Settings.ToggleTargetHotKey = toggleHotKeyBox.HotKey;
            Settings.ShowHideHotKey = showHideHotKeyBox.HotKey;
            Settings.ClearAllHotKey = clearAllHotKeyBox.HotKey;
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class HotKeyTextBox : TextBox
        {
            private HotKeySetting hotKey;

            public HotKeyTextBox()
            {
                Dock = DockStyle.Fill;
                BorderStyle = BorderStyle.FixedSingle;
                BackColor = PanelBack;
                ForeColor = TextMain;
                Font = new Font("Segoe UI", 10F);
                ReadOnly = true;
            }

            public HotKeySetting HotKey
            {
                get { return hotKey; }
                set
                {
                    hotKey = value;
                    Text = hotKey.DisplayText;
                }
            }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                CaptureHotKey(keyData);
                return true;
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                CaptureHotKey(e.KeyData);
                e.SuppressKeyPress = true;
                e.Handled = true;
            }

            private void CaptureHotKey(Keys keyData)
            {
                HotKeySetting captured = HotKeySetting.FromKeyData(keyData);
                if (!captured.IsValid)
                {
                    return;
                }

                HotKey = captured;
            }
        }
    }
}
