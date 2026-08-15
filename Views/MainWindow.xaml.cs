using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using gokao.Models;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using gokao.Views;

namespace gokao
{
    public partial class MainWindow : Window
    {
        /// <summary>获取选中事件的倒计时窗口（不存在返回 null）</summary>
        private CountdownWindow SelectedWindow =>
            _selectedEvent != null ? CountdownWindow.GetWindow(_selectedEvent.Name) : null;

        private Color currentPreviewColor = Colors.White;

        private Window _toastWindow;
        private DispatcherTimer _toastTimer;
        private Color confirmedColor = Colors.White;
        private string selectedBgPath = "";

        private Windowsucai _sucaiWindow;

        private CustomEvent _selectedEvent;
        private ExamMode _examMode;

        private bool _suppressAnimationWrite = true;
        private bool _suppressActiveToggle = true;

        // ── 构造 ──
        public MainWindow()
        {
            InitializeComponent();
            PopulateFontFamilies();
            InitializeEvents();
            LoadConfig();
            _suppressAnimationWrite = false;
            _suppressActiveToggle = false;
            UpdatePreview();
        }

        // 系统字体名缓存
        private static readonly System.Collections.Generic.List<string> _fontFamilyCache =
            new System.Collections.Generic.List<string>();

        private void PopulateFontFamilies()
        {
            fontFamilyCombo.Items.Clear();
            if (_fontFamilyCache.Count == 0)
            {
                try
                {
                    foreach (var font in Drawing.FontFamily.Families)
                        _fontFamilyCache.Add(font.Name);
                }
                catch { }
            }
            foreach (string name in _fontFamilyCache)
                fontFamilyCombo.Items.Add(new ComboBoxItem { Content = name });
        }

        private ComboBoxItem FindFontFamilyItem(string fontName)
        {
            foreach (ComboBoxItem item in fontFamilyCombo.Items)
                if (item.Content?.ToString() == fontName) return item;
            return null;
        }

        // ── 配置加载 ──
        private void LoadConfig()
        {
            string savedLang = ConfigManager.ReadString("General", "Language", "Auto");
            foreach (ComboBoxItem item in languageCombo.Items)
                if ((item.Tag?.ToString() ?? "Auto") == savedLang) { languageCombo.SelectedItem = item; break; }

            autoStartCheck.IsChecked = AutoStartManager.IsAutoStart();
            startupTipCheck.IsChecked = ConfigManager.ReadBool("State", "ShowStartupTip", true);

            // 加载事件列表 + 活跃事件
            EventManager.LoadCustomEvents();
            EventManager.LoadActiveEvents();
            foreach (var evt in EventManager.CustomEvents)
                evt.IsActive = EventManager.IsActiveEvent(evt.Name);

            eventListBox.ItemsSource = EventManager.CustomEvents;
            if (EventManager.CustomEvents.Count > 0)
                eventListBox.SelectedItem = EventManager.CustomEvents[0];
        }

        /// <summary>启动时为所有活跃事件创建倒计时窗口</summary>
        public void CreateActiveWindows()
        {
            foreach (var evt in EventManager.CustomEvents)
            {
                if (evt.IsActive)
                    CountdownWindow.ShowWindow(evt.Name, evt.Date);
            }
        }

        // ── 事件绑定 ──
        private void InitializeEvents()
        {
            autoStartCheck.Checked += (s, e) => AutoStartManager.SetAutoStart(true);
            autoStartCheck.Unchecked += (s, e) => AutoStartManager.SetAutoStart(false);

            startupTipCheck.Checked += (s, e) => ConfigManager.WriteBool("State", "ShowStartupTip", true);
            startupTipCheck.Unchecked += (s, e) => ConfigManager.WriteBool("State", "ShowStartupTip", false);

            opacitySlider.ValueChanged += (s, e) =>
            {
                double v = opacitySlider.Value;
                opacityValueText.Text = $"{(int)(v * 100)}%";
                var w = SelectedWindow;
                if (w != null) w.SetOpacity(v);
                else if (_selectedEvent != null)
                    ConfigManager.WriteDouble(ConfigManager.EventConfigPath(_selectedEvent.Name), "Style", "Opacity", v);
            };

            fontSizeSlider.ValueChanged += (s, e) =>
            {
                double v = fontSizeSlider.Value;
                fontSizeValueText.Text = $"{(int)v}";
                var w = SelectedWindow;
                if (w != null) w.SetFontSize(v);
                else if (_selectedEvent != null)
                    ConfigManager.WriteDouble(ConfigManager.EventConfigPath(_selectedEvent.Name), "Style", "FontSize", v);
            };

            lockWindowCheck.Checked += (s, e) =>
            {
                var w = SelectedWindow;
                if (w != null) w.SetLocked(true);
                else if (_selectedEvent != null)
                    ConfigManager.WriteBool(ConfigManager.EventConfigPath(_selectedEvent.Name), "State", "IsLocked", true);
            };
            lockWindowCheck.Unchecked += (s, e) =>
            {
                var w = SelectedWindow;
                if (w != null) w.SetLocked(false);
                else if (_selectedEvent != null)
                    ConfigManager.WriteBool(ConfigManager.EventConfigPath(_selectedEvent.Name), "State", "IsLocked", false);
            };

            topmostCheck.Checked += (s, e) =>
            {
                var w = SelectedWindow;
                if (w != null) w.SetTopmost(true);
                else if (_selectedEvent != null)
                    ConfigManager.WriteBool(ConfigManager.EventConfigPath(_selectedEvent.Name), "State", "Topmost", true);
            };
            topmostCheck.Unchecked += (s, e) =>
            {
                var w = SelectedWindow;
                if (w != null) w.SetTopmost(false);
                else if (_selectedEvent != null)
                    ConfigManager.WriteBool(ConfigManager.EventConfigPath(_selectedEvent.Name), "State", "Topmost", false);
            };

            clickThroughCheck.Checked += (s, e) =>
            {
                var w = SelectedWindow;
                if (w != null) w.SetClickThrough(true);
                if (_selectedEvent != null)
                    ConfigManager.WriteBool(ConfigManager.EventConfigPath(_selectedEvent.Name), "State", "ClickThrough", true);
            };
            clickThroughCheck.Unchecked += (s, e) =>
            {
                var w = SelectedWindow;
                if (w != null) w.SetClickThrough(false);
                if (_selectedEvent != null)
                    ConfigManager.WriteBool(ConfigManager.EventConfigPath(_selectedEvent.Name), "State", "ClickThrough", false);
            };

            startButton.Click += (s, e) =>
            {
                if (_selectedEvent == null) return;
                // 勾选复选框 = 显示窗口
                _selectedEvent.IsActive = true;
                EventManager.SetActive(_selectedEvent.Name, true);
                CountdownWindow.ShowWindow(_selectedEvent.Name, _selectedEvent.Date);
            };

            closeButton.Click += (s, e) =>
            {
                if (_selectedEvent == null) return;
                _selectedEvent.IsActive = false;
                EventManager.SetActive(_selectedEvent.Name, false);
                CountdownWindow.HideWindow(_selectedEvent.Name);
            };

            Btn_sourse.Click += (s, e) =>
            {
                if (_sucaiWindow == null)
                {
                    _sucaiWindow = new Windowsucai();
                    _sucaiWindow.Closed += (sender, args) => _sucaiWindow = null;
                    _sucaiWindow.Show();
                }
                else if (!WindowHelper.IsShown(_sucaiWindow))
                    WindowHelper.ShowActive(_sucaiWindow);
                else
                    _sucaiWindow.Activate();
            };

            txstButton.Click += (s, e) =>
            {
                if (_examMode == null)
                {
                    _examMode = new ExamMode();
                    _examMode.Closed += (sender, args) => _examMode = null;
                    _examMode.Show();
                }
                else if (!WindowHelper.IsShown(_examMode))
                    WindowHelper.ShowActive(_examMode);
                else
                    _examMode.Activate();
            };
        }

        // ── 事件列表交互 ──

        private void EventListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (eventListBox.SelectedItem is CustomEvent evt)
            {
                _selectedEvent = evt;
                editEventName.Text = evt.Name;
                editEventDate.SelectedDate = evt.Date;
                ApplyEventConfigToUI(evt.Name);
            }
        }

        /// <summary>双击事件列表项 = 切换该事件的倒计时窗口显隐</summary>
        private void EventListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedEvent == null) return;
            ToggleEventWindow(_selectedEvent);
        }

        /// <summary>事件复选框：勾选显示窗口，取消隐藏窗口</summary>
        private void EventActiveCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressActiveToggle) return;
            if (!(sender is CheckBox cb)) return;
            if (!(cb.DataContext is CustomEvent evt)) return;

            bool active = cb.IsChecked ?? false;
            EventManager.SetActive(evt.Name, active);
            if (active)
                CountdownWindow.ShowWindow(evt.Name, evt.Date);
            else
                CountdownWindow.HideWindow(evt.Name);
        }

        /// <summary>切换事件窗口显隐（双击调用）</summary>
        private void ToggleEventWindow(CustomEvent evt)
        {
            bool currentlyActive = EventManager.IsActiveEvent(evt.Name);
            if (currentlyActive)
            {
                evt.IsActive = false;
                EventManager.SetActive(evt.Name, false);
                CountdownWindow.HideWindow(evt.Name);
            }
            else
            {
                evt.IsActive = true;
                EventManager.SetActive(evt.Name, true);
                CountdownWindow.ShowWindow(evt.Name, evt.Date);
            }
        }

        // ── 设置变更处理 ──

        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressAnimationWrite) return;
            if (fontFamilyCombo.SelectedItem is ComboBoxItem item)
            {
                string name = item.Content?.ToString() ?? "Microsoft YaHei";
                var w = SelectedWindow;
                if (w != null) w.SetFontFamily(name);
                else if (_selectedEvent != null)
                    ConfigManager.WriteString(ConfigManager.EventConfigPath(_selectedEvent.Name), "Style", "FontFamily", name);
            }
        }

        private void AnimationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressAnimationWrite) return;
            if (animationCombo.SelectedItem is ComboBoxItem item)
            {
                string type = item.Tag?.ToString() ?? "None";
                var w = SelectedWindow;
                if (w != null) w.SetAnimation(type);
                else if (_selectedEvent != null)
                    ConfigManager.WriteString(ConfigManager.EventConfigPath(_selectedEvent.Name), "Style", "Animation", type);
            }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (languageCombo.SelectedItem is ComboBoxItem item)
            {
                string lang = item.Tag?.ToString() ?? "Auto";
                LanguageManager.Instance.SetLanguage(lang);
            }
        }

        // ── 背景图片 ──

        private void SelectBgBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEvent == null) return;
            var lang = LanguageManager.Instance;
            var dialog = new OpenFileDialog
            {
                Filter = $"{lang["ImgFileFilter"]}|*.jpg;*.jpeg;*.png;*.bmp",
                Title = lang["OpenBgDialogTitle"]
            };
            if (dialog.ShowDialog() == true)
            {
                string activeEvent = _selectedEvent.Name;
                bgPreview.Background = null;
                var w = SelectedWindow;
                if (w != null)
                {
                    w.Background = null;
                    w.UpdateLayout();
                }

                string dest = EventManager.CopyToEventBg(dialog.FileName, activeEvent);
                selectedBgPath = dest;
                string evtCfg = ConfigManager.EventConfigPath(activeEvent);
                ConfigManager.WriteString(evtCfg, "Style", "BgImage", dest);

                var previewBmp = CountdownWindow.GetSharedBitmap(selectedBgPath, 200);
                bgPreview.Background = previewBmp != null ? new ImageBrush(previewBmp) : null;
                w?.ReloadBackground();
            }
        }

        private void ClearBgBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEvent == null) return;
            selectedBgPath = "";
            bgPreview.Background = null;

            string evtCfg = ConfigManager.EventConfigPath(_selectedEvent.Name);
            ConfigManager.WriteString(evtCfg, "Style", "BgImage", "");

            var w = SelectedWindow;
            if (w != null)
            {
                w.Background = null;
                w.UpdateLayout();
                w.ReloadBackground();
            }
        }

        // ── 事件管理 ──

        /// <summary>从事件配置文件读取 UI 控件设置并应用到界面</summary>
        private void ApplyEventConfigToUI(string eventName)
        {
            string cfg = ConfigManager.EventConfigPath(eventName);

            double opacity = Clamp(ConfigManager.ReadDouble(cfg, "Style", "Opacity", 0.7), 0.1, 1.0);
            opacitySlider.Value = opacity;
            opacityValueText.Text = $"{(int)(opacity * 100)}%";

            double fontSize = Clamp(ConfigManager.ReadDouble(cfg, "Style", "FontSize", 24), 12, 72);
            fontSizeSlider.Value = fontSize;
            fontSizeValueText.Text = $"{(int)fontSize}";

            string fontFamily = ConfigManager.ReadString(cfg, "Style", "FontFamily", "Microsoft YaHei");
            fontFamilyCombo.SelectedItem = FindFontFamilyItem(fontFamily);

            string colorStr = ConfigManager.ReadString(cfg, "Style", "TextColor", "#FFFFFFFF");
            if (ColorConverter.ConvertFromString(colorStr) is Color color)
            {
                currentPreviewColor = color;
                confirmedColor = color;
                colorPreview.Background = new SolidColorBrush(color);
            }

            string animType = ConfigManager.ReadString(cfg, "Style", "Animation", "None");
            _suppressAnimationWrite = true;
            foreach (ComboBoxItem item in animationCombo.Items)
                if (item.Tag?.ToString() == animType) { animationCombo.SelectedItem = item; break; }
            _suppressAnimationWrite = false;

            string bgPath = ConfigManager.ReadString(cfg, "Style", "BgImage", "");
            if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
            {
                selectedBgPath = bgPath;
                var previewBmp = CountdownWindow.GetSharedBitmap(bgPath, 200);
                bgPreview.Background = previewBmp != null ? new ImageBrush(previewBmp) : null;
            }
            else
            {
                selectedBgPath = "";
                bgPreview.Background = null;
            }

            lockWindowCheck.IsChecked = ConfigManager.ReadBool(cfg, "State", "IsLocked", false);
            topmostCheck.IsChecked = ConfigManager.ReadBool(cfg, "State", "Topmost", true);
            clickThroughCheck.IsChecked = ConfigManager.ReadBool(cfg, "State", "ClickThrough", false);
        }

        /// <summary>生成不重复的默认事件名</summary>
        private static string GenerateNewEventName()
        {
            string baseName = LanguageManager.Instance["NewEvent"];
            string format = LanguageManager.Instance["NewEventNameFormat"];
            int maxN = 0;
            foreach (var evt in EventManager.CustomEvents)
            {
                string name = evt?.Name ?? "";
                if (!name.StartsWith(baseName, StringComparison.Ordinal)) continue;
                string rest = name.Substring(baseName.Length).Trim();
                if (int.TryParse(rest, out int n) && n > maxN) maxN = n;
            }
            return string.Format(format, baseName, maxN + 1);
        }

        private void AddEventBtn_Click(object sender, RoutedEventArgs e)
        {
            var evt = new CustomEvent { Name = GenerateNewEventName(), Date = DateTime.Now.AddDays(30) };
            EventManager.CustomEvents.Add(evt);
            EventManager.SaveCustomEvents();
            CountdownWindow.SaveDefaultStyleToEvent(evt.Name);
            eventListBox.SelectedItem = evt;
        }

        private void DeleteEventBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEvent == null) return;
            var lang = LanguageManager.Instance;
            if (MessageBox.Show(string.Format(lang["ConfirmDeleteEvent"], _selectedEvent.Name), lang["ConfirmDeleteTitle"],
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            string deletedName = _selectedEvent.Name;

            // 先隐藏窗口释放引用
            if (EventManager.IsActiveEvent(deletedName))
                CountdownWindow.HideWindow(deletedName);

            EventManager.SetActive(deletedName, false);
            EventManager.CustomEvents.Remove(_selectedEvent);
            EventManager.SaveCustomEvents();

            bgPreview.Background = null;
            selectedBgPath = "";

            EventManager.DeleteEventBgDir(deletedName);
            CountdownWindow.ClearBitmapCache(EventManager.EventBgDir(deletedName));

            string evtIni = ConfigManager.EventConfigPath(deletedName);
            if (File.Exists(evtIni))
            {
                try { File.Delete(evtIni); }
                catch (Exception ex) { LogManager.Log(ex, "删除事件配置文件失败: " + deletedName); }
            }

            if (EventManager.CustomEvents.Count > 0)
            {
                eventListBox.SelectedItem = EventManager.CustomEvents[0];
            }
            else
            {
                var defaultDate = new DateTime(DateTime.Now.Year, 6, 7);
                if (DateTime.Now > defaultDate) defaultDate = defaultDate.AddYears(1);
                var defaultEvent = new CustomEvent { Name = "高考", Date = defaultDate, IsActive = true };
                EventManager.CustomEvents.Add(defaultEvent);
                EventManager.SetActive("高考", true);
                EventManager.SaveCustomEvents();
                CountdownWindow.SaveDefaultStyleToEvent("高考");
                eventListBox.SelectedItem = defaultEvent;
            }
        }

        private void SaveEventBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEvent == null) return;
            string name = editEventName.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = LanguageManager.Instance["Unnamed"];
            DateTime? date = editEventDate.SelectedDate;
            if (!date.HasValue)
            {
                MessageBox.Show(LanguageManager.Instance["InvalidDateMsg"], LanguageManager.Instance["InfoTitle"]);
                return;
            }

            string oldName = _selectedEvent.Name;
            bool wasActive = EventManager.IsActiveEvent(oldName);

            // 如果改名了且窗口活跃，需要更新窗口管理器的 key
            if (wasActive && oldName != name)
            {
                CountdownWindow.HideWindow(oldName);
                EventManager.SetActive(oldName, false);
            }

            _selectedEvent.Name = name;
            _selectedEvent.Date = date.Value;
            eventListBox.Items.Refresh();
            EventManager.SaveCustomEvents();

            // 重新创建窗口（如果之前活跃）
            if (wasActive)
            {
                EventManager.SetActive(name, true);
                _selectedEvent.IsActive = true;
                CountdownWindow.ShowWindow(name, date.Value);
            }
            else
            {
                // 窗口不活跃时更新配置即可
                var w = CountdownWindow.GetWindow(name);
                if (w != null) w.UpdateEvent(name, date.Value);
            }
        }

        // ── 颜色选择器 ──

        private void OpenColorDialogBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var colorDialog = new Forms.ColorDialog { FullOpen = true })
            {
                colorDialog.Color = Drawing.Color.FromArgb(
                    confirmedColor.A, confirmedColor.R, confirmedColor.G, confirmedColor.B);

                if (colorDialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    var c = colorDialog.Color;
                    currentPreviewColor = Color.FromArgb(c.A, c.R, c.G, c.B);
                    confirmedColor = currentPreviewColor;
                    UpdatePreview();
                    var w = SelectedWindow;
                    if (w != null) w.SetTextColor(confirmedColor);
                    else if (_selectedEvent != null)
                        ConfigManager.WriteString(ConfigManager.EventConfigPath(_selectedEvent.Name), "Style", "TextColor", confirmedColor.ToString());
                }
            }
        }

        private void ResetStyleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEvent == null) return;
            string cfg = ConfigManager.EventConfigPath(_selectedEvent.Name);

            // 写入默认值到事件配置
            ConfigManager.WriteDouble(cfg, "Style", "Opacity", 0.8);
            ConfigManager.WriteDouble(cfg, "Style", "FontSize", 24);
            ConfigManager.WriteString(cfg, "Style", "FontFamily", "Microsoft YaHei");
            ConfigManager.WriteString(cfg, "Style", "TextColor", "#FFFFFFFF");
            ConfigManager.WriteString(cfg, "Style", "Animation", "None");
            ConfigManager.WriteString(cfg, "Style", "BgImage", "");

            // 更新 UI
            confirmedColor = Color.FromRgb(255, 255, 255);
            currentPreviewColor = confirmedColor;
            UpdatePreview();
            opacitySlider.Value = 0.8;
            fontSizeSlider.Value = 24;
            fontFamilyCombo.SelectedItem = FindFontFamilyItem("Microsoft YaHei");
            _suppressAnimationWrite = true;
            foreach (ComboBoxItem item in animationCombo.Items)
                if (item.Tag?.ToString() == "None") { animationCombo.SelectedItem = item; break; }
            _suppressAnimationWrite = false;
            ClearBgBtn_Click(null, null);

            // 重载窗口（如果活跃）
            var w = SelectedWindow;
            if (w != null)
            {
                w.ReloadSettings();
                w.CenterOnScreen();
            }
        }

        // ── 辅助 ──

        private void ShowToast(string message)
        {
            _toastWindow?.Close();

            var toast = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Topmost = true
            };
            toast.Closed += (s, e) =>
            {
                _toastTimer?.Stop();
                _toastTimer = null;
                _toastWindow = null;
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 51, 51, 51)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 10, 18, 10)
            };
            border.Child = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14
            };
            toast.Content = border;
            _toastWindow = toast;
            toast.Show();

            toast.Opacity = 0;
            toast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));

            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _toastTimer.Tick += (s, e) =>
            {
                _toastTimer.Stop();
                var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
                fadeOut.Completed += (s2, e2) => toast.Close();
                toast.BeginAnimation(OpacityProperty, fadeOut);
            };
            _toastTimer.Start();
        }

        private void UpdatePreview()
        {
            colorPreview.Background = new SolidColorBrush(currentPreviewColor);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
