using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace gokao.Views
{
    /// <summary>
    /// PomodoroWindow.xaml 的交互逻辑：番茄钟/学习计时窗口。
    /// 专注/休息时长可在窗口下方自定义（默认 25/5 分钟，1~180 范围），
    /// 应用后即时生效并保存到 usersetting.ini 的 [Pomodoro] 节。
    /// 支持开始/暂停/重置，倒计时结束响铃三声并给出下一阶段提示。
    /// </summary>
    public partial class PomodoroWindow : Window
    {
        private const int MinMinutes = 1;
        private const int MaxMinutes = 180;
        private const int DefaultFocusMinutes = 25;
        private const int DefaultBreakMinutes = 5;

        private int _focusMinutes = DefaultFocusMinutes;
        private int _breakMinutes = DefaultBreakMinutes;

        private readonly DispatcherTimer _timer;
        private TimeSpan _remaining;
        private bool _isFocusMode = true;
        private bool _running;
        private bool _finished;

        private static readonly Brush FocusBrush = MakeBrush("#1976D2");
        private static readonly Brush BreakBrush = MakeBrush("#43A047");
        private static readonly Brush InactiveBrush = MakeBrush("#BDBDBD");

        private static Brush MakeBrush(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        public PomodoroWindow()
        {
            InitializeComponent();
            // 从全局配置加载自定义时长，填入输入框
            _focusMinutes = LoadMinutes("FocusMinutes", DefaultFocusMinutes);
            _breakMinutes = LoadMinutes("BreakMinutes", DefaultBreakMinutes);
            focusMinBox.Text = _focusMinutes.ToString();
            breakMinBox.Text = _breakMinutes.ToString();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => OnTick();
            UpdateModeButtons();
            SetMode(true, reset: true);
        }

        /// <summary>读取配置中的分钟数，越界/非法时回退默认值并夹紧到 [1,180]</summary>
        private static int LoadMinutes(string key, int def)
        {
            double v = ConfigManager.ReadDouble("Pomodoro", key, def);
            int i = (int)Math.Round(v);
            if (i < MinMinutes || i > MaxMinutes) i = def;
            return i;
        }

        /// <summary>按当前配置刷新模式按钮文案（专注 X 分钟 / 休息 X 分钟）</summary>
        private void UpdateModeButtons()
        {
            var lang = LanguageManager.Instance;
            focusModeBtn.Content = $"{lang["PomodoroFocus"]} {_focusMinutes} {lang["PomodoroMinutes"]}";
            breakModeBtn.Content = $"{lang["PomodoroBreak"]} {_breakMinutes} {lang["PomodoroMinutes"]}";
        }

        private void OnTick()
        {
            _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
                _timer.Stop();
                _running = false;
                _finished = true;
                UpdateTime();
                PlayChime();

                if (_isFocusMode)
                {
                    // 专注结束 → 自动切换到休息模式
                    statusText.Text = LanguageManager.Instance["PomodoroFocusDone"];
                    SetMode(false, true);
                    // 显示提示语，让用户看到后再准备开始休息
                    statusText.Text = LanguageManager.Instance["PomodoroAutoBreakHint"];
                    startPauseBtn.Content = LanguageManager.Instance["PomodoroStart"];
                }
                else
                {
                    statusText.Text = LanguageManager.Instance["PomodoroBreakDone"];
                    startPauseBtn.Content = LanguageManager.Instance["PomodoroStart"];
                }
                return;
            }
            UpdateTime();
        }

        /// <summary>结束响铃三声（异步播放，不阻塞 UI）</summary>
        private async void PlayChime()
        {
            for (int i = 0; i < 3; i++)
            {
                System.Media.SystemSounds.Exclamation.Play();
                await System.Threading.Tasks.Task.Delay(500);
            }
        }

        /// <summary>切换模式并（可选）重置计时：更新按钮高亮与剩余时间</summary>
        private void SetMode(bool focus, bool reset)
        {
            _isFocusMode = focus;
            focusModeBtn.Background = focus ? FocusBrush : InactiveBrush;
            breakModeBtn.Background = focus ? InactiveBrush : BreakBrush;
            if (reset)
            {
                _timer.Stop();
                _running = false;
                _finished = false;
                _remaining = TimeSpan.FromMinutes(focus ? _focusMinutes : _breakMinutes);
                startPauseBtn.Content = LanguageManager.Instance["PomodoroStart"];
                statusText.Text = "";
            }
            UpdateTime();
        }

        private void FocusModeBtn_Click(object sender, RoutedEventArgs e) => SetMode(true, true);

        private void BreakModeBtn_Click(object sender, RoutedEventArgs e) => SetMode(false, true);

        private void StartPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_running)
            {
                // 暂停
                _timer.Stop();
                _running = false;
                startPauseBtn.Content = LanguageManager.Instance["PomodoroStart"];
                return;
            }
            if (_finished)
            {
                // 结束后再点开始：回到当前模式的完整时长
                _finished = false;
                _remaining = TimeSpan.FromMinutes(_isFocusMode ? _focusMinutes : _breakMinutes);
                statusText.Text = "";
            }
            if (_remaining <= TimeSpan.Zero)
                _remaining = TimeSpan.FromMinutes(_isFocusMode ? _focusMinutes : _breakMinutes);
            _running = true;
            startPauseBtn.Content = LanguageManager.Instance["PomodoroPause"];
            _timer.Start();
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e) => SetMode(_isFocusMode, true);

        /// <summary>
        /// 应用自定义时长：校验输入（1~180 整数分钟），保存到全局配置并重置当前模式计时。
        /// </summary>
        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            bool okFocus = int.TryParse(focusMinBox.Text.Trim(), out int f);
            bool okBreak = int.TryParse(breakMinBox.Text.Trim(), out int b);
            if (!okFocus || !okBreak || f < MinMinutes || f > MaxMinutes || b < MinMinutes || b > MaxMinutes)
            {
                statusText.Text = LanguageManager.Instance["PomodoroInvalid"];
                return;
            }
            _focusMinutes = f;
            _breakMinutes = b;
            ConfigManager.WriteString("Pomodoro", "FocusMinutes", f.ToString());
            ConfigManager.WriteString("Pomodoro", "BreakMinutes", b.ToString());
            UpdateModeButtons();
            SetMode(_isFocusMode, true); // 按新时长重置当前模式
            statusText.Text = LanguageManager.Instance["PomodoroApplied"];
        }

        private void UpdateTime()
        {
            timeText.Text = string.Format("{0:00}:{1:00}", (int)_remaining.TotalMinutes, _remaining.Seconds);
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
