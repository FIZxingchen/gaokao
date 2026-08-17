using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace gokao.Views
{
    /// <summary>
    /// QuoteBubbleWindow.xaml 的交互逻辑：每日一句励志气泡窗口。
    /// 无边框圆角卡片，右下角淡入，停留数秒后淡出自动关闭，不抢焦点、不进任务栏。
    /// </summary>
    public partial class QuoteBubbleWindow : Window
    {
        private const int DisplaySeconds = 8;

        public QuoteBubbleWindow(string quote)
        {
            InitializeComponent();
            quoteText.Text = quote;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 定位到工作区右下角（含任务栏区域之外）
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - ActualWidth - 24;
            Top = wa.Bottom - ActualHeight - 24;

            // 淡入
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350)));

            // 停留后淡出并关闭
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(DisplaySeconds) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
                fadeOut.Completed += (o, a) => Close();
                BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }
    }
}
