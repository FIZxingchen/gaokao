using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace gokao
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon trayIcon;
        private MainWindow mainWindow;

        // 图标文件只加载一次并静态缓存，避免每次重建窗口时反复读盘；
        // 进程生命周期内常驻，随进程退出由系统回收，无需逐实例释放。
        private static Icon _cachedIcon;
        private static readonly object _iconLock = new object();

        public TrayIconManager(MainWindow main)
        {
            mainWindow = main;
            InitializeTrayIcon();
            // 语言切换时刷新托盘文案
            LanguageManager.Instance.LanguageChanged += (s, args) => RefreshLanguage();
        }

        /// <summary>加载托盘图标：优先读程序目录 ico/daojishi_1.ico，失败回退系统图标；结果静态缓存</summary>
        private static Icon GetTrayIcon()
        {
            if (_cachedIcon != null) return _cachedIcon;
            lock (_iconLock)
            {
                if (_cachedIcon != null) return _cachedIcon;
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ico", "daojishi_1.ico");
                try
                {
                    _cachedIcon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
                }
                catch
                {
                    _cachedIcon = SystemIcons.Application;
                }
                return _cachedIcon;
            }
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = GetTrayIcon();

            trayIcon.Text = LanguageManager.Instance["TrayText"];
            trayIcon.Visible = true;

            var menu = new ContextMenuStrip();

            var settingsItem = new ToolStripMenuItem(LanguageManager.Instance["TraySettings"]);
            settingsItem.Click += (s, args) =>
            {
                if (mainWindow == null || !WindowHelper.IsShown(mainWindow))
                {
                    if (mainWindow == null)
                        mainWindow = new MainWindow();
                    WindowHelper.ShowActive(mainWindow);
                }
                else
                {
                    mainWindow.Activate();
                }
            };

            var toggleWindowItem = new ToolStripMenuItem(LanguageManager.Instance["TrayToggle"]);
            toggleWindowItem.Click += (s, args) => ToggleCountdownVisibility();

            var exitItem = new ToolStripMenuItem(LanguageManager.Instance["TrayExit"]);
            exitItem.Click += (s, args) =>
            {
                trayIcon.Visible = false;
                Application.Current.Shutdown();
            };

            menu.Items.Add(settingsItem);
            menu.Items.Add(toggleWindowItem);
            menu.Items.Add(exitItem);
            trayIcon.ContextMenuStrip = menu;

            trayIcon.DoubleClick += (s, args) => ToggleCountdownVisibility();
        }

        /// <summary>语言切换后刷新托盘图标提示与菜单文案（保持事件处理器不变）</summary>
        private void RefreshLanguage()
        {
            trayIcon.Text = LanguageManager.Instance["TrayText"];
            if (trayIcon.ContextMenuStrip != null && trayIcon.ContextMenuStrip.Items.Count >= 3)
            {
                trayIcon.ContextMenuStrip.Items[0].Text = LanguageManager.Instance["TraySettings"];
                trayIcon.ContextMenuStrip.Items[1].Text = LanguageManager.Instance["TrayToggle"];
                trayIcon.ContextMenuStrip.Items[2].Text = LanguageManager.Instance["TrayExit"];
            }
        }

        /// <summary>切换所有活跃倒计时窗口的显示/隐藏（菜单项与双击共用）</summary>
        private void ToggleCountdownVisibility()
        {
            if (CountdownWindow.AnyVisible())
                CountdownWindow.HideAll();
            else
                CountdownWindow.ShowAll();
        }

        public void ShowStartupTip()
        {
            trayIcon.ShowBalloonTip(
                1500,
                LanguageManager.Instance["TrayText"],
                LanguageManager.Instance["StartupTip"],
                ToolTipIcon.Info);
        }

        public void Dispose()
        {
            // 图标为静态缓存（进程级），不在此处释放，避免误释放共享的 SystemIcons.Application
            if (trayIcon != null)
            {
                trayIcon.Dispose();
            }
        }
    }
}
