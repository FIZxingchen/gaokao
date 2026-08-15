using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace gokao
{
    public partial class CountdownWindow : Window
    {
        // ── 多实例窗口管理 ──
        private static readonly Dictionary<string, CountdownWindow> _activeWindows =
            new Dictionary<string, CountdownWindow>();

        /// <summary>获取指定事件的窗口实例（不存在返回 null）</summary>
        public static CountdownWindow GetWindow(string eventName)
        {
            if (eventName != null && _activeWindows.TryGetValue(eventName, out var w))
                return w;
            return null;
        }

        /// <summary>创建或显示指定事件的倒计时窗口</summary>
        public static CountdownWindow ShowWindow(string eventName, DateTime targetDate)
        {
            if (_activeWindows.TryGetValue(eventName, out var existing))
            {
                if (!WindowHelper.IsShown(existing))
                    WindowHelper.ShowActive(existing);
                return existing;
            }
            var window = new CountdownWindow(eventName, targetDate);
            window.Show();
            window.Focusable = false;
            _activeWindows[eventName] = window;
            return window;
        }

        /// <summary>隐藏并销毁指定事件的倒计时窗口</summary>
        public static void HideWindow(string eventName)
        {
            if (_activeWindows.TryGetValue(eventName, out var window))
            {
                window.SavePosition();
                window._countdownTimer?.Stop();
                window._forceClose = true;
                window.Close();
                _activeWindows.Remove(eventName);
            }
        }

        /// <summary>关闭所有倒计时窗口（程序退出时调用）</summary>
        public static void CloseAll()
        {
            foreach (var kvp in _activeWindows)
            {
                kvp.Value._countdownTimer?.Stop();
                kvp.Value._forceClose = true;
                kvp.Value.Close();
            }
            _activeWindows.Clear();
        }

        /// <summary>获取所有活跃窗口实例</summary>
        public static IEnumerable<CountdownWindow> ActiveWindows => _activeWindows.Values;

        /// <summary>是否有任何活跃窗口可见</summary>
        public static bool AnyVisible()
        {
            foreach (var w in _activeWindows.Values)
                if (WindowHelper.IsShown(w)) return true;
            return false;
        }

        /// <summary>显示所有活跃窗口（托盘切换用）</summary>
        public static void ShowAll()
        {
            foreach (var w in _activeWindows.Values)
                if (!WindowHelper.IsShown(w))
                    WindowHelper.ShowActive(w);
        }

        /// <summary>隐藏所有活跃窗口（托盘切换用）</summary>
        public static void HideAll()
        {
            foreach (var w in _activeWindows.Values)
                if (WindowHelper.IsShown(w))
                    w.Hide();
        }

        // ── 共享位图缓存（避免同一图片被多次解码占用内存）──
        private static readonly Dictionary<string, BitmapImage> _bitmapCache =
            new Dictionary<string, BitmapImage>();

        /// <summary>
        /// 获取共享的冻结 BitmapImage：同一路径只解码一次，
        /// 背景窗口与设置窗口预览共用同一份位图数据，内存不翻倍。
        /// 文件损坏/被占用时返回 null，不抛异常、不残留无效缓存。
        /// </summary>
        public static BitmapImage GetSharedBitmap(string path, double decodeWidth = 0)
        {
            string key = decodeWidth > 0 ? path + "|" + (int)decodeWidth : path;
            lock (_bitmapCache)
            {
                if (_bitmapCache.TryGetValue(key, out BitmapImage cached) && cached != null)
                    return cached;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    using (var stream = File.OpenRead(path))
                    {
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        // 按需缩小解码尺寸，大幅降低内存占用
                        if (decodeWidth > 0)
                            bitmap.DecodePixelWidth = (int)decodeWidth;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 冻结后跨窗口共享是线程安全的
                    }
                    _bitmapCache[key] = bitmap;
                    return bitmap;
                }
                catch
                {
                    // 解码失败不缓存，下次重试；避免无效条目占用缓存空间
                    return null;
                }
            }
        }

        /// <summary>按路径前缀清理共享位图缓存（删除事件时调用）</summary>
        public static void ClearBitmapCache(string pathPrefix)
        {
            lock (_bitmapCache)
            {
                var keys = new List<string>();
                foreach (var k in _bitmapCache.Keys)
                    if (k.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                        keys.Add(k);
                foreach (var k in keys)
                    _bitmapCache.Remove(k);
            }
        }

        // ── 字段 ──
        private DispatcherTimer _countdownTimer;
        private bool _isLocked;
        private ImageBrush _bgBrush;
        private string _eventName = "高考";
        public string CurrentEventName => _eventName;
        private DateTime _targetDate;
        private string _currentAnimType = "None";
        private bool _forceClose;

        // 已保存的窗口位置（构造函数中读取，Window_Loaded 中应用）
        private bool _hasSavedPosition;
        private double _savedLeft;
        private double _savedTop;

        // Win32 窗口样式常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // 翻页数字样式
        private double _flipFontSize = 72;
        private Color _flipTextColor = Colors.White;
        private string _flipFontFamily = "Arial, Microsoft YaHei";
        private double _flipScale = 1.0;

        // 用户设置的基准文字颜色（呼吸动画提亮的起点，避免读到被动画改动的 brush 颜色）
        private Color _baseTextColor = Colors.White;
        // 呼吸动画 Storyboard（首次构建后缓存复用，仅更新颜色值）
        private System.Windows.Media.Animation.Storyboard _breathStory;
        private System.Windows.Media.Animation.DoubleAnimation _breathScaleX;
        private System.Windows.Media.Animation.DoubleAnimation _breathScaleY;
        private System.Windows.Media.Animation.ColorAnimation _breathColorAnim;
        private bool _breathBuilt;
        // 流光渐变动画 Storyboard（代码动态构建，颜色跟随用户设置）
        private System.Windows.Media.Animation.Storyboard _flowStory;

        // 赛博风动画
        private DispatcherTimer _glitchTimer;
        private DispatcherTimer _glitchFlashTimer;
        private DispatcherTimer _neonFlickerTimer;
        private int _glitchPhase;
        private static readonly Random _glitchRnd = new Random();

        // 打字机效果（倒计时归零时触发）
        private DispatcherTimer _typewriterTimer;
        private string _typewriterFullMsg;
        private int _typewriterCharIndex;
        private bool _isTypewriting;

        // 冰晶动画
        private DispatcherTimer _iceCrackTimer;
        private System.Windows.Media.Animation.Storyboard _snowStory;

        private const double DefaultOpacity = 0.8;
        private const double DefaultFontSize = 24;
        private const string DefaultFontFamily = "Microsoft YaHei";
        private const string DefaultTextColor = "#FFFFFFFF";

        // ── 构造函数 ──
        internal CountdownWindow(string eventName, DateTime targetDate)
        {
            _eventName = eventName;
            _targetDate = targetDate;
            InitializeComponent();
            LoadSettings();
            InitializeTimer();

            // 语言切换后立即刷新倒计时窗口文案（标题/天时分秒/结束语）
            LanguageManager.Instance.LanguageChanged += (s, args) => UpdateCountdown();

            // 窗口隐藏时暂停计时器，显示时恢复，避免不可见时持续占用 CPU
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    if (!_countdownTimer.IsEnabled)
                    {
                        _countdownTimer.Start();
                        UpdateCountdown();
                    }
                }
                else
                {
                    _countdownTimer.Stop();
                }
            };
        }

        private void InitializeTimer()
        {
            _countdownTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                (s, e) => UpdateCountdown(),
                Dispatcher);
            _countdownTimer.Start();
        }

        // ── 配置加载 ──
        private void LoadSettings()
        {
            // _eventName 和 _targetDate 由构造函数传入，不再从全局配置读取
            LoadBackground();
            LoadPosition();
            LoadStyle();
            LoadAnimation();
            UpdateCountdown();
        }

        /// <summary>
        /// 仅重新加载背景图片（不重置位置/样式/动画），避免全量 ReloadSettings 的副作用。
        /// </summary>
        public void ReloadBackground()
        {
            ApplyBackground();
        }

        /// <summary>
        /// 重新加载所有设置（用于事件名/日期变更后刷新窗口）。
        /// _eventName 由外部通过 UpdateEvent 更新，此处只重新读取事件配置。
        /// </summary>
        public void ReloadSettings()
        {
            // 重新加载各项设置（使用当前事件名读取事件级配置）
            ApplyBackground();
            LoadPosition();
            LoadStyle();

            // 重新设置鼠标穿透状态
            string evtCfg = ConfigManager.EventConfigPath(_eventName);
            bool clickThrough = File.Exists(evtCfg)
                ? ConfigManager.ReadBool(evtCfg, "State", "ClickThrough", false)
                : ConfigManager.ReadBool("State", "ClickThrough", false);
            if (clickThrough)
                SetClickThrough(true);

            // 重新应用动画
            LoadAnimation();

            // 重启计时器（如果已停止）
            if (!_countdownTimer.IsEnabled)
                _countdownTimer.Start();

            UpdateCountdown();
        }

        /// <summary>更新事件名和日期后重载设置（编辑事件后调用）</summary>
        public void UpdateEvent(string eventName, DateTime targetDate)
        {
            _eventName = eventName;
            _targetDate = targetDate;
            ReloadSettings();
        }

        /// <summary>
        /// 仅更新背景刷子（运行时安全，不触碰 AllowsTransparency）。
        /// AllowsTransparency 在构造函数首次加载时设置后就不可再改。
        /// </summary>
        private void ApplyBackground()
        {
            // 只读取事件配置，不 fallback 到全局配置，确保每个事件背景独立
            string eventCfg = ConfigManager.EventConfigPath(_eventName);
            string bgPath = File.Exists(eventCfg)
                ? ConfigManager.ReadString(eventCfg, "Style", "BgImage", "")
                : "";
            bool hasBg = !string.IsNullOrEmpty(bgPath) && File.Exists(bgPath);
            if (hasBg)
            {
                try
                {
                    // 共享缓存 + 按屏幕宽度解码，避免重复解码占用双倍内存
                    double decodeWidth = SystemParameters.PrimaryScreenWidth * 1.5;
                    var bitmap = GetSharedBitmap(bgPath, decodeWidth);
                    _bgBrush = new ImageBrush(bitmap) { Opacity = Opacity };
                    Background = _bgBrush;
                }
                catch
                {
                    _bgBrush = null;
                    Background = null;
                }
            }
            else
            {
                _bgBrush = null;
                Background = null;
            }
        }

        /// <summary>
        /// 首次加载背景（构造函数中使用，允许设置 AllowsTransparency）。
        /// </summary>
        private void LoadBackground()
        {
            AllowsTransparency = true;  // 保持透明通道，仅构造时允许
            ApplyBackground();
        }

        private void LoadPosition()
        {
            _hasSavedPosition = false;
            string eventCfg = ConfigManager.EventConfigPath(_eventName);
            string rawLeft = ConfigManager.ReadString(eventCfg, "Position", "Left", "");
            string rawTop = ConfigManager.ReadString(eventCfg, "Position", "Top", "");

            if (!string.IsNullOrEmpty(rawLeft) && !string.IsNullOrEmpty(rawTop))
            {
                if (double.TryParse(rawLeft.Replace(',', '.'),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double left) &&
                    double.TryParse(rawTop.Replace(',', '.'),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double top))
                {
                    var workArea = SystemParameters.WorkArea;
                    if (left >= workArea.Left && left <= workArea.Right - 100 &&
                        top >= workArea.Top && top <= workArea.Bottom - 50)
                    {
                        _hasSavedPosition = true;
                        _savedLeft = left;
                        _savedTop = top;
                    }
                }
            }
        }

        private void LoadStyle()
        {
            // 直接读取事件级配置，文件不存在时 Win32 API 返回默认值
            string eventCfg = ConfigManager.EventConfigPath(_eventName);

            Opacity = Clamp(ConfigManager.ReadDouble(eventCfg, "Style", "Opacity", DefaultOpacity), 0.1, 1.0);

            double fontSize = Clamp(ConfigManager.ReadDouble(eventCfg, "Style", "FontSize", DefaultFontSize), 12, 72);
            countdownText.FontSize = fontSize;
            countdownText.Padding = new Thickness(fontSize * 0.6);
            _flipFontSize = fontSize * 3.0;
            flipTitle.FontSize = Math.Max(18, fontSize * 1.4);
            _flipScale = fontSize / DefaultFontSize;

            _isLocked = ConfigManager.ReadBool(eventCfg, "State", "IsLocked", false);
            string colorStr = ConfigManager.ReadString(eventCfg, "Style", "TextColor", DefaultTextColor);
            if (ColorConverter.ConvertFromString(colorStr) is Color color)
            {
                countdownText.Foreground = new SolidColorBrush(color);
                _flipTextColor = color;
                _baseTextColor = color;
                ApplyFlipLabelColors(color);
            }

            string fontFamily = ConfigManager.ReadString(eventCfg, "Style", "FontFamily", DefaultFontFamily);
            countdownText.FontFamily = SafeFontFamily(fontFamily);
            _flipFontFamily = SafeFontFamilyName(fontFamily);

            bool topmost = ConfigManager.ReadBool(eventCfg, "State", "Topmost", true);
            Topmost = topmost;

            ApplyFlipDigitStyle();
            ApplyFlipDigitScale();
        }

        /// <summary>
        /// 安全创建字体：空串/非法字体名时回退默认字体，避免 FontFamily 构造抛异常
        /// </summary>
        private static FontFamily SafeFontFamily(string fontFamily)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fontFamily))
                    return new FontFamily(DefaultFontFamily);
                var ff = new FontFamily(fontFamily);
                return ff;
            }
            catch
            {
                return new FontFamily(DefaultFontFamily);
            }
        }

        /// <summary>返回安全的字体名字符串（供配置保存使用）</summary>
        private static string SafeFontFamilyName(string fontFamily)
        {
            return string.IsNullOrWhiteSpace(fontFamily) ? DefaultFontFamily : fontFamily;
        }

        private void LoadAnimation()
        {
            string eventCfg = ConfigManager.EventConfigPath(_eventName);
            string anim = ConfigManager.ReadString(eventCfg, "Style", "Animation", "None");
            if (string.IsNullOrEmpty(anim)) anim = "None";
            _currentAnimType = anim;
            ApplyAnimationMode();
        }

        // ── 动画 ──
        private void ApplyAnimationMode()
        {
            StopBreathAnimation();
            StopFlowAnimation();
            StopCyberAnimation();
            StopIceAnimation();

            if (_currentAnimType == "None" || _currentAnimType == "Breath" || _currentAnimType == "Flow" || _currentAnimType == "Cyber" || _currentAnimType == "Ice")
            {
                countdownText.Visibility = Visibility.Visible;
                flipGrid.Visibility = Visibility.Collapsed;
                flipGrid.LayoutTransform = null;
                countdownText.RenderTransform = new ScaleTransform(1, 1);

                // 非翻页模式下释放翻页数字位图，回收内存（切换回翻页时自动重建）
                ReleaseFlipDigitImages();

                if (_currentAnimType == "Breath")
                    StartBreathAnimation();
                else if (_currentAnimType == "Flow")
                    StartFlowAnimation();
                else if (_currentAnimType == "Cyber")
                    StartCyberAnimation();
                else if (_currentAnimType == "Ice")
                    StartIceAnimation();
            }
            else if (_currentAnimType == "Flip")
            {
                countdownText.Visibility = Visibility.Collapsed;
                flipGrid.Visibility = Visibility.Visible;
                flipTitle.Text = string.Format(LanguageManager.Instance["CountdownTitleFormat"], _eventName);
                // 进入翻页模式时确保数字样式是最新的（非翻页模式下会跳过重建）
                ApplyFlipDigitStyle();
                ApplyFlipDigitScale();
            }

            // 触发 SizeToContent 重新计算窗口大小
            InvalidateMeasure();
        }

        // ── 呼吸动画（代码动态构建，颜色跟随用户设置） ──

        /// <summary>启动呼吸动画：缩放 1.0→1.04，颜色以用户设置的文字颜色为基准提亮</summary>
        private void StartBreathAnimation()
        {
            StopBreathAnimation();

            Color from = _baseTextColor;
            Color to = ComputeBreathToColor(from);

            if (!_breathBuilt)
            {
                // 首次调用时构建 Storyboard 及子动画，后续复用、仅更新颜色值
                _breathScaleX = new System.Windows.Media.Animation.DoubleAnimation(1.0, 1.04, TimeSpan.FromSeconds(2));
                System.Windows.Media.Animation.Storyboard.SetTarget(_breathScaleX, countdownText);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(
                    _breathScaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

                _breathScaleY = new System.Windows.Media.Animation.DoubleAnimation(1.0, 1.04, TimeSpan.FromSeconds(2));
                System.Windows.Media.Animation.Storyboard.SetTarget(_breathScaleY, countdownText);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(
                    _breathScaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

                _breathColorAnim = new System.Windows.Media.Animation.ColorAnimation(from, to, TimeSpan.FromSeconds(2));
                System.Windows.Media.Animation.Storyboard.SetTarget(_breathColorAnim, countdownText);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(
                    _breathColorAnim, new PropertyPath("(TextBlock.Foreground).(SolidColorBrush.Color)"));

                _breathStory = new System.Windows.Media.Animation.Storyboard
                {
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                    AutoReverse = true
                };
                _breathStory.Children.Add(_breathScaleX);
                _breathStory.Children.Add(_breathScaleY);
                _breathStory.Children.Add(_breathColorAnim);
                _breathBuilt = true;
            }
            else
            {
                // 后续调用：仅更新颜色动画的 From/To，避免重建整个 Storyboard
                _breathColorAnim.From = from;
                _breathColorAnim.To = to;
            }

            _breathStory.Begin();
        }

        /// <summary>停止呼吸动画并清理引用</summary>
        private void StopBreathAnimation()
        {
            if (_breathStory != null)
            {
                _breathStory.Stop();
                _breathStory = null;
            }
        }

        // ── 流光渐变动画（代码动态构建，颜色跟随用户设置） ──

        /// <summary>
        /// 启动星辰流光：静态底色 + 静态星辰柔光（Effect 只在每秒文字变化时重渲染一次，不卡顿），
        /// 少量黄色半透明小星星随机散落在窗口各处，从顶部缓缓飘落到底部，循环往复。
        /// 星星动画只走 RenderTransform（GPU 合成），流畅不卡。
        /// </summary>
        private void StartFlowAnimation()
        {
            StopFlowAnimation();

            Color baseColor = _baseTextColor;
            // 星辰蓝紫仅为流光模式的默认配色：默认白色文字时替换为星辰蓝紫（不影响其他动画模式）；
            // 用户自定义了文字颜色则跟随用户设置
            if (baseColor.R >= 240 && baseColor.G >= 240 && baseColor.B >= 240)
                baseColor = Color.FromRgb(123, 104, 238); // 星辰蓝紫 #7B68EE
            Color highlight = ComputeFlowHighlightColor(baseColor);

            // 静态底色 + 静态星辰柔光（光晕不逐帧动画，不卡顿）
            countdownText.Foreground = new SolidColorBrush(baseColor);
            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = highlight,
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.45
            };

            // 少量黄色半透明小星星：星星定位延迟到布局完成后执行——
            // 窗口隐藏/未测量时 MainGrid 宽度为 0，立即定位会把随机范围夹到 40px，星星全挤在左边
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (_currentAnimType != "Flow") return; // 已切换模式则不再启动星星
                double fallDistance = Math.Max(MainGrid.ActualHeight, 200);
                var rnd = new Random();
                foreach (var star in new[] { fallStar1, fallStar2, fallStar3, fallStar4, fallStar5, fallStar6 })
                {
                    star.Visibility = Visibility.Visible; // 星星颜色与拖尾已在 XAML 定义
                    var translate = new TranslateTransform(12 + rnd.NextDouble() * GetStarAreaWidth(), -40);
                    star.RenderTransform = translate;
                    StartStarFall(translate, rnd, fallDistance); // 独立下落，完成一轮换位置再来
                }
                // 星空背景微点
                foreach (var dot in new[] { bgDot1, bgDot2, bgDot3, bgDot4, bgDot5, bgDot6 })
                    dot.Visibility = Visibility.Visible;
                // 流星：偶尔斜向划过（扫完一轮随机间隔再来）
                comet1.Visibility = Visibility.Visible;
                comet2.Visibility = Visibility.Visible;
                SweepCometOnce(comet1Translate, rnd);
                SweepCometOnce(comet2Translate, rnd);
            }));

            var story = new System.Windows.Media.Animation.Storyboard();
            // 坠落同时轻微闪烁（保持半透明比较透的观感）
            AddStarTwinkle(story, fallStar1, 2.0, TimeSpan.Zero, 0.55);
            AddStarTwinkle(story, fallStar2, 2.6, TimeSpan.FromSeconds(1.0), 0.55);
            AddStarTwinkle(story, fallStar3, 2.2, TimeSpan.FromSeconds(2.0), 0.55);
            AddStarTwinkle(story, fallStar4, 2.8, TimeSpan.FromSeconds(3.0), 0.55);
            AddStarTwinkle(story, fallStar5, 2.4, TimeSpan.FromSeconds(4.0), 0.55);
            AddStarTwinkle(story, fallStar6, 2.1, TimeSpan.FromSeconds(5.0), 0.55);

            _flowStory = story;
            story.Begin();
        }

        /// <summary>
        /// 让一颗星从窗口上方缓缓落到窗口下方；落完一轮后在 Completed 里随机换一个
        /// 横向位置，再用随机速度重新下落（每颗星每轮位置都不同）。
        /// </summary>
        private void StartStarFall(TranslateTransform translate, Random rnd, double fallDistance)
        {
            var fall = new System.Windows.Media.Animation.DoubleAnimation(
                -40, fallDistance + 40, TimeSpan.FromSeconds(9 + rnd.NextDouble() * 4)) // 9~13 秒随机
            {
                FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop
            };
            fall.Completed += (s, e) =>
            {
                translate.X = 12 + rnd.NextDouble() * GetStarAreaWidth(); // 每轮按当前窗口宽度重新随机
                translate.Y = -40; // 回到窗口上方
                StartStarFall(translate, rnd, fallDistance); // 再次下落
            };
            translate.BeginAnimation(TranslateTransform.YProperty, fall);
        }

        /// <summary>星星随机横向范围：取窗口当前宽度（布局未完成时回退文字宽度，再兜底 300，避免挤在左边）</summary>
        private double GetStarAreaWidth()
        {
            double w = MainGrid.ActualWidth;
            if (w <= 1) w = countdownText.ActualWidth;
            if (w <= 1) w = 300;
            return Math.Max(w - 24, 40);
        }

        /// <summary>
        /// 让一颗流星斜向划过窗口：扫完一轮后随机等待 6~16 秒再来一颗。
        /// 全程只动画 TranslateTransform（GPU 合成），不重绘文字不卡顿。
        /// </summary>
        private async void SweepCometOnce(TranslateTransform cometTranslate, Random rnd)
        {
            double W = Math.Max(MainGrid.ActualWidth, 200);
            double H = Math.Max(MainGrid.ActualHeight, 120);
            double dur = 2.2 + rnd.NextDouble() * 1.2; // 2.2~3.4 秒扫过
            cometTranslate.BeginAnimation(TranslateTransform.XProperty,
                new System.Windows.Media.Animation.DoubleAnimation(-260, W + 260, TimeSpan.FromSeconds(dur)));
            cometTranslate.BeginAnimation(TranslateTransform.YProperty,
                new System.Windows.Media.Animation.DoubleAnimation(-110, H + 110, TimeSpan.FromSeconds(dur)));
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(dur)); // 等本轮扫完
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(6 + rnd.NextDouble() * 10)); // 随机间隔
            if (_currentAnimType == "Flow")
                SweepCometOnce(cometTranslate, rnd); // 再来一颗
        }

        /// <summary>为星星添加闪烁动画（透明度呼吸，GPU 合成开销极小）；maxOpacity 控制透明度上限（比较透）</summary>
        private static void AddStarTwinkle(System.Windows.Media.Animation.Storyboard story,
            FrameworkElement star, double seconds, TimeSpan beginTime, double maxOpacity)
        {
            var twinkle = new System.Windows.Media.Animation.DoubleAnimation(0.25, maxOpacity, TimeSpan.FromSeconds(seconds))
            {
                AutoReverse = true,
                BeginTime = beginTime,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(twinkle, star);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(twinkle, new PropertyPath(UIElement.OpacityProperty));
            story.Children.Add(twinkle);
        }

        /// <summary>计算流光高光色：白色系→暖金（比呼吸的淡黄更明显）；其他颜色→向白提亮 70%</summary>
        private static Color ComputeFlowHighlightColor(Color from)
        {
            // 白色系（含接近白色）用暖金色高光，在白色文字上清晰可见
            if (from.R >= 240 && from.G >= 240 && from.B >= 240)
                return Color.FromRgb(255, 214, 140);
            byte r = (byte)(from.R + (255 - from.R) * 0.7);
            byte g = (byte)(from.G + (255 - from.G) * 0.7);
            byte b = (byte)(from.B + (255 - from.B) * 0.7);
            return Color.FromRgb(r, g, b);
        }

        /// <summary>停止流光动画并恢复现场：隐藏流光带与星星、还原 XAML 默认的黑色描边阴影与配置底色</summary>
        private void StopFlowAnimation()
        {
            if (_flowStory != null)
            {
                _flowStory.Stop();
                _flowStory = null;
            }
            // 停止每颗星的独立下落动画并隐藏
            foreach (var star in new[] { fallStar1, fallStar2, fallStar3, fallStar4, fallStar5, fallStar6 })
            {
                if (star.RenderTransform is TranslateTransform tt)
                    tt.BeginAnimation(TranslateTransform.YProperty, null);
                star.Visibility = Visibility.Collapsed;
            }
            // 隐藏星空背景微点；停止流星动画并隐藏（SweepCometOnce 的等待续跑会因模式切换而中止）
            foreach (var dot in new[] { bgDot1, bgDot2, bgDot3, bgDot4, bgDot5, bgDot6 })
                dot.Visibility = Visibility.Collapsed;
            foreach (var t in new[] { comet1Translate, comet2Translate })
            {
                t.BeginAnimation(TranslateTransform.XProperty, null);
                t.BeginAnimation(TranslateTransform.YProperty, null);
            }
            comet1.Visibility = Visibility.Collapsed;
            comet2.Visibility = Visibility.Collapsed;
            // 还原默认外观（与其他动画模式一致）：黑色描边阴影 + 配置的文字颜色
            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                ShadowDepth = 0,
                BlurRadius = 8,
                Opacity = 0.6
            };
            countdownText.Foreground = new SolidColorBrush(_baseTextColor);
        }

        // ── 赛博风动画（RGB 分离 + 扫描线 + 霓虹闪烁 + 故障撕裂 + 色彩抖动） ──

        /// <summary>
        /// 启动赛博风：霓虹发光底色 + 扫描线遮罩 + 定时故障闪烁。
        /// 日常态：文字带霓虹辉光呼吸 + 扫描线，偶尔微闪。
        /// 故障态（每 1.5~3.5 秒触发一次，持续 ~500ms）：5 阶段 RGB 通道分离 → 水平撕裂 → 品红偏移 → 色彩反转 → 回归。
        /// </summary>
        private void StartCyberAnimation()
        {
            Color baseColor = _baseTextColor;
            if (baseColor.R >= 240 && baseColor.G >= 240 && baseColor.B >= 240)
                baseColor = Color.FromRgb(0, 255, 255); // 霓虹青

            countdownText.Foreground = new SolidColorBrush(baseColor);
            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = baseColor,
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            countdownText.RenderTransform = new TranslateTransform(0, 0);

            scanlineOverlay.Visibility = Visibility.Visible;
            SyncGlitchLayers();

            // 霓虹微闪定时器：每 3~6 秒短暂闪烁一次（模拟霓虹灯管接触不良）
            _neonFlickerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3 + _glitchRnd.NextDouble() * 3)
            };
            _neonFlickerTimer.Tick += (s, e) =>
            {
                _neonFlickerTimer.Stop();
                NeonFlicker();
            };
            _neonFlickerTimer.Start();

            // 故障定时器：随机间隔 1.5~3.5 秒触发一次
            _glitchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5 + _glitchRnd.NextDouble() * 2)
            };
            _glitchTimer.Tick += (s, e) =>
            {
                _glitchTimer.Stop();
                TriggerGlitch();
            };
            _glitchTimer.Start();
        }

        /// <summary>霓虹微闪：2 次快速 opacity 抖动，模拟霓虹灯管接触不良</summary>
        private void NeonFlicker()
        {
            var flicker = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(250)
            };
            flicker.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.Zero));
            flicker.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0.3, TimeSpan.FromMilliseconds(40)));
            flicker.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(80)));
            flicker.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0.5, TimeSpan.FromMilliseconds(120)));
            flicker.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(200)));

            flicker.Completed += (s, e) =>
            {
                if (_currentAnimType == "Cyber" && _neonFlickerTimer != null)
                {
                    _neonFlickerTimer.Interval = TimeSpan.FromSeconds(3 + _glitchRnd.NextDouble() * 3);
                    _neonFlickerTimer.Start();
                }
            };
            countdownText.BeginAnimation(OpacityProperty, flicker);
        }

        /// <summary>触发一次故障闪烁序列：5 阶段，每阶段 ~80ms</summary>
        private void TriggerGlitch()
        {
            _glitchPhase = 0;

            glitchRed.Visibility = Visibility.Visible;
            glitchCyan.Visibility = Visibility.Visible;
            glitchMagenta.Visibility = Visibility.Visible;
            SyncGlitchLayers();

            _glitchFlashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _glitchFlashTimer.Tick += (s, e) =>
            {
                ApplyGlitchPhase(_glitchPhase);
                _glitchPhase++;
                if (_glitchPhase >= 5)
                {
                    _glitchFlashTimer.Stop();
                    _glitchFlashTimer = null;
                    StopGlitchFlash();
                }
            };
            _glitchFlashTimer.Start();
        }

        /// <summary>应用故障闪烁的某一阶段</summary>
        private void ApplyGlitchPhase(int phase)
        {
            double sign = _glitchRnd.Next(2) == 0 ? -1 : 1;

            // 每阶段不同的偏移模式
            double mainOffset, redOffset, cyanOffset, magentaOffset;
            Color baseFlash;

            if (phase == 0)
            {
                // 阶段 1：小幅度 RGB 分离
                mainOffset = sign * 2;
                redOffset = -3; cyanOffset = 3; magentaOffset = 0;
                baseFlash = Color.FromRgb(255, 0, 51);
            }
            else if (phase == 1)
            {
                // 阶段 2：大幅度撕裂
                mainOffset = sign * (5 + _glitchRnd.NextDouble() * 4);
                redOffset = -6; cyanOffset = 6; magentaOffset = sign * 3;
                baseFlash = Color.FromRgb(0, 255, 255);
                ShowGlitchBand();
            }
            else if (phase == 2)
            {
                // 阶段 3：品红主导
                mainOffset = -sign * 3;
                redOffset = 2; cyanOffset = -2; magentaOffset = sign * 5;
                baseFlash = Color.FromRgb(255, 0, 255);
                ShowGlitchBand();
            }
            else if (phase == 3)
            {
                // 阶段 4：快速抖动
                mainOffset = sign * (1 + _glitchRnd.NextDouble() * 2);
                redOffset = -2; cyanOffset = 2; magentaOffset = -sign * 2;
                baseFlash = Color.FromRgb(255, 255, 0);
            }
            else
            {
                // 阶段 5：收敛回归
                mainOffset = sign * 0.5;
                redOffset = -1; cyanOffset = 1; magentaOffset = 0;
                baseFlash = _baseTextColor;
            }

            if (countdownText.RenderTransform is TranslateTransform tt)
                tt.X = mainOffset;
            if (glitchRed.RenderTransform is TranslateTransform ttR)
                ttR.X = redOffset;
            if (glitchCyan.RenderTransform is TranslateTransform ttC)
                ttC.X = cyanOffset;
            if (glitchMagenta.RenderTransform is TranslateTransform ttM)
                ttM.X = magentaOffset;

            // 主文字颜色在故障期间染色
            if (phase < 4)
                countdownText.Foreground = new SolidColorBrush(baseFlash);

            // 偶尔垂直偏移（数据错行感）
            if (_glitchRnd.Next(3) == 0)
            {
                double vY = (_glitchRnd.NextDouble() - 0.5) * 4;
                if (glitchCyan.RenderTransform is TranslateTransform ttCV)
                    ttCV.Y = vY;
                if (glitchRed.RenderTransform is TranslateTransform ttRV)
                    ttRV.Y = -vY;
            }
        }

        /// <summary>显示故障撕裂带（随机位置的水平亮条，持续 ~150ms）</summary>
        private void ShowGlitchBand()
        {
            double yPos = _glitchRnd.NextDouble() * Math.Max(MainGrid.ActualHeight - 10, 50);
            double bandHeight = 3 + _glitchRnd.NextDouble() * 6;
            glitchBand.Height = bandHeight;
            glitchBand.Margin = new Thickness(0, yPos, 0, 0);
            glitchBand.Visibility = Visibility.Visible;
            glitchBand.Opacity = 0.8;

            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(0.8, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) => glitchBand.Visibility = Visibility.Collapsed;
            glitchBand.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>停止故障闪烁，恢复正常态</summary>
        private void StopGlitchFlash()
        {
            glitchRed.Visibility = Visibility.Collapsed;
            glitchCyan.Visibility = Visibility.Collapsed;
            glitchMagenta.Visibility = Visibility.Collapsed;
            glitchBand.Visibility = Visibility.Collapsed;

            if (countdownText.RenderTransform is TranslateTransform tt)
            { tt.X = 0; tt.Y = 0; }
            if (glitchRed.RenderTransform is TranslateTransform ttR)
                ttR.Y = 0;
            if (glitchCyan.RenderTransform is TranslateTransform ttC)
                ttC.Y = 0;
            if (glitchMagenta.RenderTransform is TranslateTransform ttM)
                ttM.X = 0;

            // 恢复霓虹底色
            Color baseColor = _baseTextColor;
            if (baseColor.R >= 240 && baseColor.G >= 240 && baseColor.B >= 240)
                baseColor = Color.FromRgb(0, 255, 255);
            countdownText.Foreground = new SolidColorBrush(baseColor);

            if (_currentAnimType == "Cyber" && _glitchTimer != null)
            {
                _glitchTimer.Interval = TimeSpan.FromSeconds(1.5 + _glitchRnd.NextDouble() * 2);
                _glitchTimer.Start();
            }
        }

        /// <summary>同步 RGB 分离层文字内容、字体、字号、对齐方式</summary>
        private void SyncGlitchLayers()
        {
            glitchRed.Text = countdownText.Text;
            glitchCyan.Text = countdownText.Text;
            glitchMagenta.Text = countdownText.Text;
            glitchRed.FontFamily = countdownText.FontFamily;
            glitchCyan.FontFamily = countdownText.FontFamily;
            glitchMagenta.FontFamily = countdownText.FontFamily;
            glitchRed.FontSize = countdownText.FontSize;
            glitchCyan.FontSize = countdownText.FontSize;
            glitchMagenta.FontSize = countdownText.FontSize;
            glitchRed.FontWeight = countdownText.FontWeight;
            glitchCyan.FontWeight = countdownText.FontWeight;
            glitchMagenta.FontWeight = countdownText.FontWeight;
        }

        /// <summary>停止赛博风动画并清理资源</summary>
        private void StopCyberAnimation()
        {
            if (_glitchTimer != null) { _glitchTimer.Stop(); _glitchTimer = null; }
            if (_glitchFlashTimer != null) { _glitchFlashTimer.Stop(); _glitchFlashTimer = null; }
            if (_neonFlickerTimer != null) { _neonFlickerTimer.Stop(); _neonFlickerTimer = null; }

            StopTypewriter();

            glitchRed.Visibility = Visibility.Collapsed;
            glitchCyan.Visibility = Visibility.Collapsed;
            glitchMagenta.Visibility = Visibility.Collapsed;
            glitchBand.Visibility = Visibility.Collapsed;
            scanlineOverlay.Visibility = Visibility.Collapsed;

            countdownText.BeginAnimation(OpacityProperty, null);
            countdownText.Opacity = 1.0;

            if (countdownText.RenderTransform is TranslateTransform tt)
            { tt.X = 0; tt.Y = 0; }
            if (glitchRed.RenderTransform is TranslateTransform ttR)
                ttR.Y = 0;
            if (glitchCyan.RenderTransform is TranslateTransform ttC)
                ttC.Y = 0;
            if (glitchMagenta.RenderTransform is TranslateTransform ttM)
                ttM.X = 0;

            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                ShadowDepth = 0,
                BlurRadius = 8,
                Opacity = 0.6
            };
            countdownText.Foreground = new SolidColorBrush(_baseTextColor);
        }

        // ── 打字机效果（倒计时归零时逐字打出祝福语） ──

        /// <summary>
        /// 启动打字机效果：每 80ms 逐字显示祝福语，光标闪烁。
        /// 打完后光标持续闪烁，文字保持冰蓝色辉光。
        /// </summary>
        private void StartTypewriter(string fullMsg)
        {
            // 停止所有动画效果，切回纯净态
            StopBreathAnimation();
            StopFlowAnimation();
            StopCyberAnimation();
            StopIceAnimation();

            _isTypewriting = true;
            _typewriterFullMsg = fullMsg;
            _typewriterCharIndex = 0;
            countdownText.Text = "";

            // 冰蓝色辉光（打字机归零时统一使用冰蓝色）
            Color iceBlue = Color.FromRgb(180, 230, 255);
            countdownText.Foreground = new SolidColorBrush(iceBlue);
            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(100, 200, 255),
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = 0.8
            };
            countdownText.RenderTransform = new TranslateTransform(0, 0);

            _typewriterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };
            _typewriterTimer.Tick += (s, e) =>
            {
                if (_typewriterCharIndex < _typewriterFullMsg.Length)
                {
                    _typewriterCharIndex++;
                    // 逐字显示，末尾带闪烁光标 ▎
                    countdownText.Text = _typewriterFullMsg.Substring(0, _typewriterCharIndex) + "▎";
                }
                else
                {
                    // 打字完成：停止定时器，启动光标闪烁
                    _typewriterTimer.Stop();
                    _typewriterTimer = null;
                    StartCursorBlink();
                }
            };
            _typewriterTimer.Start();
        }

        /// <summary>光标闪烁：每 500ms 切换 ▎ 显示/隐藏</summary>
        private void StartCursorBlink()
        {
            var blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            blinkTimer.Tick += (s, e) =>
            {
                if (!_isTypewriting)
                {
                    blinkTimer.Stop();
                    return;
                }
                string baseText = _typewriterFullMsg;
                if (countdownText.Text == baseText + "▎")
                    countdownText.Text = baseText + " ";
                else
                    countdownText.Text = baseText + "▎";
            };
            blinkTimer.Start();
        }

        /// <summary>停止打字机效果</summary>
        private void StopTypewriter()
        {
            _isTypewriting = false;
            if (_typewriterTimer != null)
            {
                _typewriterTimer.Stop();
                _typewriterTimer = null;
            }
        }

        // ── 冰晶动画（蓝白色冰冻光晕 + 冰裂纹闪烁 + 微光呼吸） ──

        /// <summary>
        /// 冰晶模式：文字带蓝白色冰冻辉光，缓慢呼吸闪烁，
        /// 每 3~6 秒随机出现冰裂纹效果（文字短暂偏移 + 白色闪烁条）。
        /// <summary>
        /// 冰晶模式：冰蓝辉光 + 倾斜缓慢裂纹 + 雪花飘落。
        /// 日常态：文字带蓝白冰冻辉光，雪花从顶部缓缓飘落。
        /// 裂纹态（每 4~8 秒）：文字沿倾斜裂纹线分裂 → 缓慢裂开 → 愈合。
        /// </summary>
        private void StartIceAnimation()
        {
            Color baseColor = _baseTextColor;
            if (baseColor.R >= 240 && baseColor.G >= 240 && baseColor.B >= 240)
                baseColor = Color.FromRgb(200, 240, 255); // 冰蓝白 #C8F0FF

            countdownText.Foreground = new SolidColorBrush(baseColor);
            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(120, 200, 255),
                BlurRadius = 22,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            countdownText.RenderTransform = new TranslateTransform(0, 0);

            // 启动雪花飘落
            StartSnowfall();

            // 裂纹定时器：随机间隔 4~8 秒触发
            _iceCrackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4 + _glitchRnd.NextDouble() * 4)
            };
            _iceCrackTimer.Tick += (s, e) =>
            {
                _iceCrackTimer.Stop();
                TriggerIceCrack();
            };
            _iceCrackTimer.Start();
        }

        /// <summary>启动雪花飘落：8 片雪花从窗口顶部缓缓飘落，循环往复</summary>
        private void StartSnowfall()
        {
            var snows = new[] { snow1, snow2, snow3, snow4, snow5, snow6, snow7, snow8 };
            double fallDistance = Math.Max(MainGrid.ActualHeight, 200);
            if (fallDistance <= 1) fallDistance = 300;
            var rnd = new Random();

            foreach (var snow in snows)
                snow.Visibility = Visibility.Visible;

            _snowStory = new System.Windows.Media.Animation.Storyboard();

            for (int i = 0; i < snows.Length; i++)
            {
                var snow = snows[i];
                var translate = new TranslateTransform(0, -20);
                snow.RenderTransform = translate;
                StartSnowFall(translate, rnd, fallDistance, i);
            }
        }

        /// <summary>单片雪花缓缓飘落 + 轻微左右摇摆</summary>
        private void StartSnowFall(TranslateTransform translate, Random rnd, double fallDistance, int index)
        {
            double startX = 10 + rnd.NextDouble() * GetStarAreaWidth();
            translate.X = startX;
            translate.Y = -20;

            // 缓慢下落（8~14 秒），轻微左右摇摆
            double duration = 8 + rnd.NextDouble() * 6;
            var fallY = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = -20, To = fallDistance + 30,
                Duration = TimeSpan.FromSeconds(duration),
                EasingFunction = new System.Windows.Media.Animation.SineEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };
            fallY.FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop;
            fallY.Completed += (s, e) =>
            {
                if (_currentAnimType != "Ice") return;
                translate.X = 10 + rnd.NextDouble() * GetStarAreaWidth();
                translate.Y = -20;
                StartSnowFall(translate, rnd, fallDistance, index);
            };

            // 左右摇摆
            double swayAmp = 8 + rnd.NextDouble() * 12;
            double swayDur = 2 + rnd.NextDouble() * 2;
            var swayX = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(swayDur),
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            swayX.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(startX, TimeSpan.Zero));
            swayX.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(startX + swayAmp, TimeSpan.FromMilliseconds(swayDur * 500)));
            swayX.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(startX - swayAmp, TimeSpan.FromMilliseconds(swayDur * 750)));
            swayX.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(startX, TimeSpan.FromSeconds(swayDur)));

            translate.BeginAnimation(TranslateTransform.YProperty, fallY);
            translate.BeginAnimation(TranslateTransform.XProperty, swayX);
        }

        /// <summary>
        /// 冰裂纹效果：文字沿倾斜裂纹线分裂，缓慢裂开 → 震颤 → 愈合，~1.2 秒。
        /// 裂纹线是斜向的（左上到右下或右上到左下），裂开过程缓慢有层次感。
        /// </summary>
        private void TriggerIceCrack()
        {
            double textW = Math.Max(countdownText.ActualWidth, 100);
            double textH = Math.Max(countdownText.ActualHeight, 30);

            // 生成 1 条倾斜裂纹线的 Y 起点
            double crackStartY = textH * (0.3 + _glitchRnd.NextDouble() * 0.4);
            double tiltAngle = (_glitchRnd.NextDouble() - 0.5) * 0.5; // 斜率：-0.25~0.25

            Color iceWhite = Color.FromRgb(220, 245, 255);

            // 复用 glitchRed = 上半，glitchCyan = 下半
            var layers = new[] { glitchRed, glitchCyan };
            for (int i = 0; i < 2; i++)
            {
                var layer = layers[i];
                layer.Text = countdownText.Text;
                layer.FontFamily = countdownText.FontFamily;
                layer.FontSize = countdownText.FontSize;
                layer.FontWeight = countdownText.FontWeight;
                layer.Foreground = new SolidColorBrush(iceWhite);
                layer.Visibility = Visibility.Visible;

                // 沿倾斜线裁剪：上半和下半
                // 用两个点定义倾斜裁剪线，然后分割上下
                if (i == 0)
                {
                    // 上半：从 (-5, crackStartY - tiltAngle * 5) 到 (textW+5, crackStartY + tiltAngle * textW)
                    var path = new PathGeometry();
                    path.FillRule = FillRule.EvenOdd;
                    var fig = new PathFigure { StartPoint = new Point(-5, -5) };
                    fig.Segments.Add(new LineSegment(new Point(textW + 10, -5), true));
                    fig.Segments.Add(new LineSegment(new Point(textW + 10, crackStartY + tiltAngle * (textW + 10)), true));
                    fig.Segments.Add(new LineSegment(new Point(-5, crackStartY - tiltAngle * 5), true));
                    fig.IsClosed = true;
                    path.Figures.Add(fig);
                    layer.Clip = path;
                }
                else
                {
                    var path = new PathGeometry();
                    var fig = new PathFigure { StartPoint = new Point(-5, crackStartY - tiltAngle * 5) };
                    fig.Segments.Add(new LineSegment(new Point(textW + 10, crackStartY + tiltAngle * (textW + 10)), true));
                    fig.Segments.Add(new LineSegment(new Point(textW + 10, textH + 5), true));
                    fig.Segments.Add(new Point(-5, textH + 5) is var p ? new LineSegment(p, true) : null);
                    fig.IsClosed = true;
                    path.Figures.Add(fig);
                    layer.Clip = path;
                }
            }
            glitchMagenta.Visibility = Visibility.Collapsed;

            // 主文字变暗
            countdownText.Opacity = 0.2;

            // 绘制倾斜锯齿裂纹线 + 冰屑
            DrawTiltedCrackLine(crackStartY, tiltAngle, textW, textH);

            // 闪白
            Color originalColor = _baseTextColor;
            if (originalColor.R >= 240 && originalColor.G >= 240 && originalColor.B >= 240)
                originalColor = Color.FromRgb(200, 240, 255);
            countdownText.Foreground = new SolidColorBrush(Colors.White);

            // 阶段 1：缓慢裂开（0→400ms，各带逐渐偏移）
            if (glitchRed.RenderTransform is TranslateTransform ttR)
            {
                var animX = new System.Windows.Media.Animation.DoubleAnimation(0, -4 - _glitchRnd.NextDouble() * 4, TimeSpan.FromMilliseconds(400));
                var animY = new System.Windows.Media.Animation.DoubleAnimation(0, -3 - _glitchRnd.NextDouble() * 3, TimeSpan.FromMilliseconds(400));
                ttR.BeginAnimation(TranslateTransform.XProperty, animX);
                ttR.BeginAnimation(TranslateTransform.YProperty, animY);
            }
            if (glitchCyan.RenderTransform is TranslateTransform ttC)
            {
                var animX = new System.Windows.Media.Animation.DoubleAnimation(0, 4 + _glitchRnd.NextDouble() * 4, TimeSpan.FromMilliseconds(400));
                var animY = new System.Windows.Media.Animation.DoubleAnimation(0, 3 + _glitchRnd.NextDouble() * 3, TimeSpan.FromMilliseconds(400));
                ttC.BeginAnimation(TranslateTransform.XProperty, animX);
                ttC.BeginAnimation(TranslateTransform.YProperty, animY);
            }

            // 阶段 2：震颤（500ms 后）
            var shudderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            shudderTimer.Tick += (s2, e2) =>
            {
                shudderTimer.Stop();
                double dir = _glitchRnd.Next(2) == 0 ? -1 : 1;
                if (glitchRed.RenderTransform is TranslateTransform ttR2)
                { ttR2.X += dir * 1.5; ttR2.Y += dir * 1; }
                if (glitchCyan.RenderTransform is TranslateTransform ttC2)
                { ttC2.X -= dir * 1.5; ttC2.Y -= dir * 1; }
            };
            shudderTimer.Start();

            // 阶段 3：愈合（900ms 后恢复）
            var healTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            healTimer.Tick += (s, e) =>
            {
                healTimer.Stop();

                glitchRed.Visibility = Visibility.Collapsed;
                glitchCyan.Visibility = Visibility.Collapsed;
                glitchMagenta.Visibility = Visibility.Collapsed;
                glitchRed.Clip = null;
                glitchCyan.Clip = null;
                glitchMagenta.Clip = null;

                if (glitchRed.RenderTransform is TranslateTransform ttR3) { ttR3.X = 0; ttR3.Y = 0; }
                if (glitchCyan.RenderTransform is TranslateTransform ttC3) { ttC3.X = 0; ttC3.Y = 0; }
                if (glitchMagenta.RenderTransform is TranslateTransform ttM3) { ttM3.X = 0; ttM3.Y = 0; }
                if (countdownText.RenderTransform is TranslateTransform tt) tt.X = 0;

                crackCanvas.Children.Clear();
                crackCanvas.Visibility = Visibility.Collapsed;

                countdownText.Opacity = 1.0;
                countdownText.Foreground = new SolidColorBrush(originalColor);
            };
            healTimer.Start();

            // 重新启动裂纹定时器
            if (_currentAnimType == "Ice" && _iceCrackTimer != null)
            {
                _iceCrackTimer.Interval = TimeSpan.FromSeconds(4 + _glitchRnd.NextDouble() * 4);
                _iceCrackTimer.Start();
            }
        }

        /// <summary>绘制倾斜锯齿裂纹线 + 冰晶碎屑</summary>
        private void DrawTiltedCrackLine(double baseY, double tilt, double textW, double textH)
        {
            crackCanvas.Children.Clear();
            crackCanvas.Visibility = Visibility.Visible;

            // 倾斜锯齿裂纹线
            var points = new PointCollection();
            double x = -5;
            double y = baseY - tilt * 5;
            points.Add(new Point(x, y));
            while (x < textW + 10)
            {
                x += 8 + _glitchRnd.NextDouble() * 14;
                y = baseY + tilt * x + (_glitchRnd.NextDouble() - 0.5) * 12;
                points.Add(new Point(x, y));
            }

            // 外层蓝色辉光
            var glowLine = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(Color.FromArgb(60, 120, 200, 255)),
                StrokeThickness = 5,
                IsHitTestVisible = false
            };
            crackCanvas.Children.Add(glowLine);

            // 主裂纹线
            var crackLine = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                StrokeThickness = 1.5,
                StrokeDashCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };
            crackCanvas.Children.Add(crackLine);

            // 分支裂纹
            if (_glitchRnd.Next(2) == 0)
            {
                int branchStart = _glitchRnd.Next(1, Math.Max(2, points.Count - 1));
                var branchPts = new PointCollection { points[branchStart] };
                double bx = points[branchStart].X;
                double by = points[branchStart].Y;
                for (int j = 0; j < 2 + _glitchRnd.Next(3); j++)
                {
                    bx += 5 + _glitchRnd.NextDouble() * 8;
                    by += (_glitchRnd.NextDouble() - 0.3) * 14;
                    branchPts.Add(new Point(bx, by));
                }
                var branch = new Polyline
                {
                    Points = branchPts,
                    Stroke = new SolidColorBrush(Color.FromArgb(140, 200, 240, 255)),
                    StrokeThickness = 1,
                    StrokeDashCap = PenLineCap.Round,
                    IsHitTestVisible = false
                };
                crackCanvas.Children.Add(branch);
            }

            // 冰晶碎屑（沿裂纹线散布）
            int sparkles = 3 + _glitchRnd.Next(4);
            for (int i = 0; i < sparkles; i++)
            {
                double sx = _glitchRnd.NextDouble() * textW;
                double sy = baseY + tilt * sx + (_glitchRnd.NextDouble() - 0.5) * 16;
                double size = 1.5 + _glitchRnd.NextDouble() * 2;

                var sparkle = new System.Windows.Shapes.Path
                {
                    Data = new GeometryGroup
                    {
                        Children =
                        {
                            new LineGeometry(new Point(sx - size, sy), new Point(sx + size, sy)),
                            new LineGeometry(new Point(sx, sy - size), new Point(sx, sy + size))
                        }
                    },
                    Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    IsHitTestVisible = false
                };
                crackCanvas.Children.Add(sparkle);
            }
        }

        /// <summary>停止冰晶动画并清理资源</summary>
        private void StopIceAnimation()
        {
            if (_iceCrackTimer != null) { _iceCrackTimer.Stop(); _iceCrackTimer = null; }
            if (_snowStory != null) { _snowStory.Stop(); _snowStory = null; }

            countdownText.BeginAnimation(OpacityProperty, null);
            countdownText.Opacity = 1.0;
            glitchBand.Visibility = Visibility.Collapsed;
            crackCanvas.Children.Clear();
            crackCanvas.Visibility = Visibility.Collapsed;

            // 隐藏雪花
            foreach (var snow in new[] { snow1, snow2, snow3, snow4, snow5, snow6, snow7, snow8 })
            {
                if (snow.RenderTransform is TranslateTransform ttS)
                {
                    ttS.BeginAnimation(TranslateTransform.YProperty, null);
                    ttS.BeginAnimation(TranslateTransform.XProperty, null);
                }
                snow.Visibility = Visibility.Collapsed;
            }

            // 清除分裂层
            glitchRed.Visibility = Visibility.Collapsed;
            glitchCyan.Visibility = Visibility.Collapsed;
            glitchMagenta.Visibility = Visibility.Collapsed;
            glitchRed.Clip = null;
            glitchCyan.Clip = null;
            glitchMagenta.Clip = null;
            if (glitchRed.RenderTransform is TranslateTransform ttR) { ttR.X = 0; ttR.Y = 0; }
            if (glitchCyan.RenderTransform is TranslateTransform ttC) { ttC.X = 0; ttC.Y = 0; }
            if (glitchMagenta.RenderTransform is TranslateTransform ttM) { ttM.X = 0; ttM.Y = 0; }

            if (countdownText.RenderTransform is TranslateTransform tt)
                tt.X = 0;

            countdownText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                ShadowDepth = 0,
                BlurRadius = 8,
                Opacity = 0.6
            };
            countdownText.Foreground = new SolidColorBrush(_baseTextColor);
        }

        /// <summary>
        /// 计算呼吸动画的提亮目标色：
        /// 白色/近白色 → 保持原有"白色→淡黄"闪烁观感；
        /// 其他颜色 → 向白色方向提亮 45%，柔和呼吸且不覆盖用户配色。
        /// </summary>
        private static Color ComputeBreathToColor(Color from)
        {
            // 白色系（含接近白色）沿用原观感
            if (from.R >= 240 && from.G >= 240 && from.B >= 240)
                return Colors.LightYellow;

            byte r = (byte)(from.R + (255 - from.R) * 0.45);
            byte g = (byte)(from.G + (255 - from.G) * 0.45);
            byte b = (byte)(from.B + (255 - from.B) * 0.45);
            return Color.FromRgb(r, g, b);
        }

        // ── 倒计时更新 ──
        public void UpdateCountdown()
        {
            var remaining = _targetDate - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                string msg;
                if (_eventName == "高考")
                    msg = LanguageManager.Instance["GaokaoCheer"];
                else
                    msg = string.Format(LanguageManager.Instance["EventStarted"], _eventName);

                _countdownTimer.Stop();

                if (_currentAnimType == "Flip")
                {
                    flipTitle.Text = msg;
                    SetFlipDigits(0, 0, 0, 0);
                }
                else
                {
                    // 打字机效果：逐字打出祝福语
                    StartTypewriter(msg);
                }
                return;
            }

            int days = remaining.Days;
            int hours = remaining.Hours;
            int minutes = remaining.Minutes;
            int seconds = remaining.Seconds;

            if (_currentAnimType == "Flip")
            {
                SetFlipDigits(days, hours, minutes, seconds);
                // 标题内容不变时跳过赋值，避免每次 tick 触发布局
                string title = string.Format(LanguageManager.Instance["CountdownTitleFormat"], _eventName);
                if (flipTitle.Text != title)
                    flipTitle.Text = title;
            }
            else
            {
                // 文本内容不变时跳过赋值（秒数不变或定时器抖动时省一次布局）
                string text = string.Format(LanguageManager.Instance["CountdownTextFormat"], _eventName, days, hours, minutes, seconds);
                if (countdownText.Text != text)
                {
                    countdownText.Text = text;
                    // 赛博风模式下同步 RGB 分离层文字
                    if (_currentAnimType == "Cyber")
                        SyncGlitchLayers();
                }
            }
        }

        private void SetFlipDigits(int days, int hours, int minutes, int seconds)
        {
            dayHundreds.Value = (days / 100) % 10;
            dayTens.Value   = (days / 10) % 10;
            dayOnes.Value   = days % 10;
            hourTens.Value  = (hours / 10) % 10;
            hourOnes.Value  = hours % 10;
            minuteTens.Value  = (minutes / 10) % 10;
            minuteOnes.Value  = minutes % 10;
            secondTens.Value  = (seconds / 10) % 10;
            secondOnes.Value  = seconds % 10;
        }

        // ── 公共设置接口（封装内部控件） ──
        /// <summary>将当前窗口样式保存到指定事件的配置文件中</summary>
        public void SaveCurrentStyleToEvent(string eventName)
        {
            string cfg = ConfigManager.EventConfigPath(eventName);
            // 外观
            ConfigManager.WriteDouble(cfg, "Style", "Opacity", Opacity);
            ConfigManager.WriteDouble(cfg, "Style", "FontSize", countdownText.FontSize);
            ConfigManager.WriteString(cfg, "Style", "FontFamily", _flipFontFamily);
            // 用 _baseTextColor 取基准色：呼吸/流光动画会改动 Foreground 画笔（流光模式甚至是渐变画刷），读 brush 拿不到用户颜色
            string colorStr = _baseTextColor.ToString();
            ConfigManager.WriteString(cfg, "Style", "TextColor", colorStr);
            ConfigManager.WriteString(cfg, "Style", "Animation", _currentAnimType);
            // 背景图片 — 每个事件独立，只从事件自身配置读取，不继承全局背景
            string evtCfg = ConfigManager.EventConfigPath(eventName);
            string bgPath = ConfigManager.ReadString(evtCfg, "Style", "BgImage", "");
            ConfigManager.WriteString(cfg, "Style", "BgImage", bgPath);
            // 行为
            ConfigManager.WriteBool(cfg, "State", "IsLocked", _isLocked);
            ConfigManager.WriteBool(cfg, "State", "Topmost", Topmost);
            bool clickThrough = (GetWindowLong(
                new System.Windows.Interop.WindowInteropHelper(this).Handle, GWL_EXSTYLE) & WS_EX_TRANSPARENT) != 0;
            ConfigManager.WriteBool(cfg, "State", "ClickThrough", clickThrough);
            // 位置
            ConfigManager.WriteDouble(cfg, "Position", "Left", Left);
            ConfigManager.WriteDouble(cfg, "Position", "Top", Top);
        }

        /// <summary>将窗口移到屏幕中央（工作区居中）并保存位置，恢复默认样式时调用</summary>
        public void CenterOnScreen()
        {
            var area = SystemParameters.WorkArea;
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 2;
            SavePosition(); // 同时写入全局与事件配置，重启后保持居中
        }

        /// <summary>
        /// 将默认样式写入指定事件的配置文件（新建事件时调用，不继承当前窗口样式）。
        /// </summary>
        public static void SaveDefaultStyleToEvent(string eventName)
        {
            string cfg = ConfigManager.EventConfigPath(eventName);
            // 外观 — 使用默认值
            ConfigManager.WriteDouble(cfg, "Style", "Opacity", DefaultOpacity);
            ConfigManager.WriteDouble(cfg, "Style", "FontSize", DefaultFontSize);
            ConfigManager.WriteString(cfg, "Style", "FontFamily", DefaultFontFamily);
            ConfigManager.WriteString(cfg, "Style", "TextColor", DefaultTextColor);
            ConfigManager.WriteString(cfg, "Style", "Animation", "None");
            ConfigManager.WriteString(cfg, "Style", "BgImage", "");
            // 行为 — 默认值
            ConfigManager.WriteBool(cfg, "State", "IsLocked", false);
            ConfigManager.WriteBool(cfg, "State", "Topmost", true);
            ConfigManager.WriteBool(cfg, "State", "ClickThrough", false);
        }

        /// <summary>
        /// 补齐事件配置中缺失的键（写入默认值），确保事件配置自洽，
        /// 切换事件时绝不因缺键回退到全局配置而串带上一事件的样式。
        /// 已存在的键不会被覆盖；置顶/鼠标穿透保持全局语义，不写入事件配置。
        /// </summary>
        public static void EnsureDefaultStyleToEvent(string eventName)
        {
            string cfg = ConfigManager.EventConfigPath(eventName);
            const string missing = "__GOKAO_KEY_MISSING__";
            // 外观 — 仅补齐缺失的键
            if (ConfigManager.ReadString(cfg, "Style", "Opacity", missing) == missing)
                ConfigManager.WriteDouble(cfg, "Style", "Opacity", DefaultOpacity);
            if (ConfigManager.ReadString(cfg, "Style", "FontSize", missing) == missing)
                ConfigManager.WriteDouble(cfg, "Style", "FontSize", DefaultFontSize);
            if (ConfigManager.ReadString(cfg, "Style", "FontFamily", missing) == missing)
                ConfigManager.WriteString(cfg, "Style", "FontFamily", DefaultFontFamily);
            if (ConfigManager.ReadString(cfg, "Style", "TextColor", missing) == missing)
                ConfigManager.WriteString(cfg, "Style", "TextColor", DefaultTextColor);
            if (ConfigManager.ReadString(cfg, "Style", "Animation", missing) == missing)
                ConfigManager.WriteString(cfg, "Style", "Animation", "None");
            if (ConfigManager.ReadString(cfg, "Style", "BgImage", missing) == missing)
                ConfigManager.WriteString(cfg, "Style", "BgImage", "");
            // 行为 — 锁定按事件保存；缺键时默认未锁定
            if (ConfigManager.ReadString(cfg, "State", "IsLocked", missing) == missing)
                ConfigManager.WriteBool(cfg, "State", "IsLocked", false);
        }

        public void SetOpacity(double opacity)
        {
            Opacity = Clamp(opacity, 0.1, 1.0);
            // 背景图片透明度跟随滑块，实现内容与背景同步淡入淡出
            if (_bgBrush != null)
                _bgBrush.Opacity = Clamp(opacity, 0.1, 1.0);
            // 同步保存到事件配置
            string cfg = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteDouble(cfg, "Style", "Opacity", opacity);
        }

        public void SetFontSize(double fontSize)
        {
            fontSize = Clamp(fontSize, 12, 72);

            // 记录当前窗口中心，缩放后保持中心不变（居中缩放）
            double centerX = Left + ActualWidth / 2;
            double centerY = Top + ActualHeight / 2;

            countdownText.FontSize = fontSize;
            // 呼吸动画最大缩放 1.04x，Padding 留够空间避免溢出
            countdownText.Padding = new Thickness(fontSize * 0.6);

            _flipFontSize = Clamp(fontSize * 3.0, 36, 216);
            flipTitle.FontSize = Clamp(fontSize * 1.4, 18, 100);
            _flipScale = fontSize / DefaultFontSize;

            // 仅翻页模式需要重建翻页数字位图；实时重建保证拖动立即生效
            if (_currentAnimType == "Flip")
            {
                ApplyFlipDigitStyle();
                ApplyFlipDigitScale();
            }
            else
                ApplyFlipDigitScale(); // 非翻页模式只刷新布局，不重建位图

            // 赛博风模式下同步 RGB 分离层字号
            if (_currentAnimType == "Cyber")
                SyncGlitchLayers();

            // 同步保存到事件配置
            string cfgFs = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteDouble(cfgFs, "Style", "FontSize", fontSize);

            // 强制布局更新，然后重新定位使窗口保持居中
            UpdateLayout();
            Left = centerX - ActualWidth / 2;
            Top = centerY - ActualHeight / 2;
        }

        public void SetFontFamily(string fontFamily)
        {
            countdownText.FontFamily = new FontFamily(fontFamily);
            _flipFontFamily = fontFamily;

            // 同步翻页模式的标题和中文标签字体
            var ff = new FontFamily(fontFamily);
            flipTitle.FontFamily = ff;
            flipLabelDays.FontFamily = ff;
            flipLabelHours.FontFamily = ff;
            flipLabelMinutes.FontFamily = ff;
            flipLabelSeconds.FontFamily = ff;

            // 仅翻页模式需要重建翻页数字位图
            if (_currentAnimType == "Flip")
                ApplyFlipDigitStyle();

            // 赛博风模式下同步 RGB 分离层字体
            if (_currentAnimType == "Cyber")
                SyncGlitchLayers();

            // 同步保存到事件配置
            string cfgFf = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteString(cfgFf, "Style", "FontFamily", fontFamily);
        }

        /// <summary>
        /// 设置鼠标穿透：勾选后鼠标点击可穿透倒计时窗口操作后方程序
        /// </summary>
        public void SetClickThrough(bool enable)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hWnd = helper.Handle;
            if (hWnd == IntPtr.Zero) return;

            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if (enable)
                exStyle |= WS_EX_TRANSPARENT;
            else
                exStyle &= ~WS_EX_TRANSPARENT;
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
        }

        
        public void SetTextColor(Color color)
        {
            countdownText.Foreground = new SolidColorBrush(color);
            _flipTextColor = color;
            _baseTextColor = color;

            // 同步翻页模式的标题和中文标签
            ApplyFlipLabelColors(color);

            // 呼吸/流光/赛博风模式下立即用新颜色重启动画，跟随用户设置
            if (_currentAnimType == "Breath")
                StartBreathAnimation();
            else if (_currentAnimType == "Flow")
                StartFlowAnimation();
            else if (_currentAnimType == "Cyber")
                StartCyberAnimation();
            else if (_currentAnimType == "Ice")
                StartIceAnimation();

            // 仅翻页模式需要重建翻页数字位图
            if (_currentAnimType == "Flip")
                ApplyFlipDigitStyle();

            // 同步保存到事件配置
            string cfgTc = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteString(cfgTc, "Style", "TextColor", color.ToString());
        }

        /// <summary>将文字颜色同步到翻页模式的标题与天/时/分/秒标签</summary>
        private void ApplyFlipLabelColors(Color color)
        {
            var brush = new SolidColorBrush(color);
            flipTitle.Foreground = brush;
            flipLabelDays.Foreground = brush;
            flipLabelHours.Foreground = brush;
            flipLabelMinutes.Foreground = brush;
            flipLabelSeconds.Foreground = brush;
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            // 锁定状态按事件保存，避免串带到其他事件
            string cfg = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteBool(cfg, "State", "IsLocked", locked);
        }

        public void SetTopmost(bool topmost)
        {
            Topmost = topmost;
            ConfigManager.WriteBool("State", "Topmost", topmost);
        }

        public void SetAnimation(string animType)
        {
            _currentAnimType = animType;
            // 同步保存到事件配置（不再写入全局，避免动画类型被串带到其他事件）
            string cfg = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteString(cfg, "Style", "Animation", animType);
            ApplyAnimationMode();
            UpdateCountdown();
        }

        /// <summary>
        /// 将字体颜色/字号同步到所有翻页数字控件
        /// </summary>
        private void ApplyFlipDigitStyle()
        {
            dayHundreds.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            dayTens.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            dayOnes.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            hourTens.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            hourOnes.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            minuteTens.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            minuteOnes.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            secondTens.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
            secondOnes.ApplyStyle(_flipFontSize, _flipTextColor, _flipFontFamily);
        }

        /// <summary>
        /// 释放全部翻页数字位图（非翻页模式调用，回收内存）
        /// </summary>
        private void ReleaseFlipDigitImages()
        {
            dayHundreds.ReleaseImages();
            dayTens.ReleaseImages();
            dayOnes.ReleaseImages();
            hourTens.ReleaseImages();
            hourOnes.ReleaseImages();
            minuteTens.ReleaseImages();
            minuteOnes.ReleaseImages();
            secondTens.ReleaseImages();
            secondOnes.ReleaseImages();
        }

        /// <summary>
        /// 翻页模式下触发 SizeToContent 重新计算窗口大小
        /// </summary>
        private void ApplyFlipDigitScale()
        {
            // 翻页数字控件的物理尺寸已在 FlipDigitControl.ApplyStyle() 中自动缩放
            // 此处仅刷新布局确保窗口自适应
            InvalidateMeasure();
        }

        // ── 辅助 ──
        private void SavePosition()
        {
            ConfigManager.WriteDouble("Position", "Left", Left);
            ConfigManager.WriteDouble("Position", "Top", Top);
            // 同步保存到事件配置
            string cfg = ConfigManager.EventConfigPath(_eventName);
            ConfigManager.WriteDouble(cfg, "Position", "Left", Left);
            ConfigManager.WriteDouble(cfg, "Position", "Top", Top);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        // ── 事件 ──
        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isLocked)
                DragMove();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_forceClose)
            {
                base.OnClosing(e);
                return;
            }
            e.Cancel = true;
            Hide();
            SavePosition();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口句柄已创建，此时设置 Left/Top 才生效
            if (_hasSavedPosition)
            {
                Left = _savedLeft;
                Top = _savedTop;
                WindowStartupLocation = WindowStartupLocation.Manual;
            }

            // 从事件配置恢复鼠标穿透状态
            string evtCfg = ConfigManager.EventConfigPath(_eventName);
            if (File.Exists(evtCfg) && ConfigManager.ReadBool(evtCfg, "State", "ClickThrough", false))
                SetClickThrough(true);

            InvalidateMeasure();
        }
    }
}
