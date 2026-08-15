using System.Windows;

namespace gokao
{
    /// <summary>
    /// 窗口显示辅助：统一处理"窗口最小化后无法唤回桌面"的问题。
    /// WPF 中 IsVisible 在最小化时仍为 true，仅 Activate() 不会还原最小化窗口，
    /// 因此判断"是否显示在桌面"必须同时检查 IsVisible 与 WindowState。
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>窗口是否真正显示在桌面（可见且未最小化）</summary>
        public static bool IsShown(Window w)
        {
            return w != null && w.IsVisible && w.WindowState != WindowState.Minimized;
        }

        /// <summary>将窗口唤回桌面：最小化则先还原，再显示并激活到前台</summary>
        public static void ShowActive(Window w)
        {
            if (w == null) return;
            if (w.WindowState == WindowState.Minimized)
                w.WindowState = WindowState.Normal;
            w.Show();
            w.Activate();
        }
    }
}
