using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MxHmi
{
    internal sealed class AppSettings
    {
        private const string SettingsKeyPath = @"Software\MX HMI\TopMost";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "MX HMI TopMost";

        public AppSettings()
        {
            ToggleTargetHotKey = new HotKeySetting(NativeMethods.MOD_CONTROL, Keys.D5);
            ShowHideHotKey = new HotKeySetting(NativeMethods.MOD_CONTROL, Keys.D6);
            ClearAllHotKey = new HotKeySetting(NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, Keys.R);
        }

        public bool StartWithWindows { get; set; }
        public HotKeySetting ToggleTargetHotKey { get; set; }
        public HotKeySetting ShowHideHotKey { get; set; }
        public HotKeySetting ClearAllHotKey { get; set; }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath))
            {
                if (key != null)
                {
                    settings.ToggleTargetHotKey = ReadHotKey(key, "Toggle", settings.ToggleTargetHotKey);
                    settings.ShowHideHotKey = ReadHotKey(key, "ShowHide", settings.ShowHideHotKey);
                    settings.ClearAllHotKey = ReadHotKey(key, "ClearAll", settings.ClearAllHotKey);
                }
            }

            settings.StartWithWindows = IsStartWithWindowsEnabled();
            return settings;
        }

        public AppSettings Clone()
        {
            return new AppSettings
            {
                StartWithWindows = StartWithWindows,
                ToggleTargetHotKey = ToggleTargetHotKey,
                ShowHideHotKey = ShowHideHotKey,
                ClearAllHotKey = ClearAllHotKey
            };
        }

        public void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath))
            {
                WriteHotKey(key, "Toggle", ToggleTargetHotKey);
                WriteHotKey(key, "ShowHide", ShowHideHotKey);
                WriteHotKey(key, "ClearAll", ClearAllHotKey);
            }

            SetStartWithWindows(StartWithWindows);
        }

        private static HotKeySetting ReadHotKey(RegistryKey key, string prefix, HotKeySetting fallback)
        {
            object modifiersValue = key.GetValue(prefix + "Modifiers");
            object keyValue = key.GetValue(prefix + "Key");

            if (modifiersValue == null || keyValue == null)
            {
                return fallback;
            }

            try
            {
                uint modifiers = Convert.ToUInt32(modifiersValue);
                Keys hotKey = (Keys)Convert.ToInt32(keyValue);
                HotKeySetting result = new HotKeySetting(modifiers, hotKey);
                return result.IsValid ? result : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static void WriteHotKey(RegistryKey key, string prefix, HotKeySetting hotKey)
        {
            key.SetValue(prefix + "Modifiers", (int)hotKey.Modifiers, RegistryValueKind.DWord);
            key.SetValue(prefix + "Key", (int)hotKey.Key, RegistryValueKind.DWord);
        }

        private static bool IsStartWithWindowsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
            {
                if (key == null)
                {
                    return false;
                }

                object value = key.GetValue(RunValueName);
                if (value == null)
                {
                    return false;
                }

                string text = value.ToString();
                return text.IndexOf(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private static void SetStartWithWindows(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (enabled)
                {
                    key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\" --tray", RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(RunValueName, false);
                }
            }
        }
    }

    internal struct HotKeySetting
    {
        private readonly uint modifiers;
        private readonly Keys key;

        public HotKeySetting(uint modifiers, Keys key)
        {
            this.modifiers = modifiers;
            this.key = key;
        }

        public uint Modifiers
        {
            get { return modifiers; }
        }

        public Keys Key
        {
            get { return key; }
        }

        public bool IsValid
        {
            get
            {
                return Key != Keys.None
                    && Key != Keys.ControlKey
                    && Key != Keys.Menu
                    && Key != Keys.ShiftKey
                    && Modifiers != 0;
            }
        }

        public string DisplayText
        {
            get
            {
                if (!IsValid)
                {
                    return "";
                }

                string text = "";
                if ((Modifiers & NativeMethods.MOD_CONTROL) == NativeMethods.MOD_CONTROL)
                {
                    text += "Ctrl+";
                }
                if ((Modifiers & NativeMethods.MOD_ALT) == NativeMethods.MOD_ALT)
                {
                    text += "Alt+";
                }
                if ((Modifiers & NativeMethods.MOD_SHIFT) == NativeMethods.MOD_SHIFT)
                {
                    text += "Shift+";
                }

                return text + Key.ToString();
            }
        }

        public static HotKeySetting FromKeyData(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            uint modifiers = 0;

            if ((keyData & Keys.Control) == Keys.Control)
            {
                modifiers |= NativeMethods.MOD_CONTROL;
            }
            if ((keyData & Keys.Alt) == Keys.Alt)
            {
                modifiers |= NativeMethods.MOD_ALT;
            }
            if ((keyData & Keys.Shift) == Keys.Shift)
            {
                modifiers |= NativeMethods.MOD_SHIFT;
            }

            return new HotKeySetting(modifiers, key);
        }

        public bool Equals(HotKeySetting other)
        {
            return Modifiers == other.Modifiers && Key == other.Key;
        }
    }
}
