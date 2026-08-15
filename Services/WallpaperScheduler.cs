using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace gokao
{
    /// <summary>
    /// 倒计时自动切换壁纸调度器：按当前事件剩余天数匹配预设规则，
    /// 跨过阈值时自动切换桌面壁纸。只在活动规则变化时设置一次，避免重复写入注册表。
    /// 规则存于事件配置文件 [WallpaperSchedule] 节，RuleN = 天数|壁纸路径。
    /// </summary>
    public static class WallpaperScheduler
    {
        private const string Section = "WallpaperSchedule";
        private const int MaxRules = 20;
        private const string LastAppliedKey = "LastApplied"; // 事件配置中记录"上次已应用规则"，重启后据此判断是否补切
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

        private static DispatcherTimer _timer;
        private static string _lastApplied = ""; // 上次已应用的规则标识（天数|壁纸），防止反复设置同一壁纸

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        /// <summary>启动调度器（App 启动时调用）：读取上次已应用的规则记录（不立即切换），之后每 30 秒复查</summary>
        public static void Start()
        {
            if (_timer != null) return;
            _timer = new DispatcherTimer(
                CheckInterval,
                DispatcherPriority.Background,
                (s, e) => CheckNow(),
                Application.Current.Dispatcher);
            _timer.Start();
            InitializeLastApplied();
        }

        /// <summary>
        /// 启动时从事件配置读取上次已应用的规则标识（不调用 ApplyWallpaper）。
        /// 首次启动或跨过阈值后（含当天未运行、之后才启动的情况）由后续 CheckNow 补切一次，
        /// 切换后记录会写回配置，因此之后每次启动若规则未变化则不再强制切换。
        /// </summary>
        private static void InitializeLastApplied()
        {
            try
            {
                string eventName = EventManager.GetActiveEventName();
                if (string.IsNullOrEmpty(eventName)) return;
                _lastApplied = ConfigManager.ReadString(
                    ConfigManager.EventConfigPath(eventName), Section, LastAppliedKey, "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[壁纸调度] 初始化失败: {ex.Message}");
            }
        }

        /// <summary>将当前已应用规则写回事件配置，保证重启后不重复切换</summary>
        private static void SaveLastApplied(string eventName)
        {
            try
            {
                ConfigManager.WriteString(
                    ConfigManager.EventConfigPath(eventName), Section, LastAppliedKey, _lastApplied ?? "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[壁纸调度] 保存规则记录失败: {ex.Message}");
            }
        }

        /// <summary>立即检查一次（规则增删后由壁纸窗口调用）</summary>
        public static void CheckNow()
        {
            try
            {
                string eventName = EventManager.GetActiveEventName();
                if (string.IsNullOrEmpty(eventName)) return;

                // 目标日期：从事件列表获取
                var evt = EventManager.CustomEvents.FirstOrDefault(e => e.Name == eventName);
                if (evt == null) return;
                DateTime target = evt.Date;

                int daysLeft = (int)(target - DateTime.Now).TotalDays;

                // 剩余天数 <= 阈值的规则中，取阈值最小的那条（如剩 75 天时 100 天规则生效，到 50 天切换下一条）
                string activeRule = FindActiveRule(eventName, daysLeft);
                if (activeRule == _lastApplied) return;
                _lastApplied = activeRule;
                // 无论是否命中规则都立即持久化，保证下次启动（含跨过阈值后才启动）能按记录判断：
                // 与上次记录不同则本次补切一次并更新记录；相同则后续启动不再强制切换
                SaveLastApplied(eventName);
                if (activeRule == null) return; // 未进入任何规则的阈值范围

                string path = activeRule.Substring(activeRule.IndexOf('|') + 1);
                ApplyWallpaper(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[壁纸调度] 检查失败: {ex.Message}");
            }
        }

        /// <summary>找出当前应生效的规则；无命中返回 null</summary>
        private static string FindActiveRule(string eventName, int daysLeft)
        {
            string cfg = ConfigManager.EventConfigPath(eventName);
            string best = null;
            int bestDays = int.MaxValue;
            for (int i = 1; i <= MaxRules; i++)
            {
                string raw = ConfigManager.ReadString(cfg, Section, "Rule" + i, "");
                if (string.IsNullOrEmpty(raw)) continue;
                int sep = raw.IndexOf('|');
                if (sep <= 0) continue;
                if (!int.TryParse(raw.Substring(0, sep), out int days)) continue;
                string path = raw.Substring(sep + 1);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                if (daysLeft <= days && days < bestDays)
                {
                    best = raw;
                    bestDays = days;
                }
            }
            return best;
        }

        /// <summary>设置桌面壁纸（填充模式），失败不抛异常</summary>
        private static void ApplyWallpaper(string imagePath)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        key.SetValue("WallpaperStyle", "10"); // 填充
                        key.SetValue("TileWallpaper", "0");
                    }
                }
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[壁纸调度] 设置失败: {ex.Message}");
            }
        }
    }
}
