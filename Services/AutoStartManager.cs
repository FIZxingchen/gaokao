using Microsoft.Win32;
using System;

namespace gokao
{
    public static class AutoStartManager
    {
        private const string AppName = "高考倒计时";
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    return key?.GetValue(AppName)?.ToString() == AssemblyPath;
                }
            }
            catch { return false; }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (enable)
                        key?.SetValue(AppName, AssemblyPath);
                    else
                        key?.DeleteValue(AppName, false);
                }
            }
            catch { /* 无注册表写入权限时静默忽略 */ }
        }

        private static string AssemblyPath =>
            System.Reflection.Assembly.GetExecutingAssembly().Location;
    }
}
