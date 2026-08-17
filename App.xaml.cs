using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace gokao
{
    public partial class App : Application
    {
        private MainWindow mainWindow;
        private TrayIconManager trayManager;
        private const string MutexName = "F5E5A5B5-3D5E-4F5A-9B5C-1D5E7F8A9B0C";
        private System.Threading.Mutex _mutex;
        private bool _mutexOwned;

        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterGlobalExceptionHandlers();

            // 单实例：带 --event 参数时通过命名管道通知已运行实例切换事件（通知成功说明已有实例，提示后退出）；
            // 无参数双击 exe 保持原行为——不唤起窗口，仅由互斥锁判断重复并提示退出。
            string command = BuildCommand(e.Args);
            if (command != null && SingleInstanceServer.TrySendCommand(command))
            {
                ShowAutoCloseMessage(LanguageManager.Instance["AlreadyRunning"], LanguageManager.Instance["InfoTitle"], 3000);
                Current.Shutdown();
                return;
            }

            _mutex = new System.Threading.Mutex(true, MutexName, out bool createdNew);
            _mutexOwned = createdNew;
            if (!createdNew)
            {
                // 极短竞态窗口：管道未建立而互斥锁已被占用
                ShowAutoCloseMessage(LanguageManager.Instance["AlreadyRunning"], LanguageManager.Instance["InfoTitle"], 3000);
                Current.Shutdown();
                return;
            }

            try
            {
                base.OnStartup(e);
                mainWindow = new MainWindow();
                mainWindow.Hide();
                mainWindow.ShowInTaskbar = true;

                // 启动单实例命令管道服务器
                SingleInstanceServer.Start(HandleSingleInstanceCommand);

                trayManager = new TrayIconManager(mainWindow);
                if (ConfigManager.ReadBool("State", "ShowStartupTip", true))
                    trayManager.ShowStartupTip();
                WallpaperScheduler.Start();
                DailyQuoteManager.ShowIfDue();

                // 为所有活跃事件创建倒计时窗口
                mainWindow.CreateActiveWindows();
            }
            catch (Exception ex)
            {
                LogManager.Log(ex, "程序启动失败");
                MessageBox.Show(string.Format(LanguageManager.Instance["StartupFailed"], ex.Message),
                    LanguageManager.Instance["ErrorTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        /// <summary>
        /// 根据启动参数构造发给已运行实例的命令：
        /// 仅 --event/-e 事件名 → Event:事件名（切换事件）；无参数返回 null（不通知，保持原提示退出行为）。
        /// </summary>
        private static string BuildCommand(string[] args)
        {
            if (args != null && args.Length >= 2 && (args[0] == "--event" || args[0] == "-e"))
                return "Event:" + args[1];
            return null;
        }

        /// <summary>
        /// 处理来自二次启动实例的命令（在 UI 线程执行）：
        /// Event:名称 → 切换事件（无参数双击已不发送命令，保持原提示退出行为）。
        /// </summary>
        private void HandleSingleInstanceCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            try
            {
                if (command.StartsWith("Event:", StringComparison.Ordinal))
                {
                    string name = command.Substring("Event:".Length);
                    SwitchEvent(name);
                }
            }
            catch (Exception ex)
            {
                LogManager.Log(ex, "处理单实例命令失败");
            }
        }

        /// <summary>确保设置窗口可见并激活（窗口可能被关闭、隐藏或最小化）</summary>
        private void EnsureMainWindowVisible()
        {
            if (mainWindow == null)
            {
                mainWindow = new MainWindow();
                mainWindow.Show();
                mainWindow.ShowInTaskbar = true;
            }
            else if (!WindowHelper.IsShown(mainWindow))
            {
                WindowHelper.ShowActive(mainWindow);
            }
            else
            {
                mainWindow.Activate();
            }
        }

        /// <summary>
        /// 切换到指定事件：激活该事件的倒计时窗口（多窗口模式）
        /// </summary>
        private void SwitchEvent(string name)
        {
            var evt = EventManager.CustomEvents.FirstOrDefault(x => x.Name == name);
            if (evt == null)
            {
                EnsureMainWindowVisible();
                return;
            }

            // 激活该事件的窗口
            evt.IsActive = true;
            EventManager.SetActive(name, true);
            CountdownWindow.ShowWindow(name, evt.Date);
            EnsureMainWindowVisible();
        }

        /// <summary>
        /// 注册全局异常捕获：UI 线程、AppDomain、后台 Task 三层兜底，
        /// 异常写入日志文件（logs/ 目录）并弹窗提示用户。
        /// </summary>
        private void RegisterGlobalExceptionHandlers()
        {
            // 1. UI 线程未处理异常（WPF Dispatcher）
            DispatcherUnhandledException += (sender, args) =>
            {
                LogManager.Log(args.Exception, "UI线程未处理异常");
                ShowCrashMessage(args.Exception);
                // 标记已处理，程序继续运行
                args.Handled = true;
            };

            // 2. AppDomain 未处理异常（后台线程兜底）
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LogManager.Log(args.ExceptionObject as Exception, "AppDomain未处理异常");
                ShowCrashMessage(args.ExceptionObject as Exception);
            };

            // 3. Task 未观察异常（async/await 中遗漏的异常）
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogManager.Log(args.Exception, "Task未观察异常");
                ShowCrashMessage(args.Exception);
                args.SetObserved(); // 标记已观察，防止进程被终止
            };
        }

        private bool _isShowingCrashDialog;

        /// <summary>
        /// 弹窗提示用户程序出现异常：告知日志位置，便于排查或发送给作者。
        /// 使用标志位防止连续异常时弹出多个窗口。
        /// </summary>
        private void ShowCrashMessage(Exception ex)
        {
            if (_isShowingCrashDialog) return;
            _isShowingCrashDialog = true;
            try
            {
                var lang = LanguageManager.Instance;
                MessageBox.Show(
                    lang["CrashBody1"] + "\n\n" +
                    string.Format(lang["CrashLogDir"], LogManager.LogDirectory) + "\n" +
                    lang["CrashBody2"] + "\n\n" +
                    string.Format(lang["CrashBody3"], ex?.Message),
                    lang["CrashTitle"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch
            {
                // 弹窗本身失败时静默忽略
            }
            finally
            {
                _isShowingCrashDialog = false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SingleInstanceServer.Stop();
            if (_mutexOwned)
                _mutex?.ReleaseMutex();
            _mutex?.Close();
            trayManager?.Dispose();
            mainWindow?.Close();
            CountdownWindow.CloseAll();
        }

        /// <summary>
        /// 优化：使用更轻量的方式弹出自关闭提示
        /// </summary>
        private void ShowAutoCloseMessage(string message, string title, int delayMs)
        {
            var popup = new Window
            {
                Title = title,
                Content = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20),
                    TextWrapping = TextWrapping.Wrap
                },
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true
            };

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
            timer.Tick += (sender, args) => { timer.Stop(); popup.Close(); };
            timer.Start();
            popup.ShowDialog();
        }
    }
}
