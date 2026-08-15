using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using gokao.Models;

namespace gokao
{
    /// <summary>
    /// 壁纸选择器窗口。优化：提取壁纸样式常量，简化路径处理。
    /// </summary>
    public partial class Windowsucai : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private static readonly Dictionary<int, int> WallpaperStyles = new Dictionary<int, int>
        {
            { 0, 10 },  // 填充
            { 1, 6 },   // 适应
            { 2, 2 },   // 拉伸
            { 3, 0 },   // 平铺
            { 4, 0 },   // 居中
        };

        private string wallpaperDir;
        private string _pickedWallpaper = ""; // 自动切换规则里选定的壁纸路径

        public Windowsucai()
        {
            InitializeComponent();
            wallpaperDir = ResolveWallpaperDir();
        }

        /// <summary>
        /// 解析壁纸目录：优先程序目录，不可写时回退 E:/lj/ 或系统临时目录，
        /// 避免只读安装目录导致启动崩溃。
        /// </summary>
        private static string ResolveWallpaperDir()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpaper"),
                Path.Combine(@"E:\lj", "wallpaper"),
                Path.Combine(Path.GetTempPath(), "wallpaper")
            };
            foreach (string dir in candidates)
            {
                try
                {
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    // 探测可写性：能创建并删除临时文件才算可用
                    string probe = Path.Combine(dir, ".write_probe");
                    File.WriteAllText(probe, "1");
                    File.Delete(probe);
                    return dir;
                }
                catch
                {
                    // 尝试下一个候选目录
                }
            }
            // 全部失败时兜底返回程序目录（后续操作会走各自 try-catch）
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpaper");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWallpapers();
            LoadScheduleEvents();
        }

        /// <summary>点 X 只隐藏窗口，保留壁纸列表与规则状态，下次打开即时恢复（与主设置窗口行为一致）</summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }

        /// <summary>关闭窗口时释放缩略图位图，避免反复开关壁纸窗口导致内存持续占用。
        /// 先调用 base 触发 Closed 事件（让外部引用置空），再执行清理，防止清理异常阻断引用释放。</summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e); // 触发 Closed 事件，MainWindow 借此清空 _sucaiWindow 引用
            wallpaperPanel.Children.Clear(); // 解除对全部缩略图位图的引用
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadWallpapers();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var lang = LanguageManager.Instance;
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = $"{lang["ImgFileFilter"]}|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = lang["OpenWallpaperDialogTitle"],
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                int copied = 0;
                foreach (string file in dialog.FileNames)
                {
                    try
                    {
                        string dest = Path.Combine(wallpaperDir, Path.GetFileName(file));
                        if (!File.Exists(dest))
                        {
                            File.Copy(file, dest);
                            copied++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[壁纸] 复制失败: {ex.Message}");
                    }
                }
                if (copied > 0)
                {
                    statusBar.Text = string.Format(LanguageManager.Instance["AddedCount"], copied);
                    LoadWallpapers();
                }
                else
                {
                    statusBar.Text = LanguageManager.Instance["ExistsOrInvalid"];
                }
            }
        }

        private void LoadWallpapers()
        {
            wallpaperPanel.Children.Clear();
            ShowLoading(true);

            try
            {
                if (!Directory.Exists(wallpaperDir))
                {
                    statusBar.Text = LanguageManager.Instance["DirNotExist"];
                    ShowLoading(false);
                    return;
                }

                string[] files = Directory.GetFiles(wallpaperDir, "*.*", SearchOption.TopDirectoryOnly);
                int count = 0;
                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        // 单张失败不影响其他壁纸
                        if (AddWallpaperToPanel(file))
                            count++;
                    }
                }
                statusBar.Text = string.Format(LanguageManager.Instance["TotalCount"], count);
            }
            catch (Exception ex)
            {
                statusBar.Text = string.Format(LanguageManager.Instance["LoadFailed"], ex.Message);
            }
            ShowLoading(false);
        }

        /// <summary>添加单张壁纸缩略图；图片损坏/解码失败时返回 false，不影响其他壁纸</summary>
        private bool AddWallpaperToPanel(string imagePath)
        {
            BitmapImage bitmap;
            try
            {
                // 缩略图按需小尺寸解码：避免全尺寸解码 4K 壁纸浪费内存
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 360; // 178 缩略图 × 2 倍高清屏足够
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            catch
            {
                // 损坏图片跳过，不中断整个列表；失败位图不入树，无内存残留
                return false;
            }

            var border = new Border
            {
                Width = 180,
                Height = 120,
                Margin = new Thickness(5),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = imagePath,
                ToolTip = Path.GetFileName(imagePath),
                Background = Brushes.White
            };

            var grid = new Grid();
            var image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.UniformToFill,
                Width = 178,
                Height = 118,
                ClipToBounds = true
            };
            grid.Children.Add(image);

            var deleteBtn = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 3, 0),
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Arrow,
                Tag = imagePath,
                ToolTip = LanguageManager.Instance["DeleteWallpaperTip"]
            };
            deleteBtn.Click += (s, e) =>
            {
                string path = (s as Button)?.Tag as string;
                if (path != null && File.Exists(path))
                {
                    string name = Path.GetFileName(path);
                    var lang = LanguageManager.Instance;
                    if (MessageBox.Show(string.Format(lang["DeleteWallpaper"], name), lang["DeleteConfirmTitle"],
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            File.Delete(path);
                            statusBar.Text = string.Format(LanguageManager.Instance["Deleted"], name);
                            LoadWallpapers();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format(LanguageManager.Instance["DeleteFailed"], ex.Message),
                                LanguageManager.Instance["ErrorTitle"],
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                e.Handled = true;
            };
            grid.Children.Add(deleteBtn);

            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is Button) return; // 删除按钮不触发选择
                Image_MouseLeftButtonDown(s, e);
            };

            border.MouseEnter += (s, e) => border.BorderBrush = Brushes.DodgerBlue;
            border.MouseLeave += (s, e) => border.BorderBrush = Brushes.LightGray;

            wallpaperPanel.Children.Add(border);
            return true;
        }

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string path && File.Exists(path))
            {
                bool success = SetWallpaper(path);
                if (success)
                {
                    ShowSuccessAnimation(border);
                    statusBar.Text = string.Format(LanguageManager.Instance["SetWallpaperDone"], Path.GetFileName(path));
                }
                else
                {
                    statusBar.Text = LanguageManager.Instance["SetWallpaperFailed"];
                }
            }
        }

        private void ShowSuccessAnimation(Border border)
        {
            border.BorderBrush = Brushes.LimeGreen;
            border.BorderThickness = new Thickness(3);
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                border.BorderBrush = Brushes.LightGray;
                border.BorderThickness = new Thickness(1);
            };
            timer.Start();
        }

        private bool SetWallpaper(string imagePath)
        {
            try
            {
                int styleIndex = wallpaperStyle.SelectedIndex;
                if (styleIndex < 0) styleIndex = 0;
                int style = WallpaperStyles.TryGetValue(styleIndex, out int s) ? s : 10;

                // 先设置注册表样式
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        key.SetValue("WallpaperStyle", style.ToString());
                        key.SetValue("TileWallpaper", "0");
                    }
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[壁纸] 设置失败: {ex.Message}");
                return false;
            }
        }

        private void ShowLoading(bool isLoading)
        {
            loadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── 倒计时自动切换壁纸规则 ──

        private const string ScheduleSection = "WallpaperSchedule";
        private const int ScheduleMaxRules = 20;

        /// <summary>单条规则：剩余天数 + 壁纸路径</summary>
        private class RuleInfo
        {
            public int Days;
            public string Path;
            public override string ToString()
            {
                // 字段名 Path 遮蔽 System.IO.Path，这里显式限定
                return string.Format(LanguageManager.Instance["WsRuleFormat"], Days, System.IO.Path.GetFileName(Path));
            }
        }

        /// <summary>填充事件下拉框（缺省补"高考"），并定位到当前激活事件</summary>
        private void LoadScheduleEvents()
        {
            EventManager.LoadCustomEvents();
            if (EventManager.CustomEvents.Count == 0)
            {
                var defaultDate = new DateTime(DateTime.Now.Year, 6, 7);
                if (DateTime.Now > defaultDate) defaultDate = defaultDate.AddYears(1);
                EventManager.CustomEvents.Add(new CustomEvent { Name = "高考", Date = defaultDate });
            }
            scheduleEventCombo.ItemsSource = EventManager.CustomEvents;
            string active = EventManager.GetActiveEventName();
            scheduleEventCombo.SelectedItem = EventManager.CustomEvents.FirstOrDefault(e => e.Name == active)
                                              ?? EventManager.CustomEvents[0];
        }

        /// <summary>重新加载当前选中事件的规则列表</summary>
        private void LoadScheduleRules()
        {
            scheduleRuleList.Items.Clear();
            var evt = scheduleEventCombo.SelectedItem as CustomEvent;
            if (evt == null) return;
            string cfg = ConfigManager.EventConfigPath(evt.Name);
            for (int i = 1; i <= ScheduleMaxRules; i++)
            {
                string raw = ConfigManager.ReadString(cfg, ScheduleSection, "Rule" + i, "");
                if (string.IsNullOrEmpty(raw)) continue;
                int sep = raw.IndexOf('|');
                if (sep <= 0) continue;
                if (!int.TryParse(raw.Substring(0, sep), out int days)) continue;
                string path = raw.Substring(sep + 1);
                if (string.IsNullOrEmpty(path)) continue;
                scheduleRuleList.Items.Add(new RuleInfo { Days = days, Path = path });
            }
            if (scheduleRuleList.Items.Count == 0)
                scheduleRuleList.Items.Add(LanguageManager.Instance["WsNoRules"]);
        }

        private void ScheduleEventCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadScheduleRules();
        }

        private void PickWallpaperBtn_Click(object sender, RoutedEventArgs e)
        {
            var lang = LanguageManager.Instance;
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = $"{lang["ImgFileFilter"]}|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = lang["WsPickWallpaper"],
                InitialDirectory = wallpaperDir
            };
            if (dialog.ShowDialog() == true)
            {
                _pickedWallpaper = dialog.FileName;
                scheduleWallpaperName.Text = Path.GetFileName(dialog.FileName);
            }
        }

        private void AddRuleBtn_Click(object sender, RoutedEventArgs e)
        {
            var lang = LanguageManager.Instance;
            if (!int.TryParse(scheduleDaysBox.Text.Trim(), out int days) || days < 0)
            {
                statusBar.Text = lang["WsRuleDaysInvalid"];
                return;
            }
            if (string.IsNullOrEmpty(_pickedWallpaper) || !File.Exists(_pickedWallpaper))
            {
                statusBar.Text = lang["WsRuleNoWallpaper"];
                return;
            }
            var evt = scheduleEventCombo.SelectedItem as CustomEvent;
            if (evt == null) return;
            string cfg = ConfigManager.EventConfigPath(evt.Name);
            int index = 1;
            while (index <= ScheduleMaxRules &&
                   !string.IsNullOrEmpty(ConfigManager.ReadString(cfg, ScheduleSection, "Rule" + index, "")))
                index++;
            if (index > ScheduleMaxRules)
            {
                statusBar.Text = lang["WsRuleLimit"];
                return;
            }
            ConfigManager.WriteString(cfg, ScheduleSection, "Rule" + index, days + "|" + _pickedWallpaper);
            statusBar.Text = lang["WsRuleAdded"];
            LoadScheduleRules();
            WallpaperScheduler.CheckNow(); // 规则变化后立即重新检查
        }

        private void DeleteRuleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(scheduleRuleList.SelectedItem is RuleInfo rule)) return;
            var evt = scheduleEventCombo.SelectedItem as CustomEvent;
            if (evt == null) return;
            string cfg = ConfigManager.EventConfigPath(evt.Name);

            // 读出全部规则，去掉选中的那条后按顺序重写（空值会删除 INI 键，清掉多余编号）
            var rules = new List<string>();
            for (int i = 1; i <= ScheduleMaxRules; i++)
            {
                string raw = ConfigManager.ReadString(cfg, ScheduleSection, "Rule" + i, "");
                if (!string.IsNullOrEmpty(raw)) rules.Add(raw);
            }
            rules.Remove(rule.Days + "|" + rule.Path);
            for (int i = 1; i <= ScheduleMaxRules; i++)
                ConfigManager.WriteString(cfg, ScheduleSection, "Rule" + i, i <= rules.Count ? rules[i - 1] : "");

            statusBar.Text = LanguageManager.Instance["WsRuleDeleted"];
            LoadScheduleRules();
            WallpaperScheduler.CheckNow();
        }
    }
}
