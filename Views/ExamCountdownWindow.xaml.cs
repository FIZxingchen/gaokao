using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using gokao;

namespace gokao.Views
{
    /// <summary>
    /// 全屏考试倒计时窗口：黑底白字大数字，鼠标滚轮设置考试时长，
    /// 开始/暂停/关闭三个低调按钮，Esc 也可关闭。用于考试时清晰显示剩余时分秒。
    /// </summary>
    public partial class ExamCountdownWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _clockTimer; // 左下角实时时钟
        private int _setHours;         // 设定时长：小时（0~9，与分钟无关联）
        private int _setMinutes;       // 设定时长：分钟（0~59，与小时无关联）
        private TimeSpan _remaining;   // 当前剩余时间（倒计时过程中时分秒正常进制）
        private bool _running;
        private bool _started;         // 是否已开始过（进入倒计时后滚轮/滑动不再调整）
        private bool _finished;
        private Point _touchStart;     // 触屏滑动起点（设置阶段调整时长）
        private bool _touchTracking;   // 是否正在跟踪一次触屏滑动
        private bool _touchOnHours;    // 本次滑动起点在小时数字上（否则为分钟）

        // 滑动灵敏度：每约 160 像素调整 1 个单位（触屏滑动与左键拖动共用，适应触屏白板）
        private const double DragUnitPixels = 140;

        // ── 阻止系统睡眠/关屏（考试期间白板/电脑不自动休眠） ──
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        // 左键拖动设置时长（未开始阶段）
        private Point _mouseStart;      // 左键按下位置
        private int _mouseAppliedUnits; // 已应用的累计滑动单位（拖动过程增量调整）
        private bool _mouseTracking;    // 是否正在跟踪一次左键拖动
        private bool _mouseOnHours;     // 本次拖动起点在小时数字上（否则为分钟）

        public ExamCountdownWindow()
        {
            InitializeComponent();
            _setHours = 2;   // 默认 2 小时，小时/分钟可分别用滚轮/滑动/左键拖动调整
            _setMinutes = 30; // 默认 30 分钟
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => OnTick();
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            UpdateClock();
            UpdateTimeText();
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
                hintText.Visibility = Visibility.Collapsed;
                timePanel.Visibility = Visibility.Collapsed;
                timeUpText.Visibility = Visibility.Visible;
                PlayFinishChime(); // 结束响铃，提醒停止答卷
                return;
            }
            UpdateTimeText();
        }

        /// <summary>考试结束：连续响铃三声（异步播放，不阻塞 UI）</summary>
        private async void PlayFinishChime()
        {
            for (int i = 0; i < 3; i++)
            {
                System.Media.SystemSounds.Exclamation.Play();
                await System.Threading.Tasks.Task.Delay(500);
            }
        }

        private void UpdateTimeText()
        {
            // 倒计时中按剩余秒数显示（时分秒正常进制借位）；设置阶段按独立的时分显示
            if (_started && !_finished)
            {
                hoursText.Text = ((int)_remaining.TotalHours).ToString("00");
                minutesText.Text = _remaining.Minutes.ToString("00");
                secondsText.Text = _remaining.Seconds.ToString("00");
            }
            else
            {
                hoursText.Text = _setHours.ToString("00");
                minutesText.Text = _setMinutes.ToString("00");
                secondsText.Text = "00";
            }
        }

        /// <summary>更新左下角实时时钟（显示当前真实时间）</summary>
        private void UpdateClock()
        {
            clockText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_finished)
            {
                // 结束后重新开始：回到设定的小时/分钟
                _remaining = TimeSpan.FromHours(_setHours) + TimeSpan.FromMinutes(_setMinutes);
                _finished = false;
                timeUpText.Visibility = Visibility.Collapsed;
                timePanel.Visibility = Visibility.Visible;
            }
            if (_running) return;
            if (!_started)
            {
                // 首次开始：由独立的小时/分钟合成总时长
                _started = true;
                _remaining = TimeSpan.FromHours(_setHours) + TimeSpan.FromMinutes(_setMinutes);
            }
            if (_remaining <= TimeSpan.Zero) return; // 未设置时长
            _running = true;
            hintText.Visibility = Visibility.Collapsed;
            ApplyRunningLayout(); // 开始后：数字放大、标语与按钮拉开距离
            _timer.Start();
        }

        /// <summary>开始计时后：倒计时数字放大，标语与按钮拉开距离，便于远处观看</summary>
        private void ApplyRunningLayout()
        {
            double h = Math.Max(ActualHeight, 600);
            double bigSize = Math.Max(120, h / 4.5);
            TextElement.SetFontSize(timePanel, bigSize);
            timeUpText.FontSize = bigSize;
            sloganText.Margin = new Thickness(0, 0, 0, 64);
            buttonsPanel.Margin = new Thickness(0, 120, 0, 0);
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_running) return;
            _timer.Stop();
            _running = false;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 仅未开始的设置阶段可调：滚轮在"小时"数字上调小时，其余位置调分钟
            if (_running || _started || _finished) return;
            int d = e.Delta > 0 ? 1 : -1;
            if (ReferenceEquals(e.Source, hoursText)) AdjustHours(d);
            else AdjustMinutes(d);
        }

        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            if (_running || _started || _finished) return;
            if (e.Source is Button) return; // 按钮自己处理点击，不作为滑动起点
            _touchOnHours = ReferenceEquals(e.Source, hoursText);
            _touchStart = e.GetTouchPoint(this).Position;
            _touchTracking = true;
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            if (!_touchTracking) return;
            _touchTracking = false;
            if (_running || _started || _finished) return;
            var end = e.GetTouchPoint(this).Position;
            // 上滑增加、下滑减少；灵敏度较慢：每约 160 像素才调 1 个单位（适应触屏白板）
            int units = (int)Math.Round((_touchStart.Y - end.Y) / DragUnitPixels);
            if (units == 0) return;
            if (_touchOnHours) AdjustHours(units);
            else AdjustMinutes(units);
        }

        // ── 左键拖动设置时长（与触屏滑动同一套灵敏度与语义） ──
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_running || _started || _finished) return;
            if (e.Source is Button) return; // 按钮自己处理点击，不作为拖动起点
            _mouseOnHours = ReferenceEquals(e.Source, hoursText);
            _mouseStart = e.GetPosition(this);
            _mouseAppliedUnits = 0;
            _mouseTracking = true;
            CaptureMouse(); // 捕获后移出窗口也能继续拖动
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseTracking) return;
            var pos = e.GetPosition(this);
            // 上移增加、下移减少；按累计单位计算增量，拖动过程实时刷新数字
            int total = (int)Math.Round((_mouseStart.Y - pos.Y) / DragUnitPixels);
            if (total == _mouseAppliedUnits) return;
            int delta = total - _mouseAppliedUnits;
            _mouseAppliedUnits = total;
            if (_mouseOnHours) AdjustHours(delta);
            else AdjustMinutes(delta);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_mouseTracking) return;
            _mouseTracking = false;
            ReleaseMouseCapture();
        }

        private void Window_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _mouseTracking = false; // 捕获意外丢失（切窗口/关闭）时结束本次拖动
        }

        private void AdjustHours(int hours)
        {
            // 小时独立调整（0~9），不影响分钟
            _setHours += hours;
            if (_setHours < 0) _setHours = 0;
            if (_setHours > 9) _setHours = 9;
            UpdateTimeText();
        }

        private void AdjustMinutes(int minutes)
        {
            // 分钟独立调整（0~59 循环）：满 60 回 00、减到负数回 59，均不影响小时
            _setMinutes = (_setMinutes + minutes) % 60;
            if (_setMinutes < 0) _setMinutes += 60;
            UpdateTimeText();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 左下角实时时钟随窗口启动
            _clockTimer.Start();
            // 考试期间阻止系统睡眠与关屏（窗口关闭时恢复）
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
            // 大屏（希沃/科大讯飞白板等）自适应：按屏幕高度缩放字号与按钮，保证远处清晰可见
            double h = Math.Max(ActualHeight, 600);
            double timeSize = Math.Max(100, h / 6.0);
            TextElement.SetFontSize(timePanel, timeSize); // 附加属性继承到各数字 TextBlock
            timeUpText.FontSize = timeSize;
            sloganText.FontSize = Math.Max(28, h / 30.0);
            hintText.FontSize = Math.Max(16, h / 60.0);
            double btnH = Math.Max(40, h / 26.0);
            double btnFont = Math.Max(15, h / 68.0);
            foreach (var b in new[] { startBtn, pauseBtn, closeBtn })
            {
                b.Height = btnH;
                b.FontSize = btnFont;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            _clockTimer.Stop();
            // 恢复系统默认的睡眠/关屏行为
            SetThreadExecutionState(ES_CONTINUOUS);
            base.OnClosed(e);
        }
    }
}
