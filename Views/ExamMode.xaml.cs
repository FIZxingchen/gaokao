using System.Windows;

namespace gokao.Views
{
    /// <summary>
    /// ExamMode.xaml 的交互逻辑：考试模式入口窗口，
    /// 点击"考试倒计时"打开全屏考试倒计时窗口。
    /// </summary>
    public partial class ExamMode : Window
    {
        private ExamCountdownWindow _countdownWindow;
        private ExamNotesWindow _notesWindow;
        private PomodoroWindow _pomodoroWindow;

        public ExamMode()
        {
            InitializeComponent();
            // 复选框状态从全局配置加载；勾选/取消即时保存
            dailyQuoteCheck.IsChecked = DailyQuoteManager.IsEnabled();
            dailyQuoteCheck.Checked += (s, e) => DailyQuoteManager.SetEnabled(true);
            dailyQuoteCheck.Unchecked += (s, e) => DailyQuoteManager.SetEnabled(false);
        }

        private void ExamCountdownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_countdownWindow == null)
            {
                _countdownWindow = new ExamCountdownWindow();
                _countdownWindow.Closed += (s, args) => _countdownWindow = null;
                _countdownWindow.Show();
            }
            else if (!WindowHelper.IsShown(_countdownWindow))
            {
                WindowHelper.ShowActive(_countdownWindow);
            }
            else
            {
                _countdownWindow.Activate();
            }
        }

        private void ExamNotesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_notesWindow == null)
            {
                _notesWindow = new ExamNotesWindow();
                _notesWindow.Closed += (s, args) => _notesWindow = null;
                _notesWindow.Show();
            }
            else if (!WindowHelper.IsShown(_notesWindow))
            {
                WindowHelper.ShowActive(_notesWindow);
            }
            else
            {
                _notesWindow.Activate();
            }
        }

        private void PomodoroBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_pomodoroWindow == null)
            {
                _pomodoroWindow = new PomodoroWindow();
                _pomodoroWindow.Closed += (s, args) => _pomodoroWindow = null;
                _pomodoroWindow.Show();
            }
            else if (!WindowHelper.IsShown(_pomodoroWindow))
            {
                WindowHelper.ShowActive(_pomodoroWindow);
            }
            else
            {
                _pomodoroWindow.Activate();
            }
        }
    }
}
