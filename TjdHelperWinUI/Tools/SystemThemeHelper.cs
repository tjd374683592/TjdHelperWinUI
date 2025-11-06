using System;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace TjdHelperWinUI.Tools
{
    public class SystemThemeHelper
    {
        /// <summary>
        /// 判断当前系统是否为深色主题
        /// </summary>
        public static bool IsSystemDarkTheme()
        {
            var uiSettings = new UISettings();
            Color backgroundColor = uiSettings.GetColorValue(UIColorType.Background);
            // 计算颜色亮度（简单平均）
            double brightness = (backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3.0;
            return brightness < 128; // 小于128视为深色主题
        }

        /// <summary>
        /// 设置系统全局浅色/深色主题
        /// </summary>
        /// <param name="dark">true = 深色, false = 浅色</param>
        public static void SetSystemTheme(bool dark)
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            using (var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true))
            {
                if (key == null)
                    return;

                int value = dark ? 0 : 1;
                key.SetValue("AppsUseLightTheme", value, RegistryValueKind.DWord);
                key.SetValue("SystemUsesLightTheme", value, RegistryValueKind.DWord);
            }

            // 通知系统刷新主题
            RefreshSystemTheme();
        }

        /// <summary>
        /// 广播系统设置变更，立即刷新主题
        /// </summary>
        private static void RefreshSystemTheme()
        {
            const int HWND_BROADCAST = 0xffff;
            const int WM_SETTINGCHANGE = 0x001A;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessageTimeout(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);
    }
}