using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows;

namespace gokao
{
    /// <summary>
    /// 单实例通信：主实例创建命名管道服务器等待命令；二次启动的实例作为
    /// 客户端连接管道发送命令（唤起窗口、切换事件等）后自行退出。
    /// 命令格式：Show（唤起设置/倒计时窗口）、Event:事件名（切换事件）。
    /// </summary>
    public static class SingleInstanceServer
    {
        private const string PipeName = "gokao_optimized_single_instance";
        private const int ConnectTimeoutMs = 2000;

        private static Thread _serverThread;
        private static volatile bool _running;

        /// <summary>启动管道服务器（仅主实例调用；后台线程循环接收命令）</summary>
        public static void Start(Action<string> onCommand)
        {
            if (_serverThread != null) return;
            _running = true;
            _serverThread = new Thread(() => ServerLoop(onCommand))
            {
                IsBackground = true,
                Name = "SingleInstancePipe"
            };
            _serverThread.Start();
        }

        public static void Stop() => _running = false;

        private static void ServerLoop(Action<string> onCommand)
        {
            while (_running)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(
                        PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        server.WaitForConnection();
                        using (var reader = new StreamReader(server, Encoding.UTF8))
                        {
                            string cmd = reader.ReadLine();
                            if (!string.IsNullOrEmpty(cmd))
                            {
                                // 命令处理涉及窗口操作，必须转回 UI 线程
                                Application.Current?.Dispatcher?.BeginInvoke(
                                    new Action(() => SafeRun(onCommand, cmd)));
                            }
                        }
                    }
                }
                catch
                {
                    // 管道异常/被中断：忽略并短暂等待后继续接受下一个连接
                    Thread.Sleep(200);
                }
            }
        }

        private static void SafeRun(Action<string> onCommand, string cmd)
        {
            try { onCommand(cmd); }
            catch (Exception ex) { LogManager.Log(ex, "处理单实例命令失败"); }
        }

        /// <summary>
        /// 尝试连接主实例并发送命令；成功返回 true（说明已有实例在运行，
        /// 调用方应提示后退出）。连接失败返回 false（说明是首个实例）。
        /// </summary>
        public static bool TrySendCommand(string command)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(ConnectTimeoutMs);
                    using (var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true })
                    {
                        writer.WriteLine(command);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
