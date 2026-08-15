using System;
using System.IO;
using System.Text;
using System.Windows;

namespace gokao
{
    /// <summary>
    /// 配置管理器 — 使用 INI 文件持久化（兼容原版格式）。
    /// 优化：增加异常日志、缓存文件存在检查、字符串安全处理。
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usersetting.ini");

        private static readonly int BufferSize = 1024;

        [System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);

        [System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, char[] buffer, int size, string filePath);

        static ConfigManager()
        {
            EnsureFileExists();
        }

        private static void EnsureFileExists()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    var dir = Path.GetDirectoryName(ConfigPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    // 以 UTF-16LE(BOM) 创建，与 WritePrivateProfileStringW 的 Unicode 写入保持一致，
                    // 避免旧版"UTF-8 注释头 + ANSI 数据"导致的混编码问题
                    File.WriteAllText(ConfigPath, "; 高考倒计时配置文件\r\n", Encoding.Unicode);
                }
                else
                {
                    NormalizeLegacyEncoding();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 创建配置文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 兼容旧版混编码配置：早期文件头由 .NET 以 UTF-8(带 BOM) 写入注释，
        /// 而数据由 Win32 API 以系统 ANSI 代码页追加，两种编码混在同一文件。
        /// 检测到 UTF-8 BOM 时，将注释头(UTF-8)与数据体(ANSI)统一重写为 UTF-16LE(BOM)；
        /// 任一步失败则保持原文件不变，避免损坏配置。
        /// </summary>
        private static void NormalizeLegacyEncoding()
        {
            try
            {
                byte[] raw = File.ReadAllBytes(ConfigPath);
                if (raw.Length < 3 || raw[0] != 0xEF || raw[1] != 0xBB || raw[2] != 0xBF)
                    return; // 无 UTF-8 BOM，说明不是旧版混编码文件

                // 分离 UTF-8 注释头（BOM 之后到第一个换行）与 ANSI 数据体
                int nl = -1;
                for (int i = 3; i < raw.Length; i++)
                {
                    if (raw[i] == (byte)'\n') { nl = i; break; }
                }
                if (nl < 0) return;

                string header = Encoding.UTF8.GetString(raw, 3, nl - 3).TrimEnd('\r');
                string body = Encoding.Default.GetString(raw, nl + 1, raw.Length - nl - 1);

                File.WriteAllText(ConfigPath, header + "\r\n" + body, Encoding.Unicode);
                LogManager.Info($"[ConfigManager] 旧版混编码配置已转换为 UTF-16: {ConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 转换配置编码失败: {ex.Message}");
            }
        }

        public static string ReadString(string section, string key, string defaultValue = "")
        {
            try
            {
                char[] buffer = new char[BufferSize];
                int length = GetPrivateProfileString(section ?? "", key ?? "", defaultValue ?? "", buffer, buffer.Length, ConfigPath);
                return new string(buffer, 0, Math.Max(0, length));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 读取 [{section}]{key} 失败: {ex.Message}");
                return defaultValue;
            }
        }

        public static double ReadDouble(string section, string key, double defaultValue = 0)
        {
            string raw = ReadString(section, key, null);
            if (raw == null) return defaultValue;
            // 支持逗号/点号小数
            raw = raw.Replace(',', '.');
            if (double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;
            return defaultValue;
        }

        public static bool ReadBool(string section, string key, bool defaultValue = false)
        {
            string val = ReadString(section, key, defaultValue ? "1" : "0");
            return val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static void WriteString(string section, string key, string value)
        {
            try
            {
                bool ok = WritePrivateProfileString(section ?? "", key ?? "", value ?? "", ConfigPath);
                if (!ok)
                    LogManager.Info($"[ConfigManager] 写入失败 [{section}]{key} → {ConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 写入 [{section}]{key} 失败: {ex.Message}");
            }
        }

        public static void WriteDouble(string section, string key, double value)
        {
            WriteString(section, key, value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        }

        public static void WriteBool(string section, string key, bool value)
        {
            WriteString(section, key, value ? "1" : "0");
        }

        // ── 事件级配置（每个事件独立一个 .ini 文件） ──

        /// <summary>获取事件对应的配置文件路径</summary>
        public static string EventConfigPath(string eventName)
        {
            string safe = eventName;
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{safe}.ini");
        }

        public static string ReadString(string filePath, string section, string key, string defaultValue = "")
        {
            try
            {
                char[] buffer = new char[BufferSize];
                int length = GetPrivateProfileString(section ?? "", key ?? "", defaultValue ?? "", buffer, buffer.Length, filePath);
                return new string(buffer, 0, Math.Max(0, length));
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void WriteString(string filePath, string section, string key, string value)
        {
            try
            {
                bool ok = WritePrivateProfileString(section ?? "", key ?? "", value ?? "", filePath);
                if (!ok)
                    LogManager.Info($"[ConfigManager] 写入失败 [{section}]{key} → {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 写入 [{section}]{key} 失败: {ex.Message}");
            }
        }

        public static double ReadDouble(string filePath, string section, string key, double defaultValue = 0)
        {
            string raw = ReadString(filePath, section, key, null);
            if (raw == null) return defaultValue;
            raw = raw.Replace(',', '.');
            if (double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;
            return defaultValue;
        }

        public static void WriteDouble(string filePath, string section, string key, double value)
        {
            WriteString(filePath, section, key, value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        }

        public static bool ReadBool(string filePath, string section, string key, bool defaultValue = false)
        {
            string val = ReadString(filePath, section, key, defaultValue ? "1" : "0");
            return val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static void WriteBool(string filePath, string section, string key, bool value)
        {
            WriteString(filePath, section, key, value ? "1" : "0");
        }
    }
}
