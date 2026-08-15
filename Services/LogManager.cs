using System;
using System.IO;
using System.Text;

namespace gokao
{
    /// <summary>
    /// 日志管理器：记录运行时异常到程序目录 logs/ 文件夹，便于排查崩溃问题。
    /// </summary>
    public static class LogManager
    {
        private static readonly object _lock = new object();
        private static string _logDir;

        private static string LogDir
        {
            get
            {
                if (_logDir == null)
                {
                    _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    try
                    {
                        if (!Directory.Exists(_logDir))
                            Directory.CreateDirectory(_logDir);
                    }
                    catch
                    {
                        _logDir = Path.GetTempPath();
                    }
                }
                return _logDir;
            }
        }

        /// <summary>日志文件目录（供 UI 提示用户查看）</summary>
        public static string LogDirectory => LogDir;

        /// <summary>记录一条异常日志（线程安全，自动按天分文件）</summary>
        public static void Log(Exception ex, string context = "")
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {context}");
                sb.AppendLine($"  异常类型: {ex?.GetType().FullName}");
                sb.AppendLine($"  消息: {ex?.Message}");
                sb.AppendLine($"  堆栈: {ex?.StackTrace}");
                if (ex?.InnerException != null)
                {
                    sb.AppendLine($"  内部异常: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                    sb.AppendLine($"  内部堆栈: {ex.InnerException.StackTrace}");
                }
                sb.AppendLine();

                lock (_lock)
                {
                    string file = Path.Combine(LogDir, $"error_{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(file, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // 日志写入失败时静默忽略，避免二次异常
            }
        }

        /// <summary>记录一条普通信息日志（可选使用）</summary>
        public static void Info(string message)
        {
            try
            {
                lock (_lock)
                {
                    string file = Path.Combine(LogDir, $"app_{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n", Encoding.UTF8);
                }
            }
            catch
            {
            }
        }
    }
}
