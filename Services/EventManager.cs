using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using gokao.Models;

namespace gokao
{
    /// <summary>
    /// 事件管理器：统一负责自定义事件的列表持久化、活跃事件追踪、
    /// 以及事件专属背景图片目录的增删管理。UI 层只做展示与交互。
    /// </summary>
    public static class EventManager
    {
        /// <summary>自定义事件列表（供 ListBox 绑定）</summary>
        public static readonly ObservableCollection<CustomEvent> CustomEvents =
            new ObservableCollection<CustomEvent>();

        /// <summary>活跃事件名集合（有倒计时窗口在桌面显示的事件）</summary>
        private static readonly HashSet<string> _activeEvents = new HashSet<string>();

        // ── 列表持久化 ──

        /// <summary>从配置加载事件列表；无事件时创建默认"高考"事件</summary>
        public static void LoadCustomEvents()
        {
            CustomEvents.Clear();
            string data = ConfigManager.ReadString("CustomEvents", "List", "");
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var item in data.Split(';'))
                {
                    var parts = item.Split('|');
                    if (parts.Length == 2 && DateTime.TryParse(parts[1], out DateTime date))
                        CustomEvents.Add(new CustomEvent { Name = parts[0], Date = date });
                }
            }
            if (CustomEvents.Count == 0)
            {
                var defaultDate = new DateTime(DateTime.Now.Year, 6, 7);
                if (DateTime.Now > defaultDate) defaultDate = defaultDate.AddYears(1);
                CustomEvents.Add(new CustomEvent { Name = "高考", Date = defaultDate });
                SaveCustomEvents();
            }
        }

        // ── 活跃事件管理 ──

        /// <summary>从配置加载活跃事件列表；无记录时默认"高考"活跃</summary>
        public static void LoadActiveEvents()
        {
            _activeEvents.Clear();
            string data = ConfigManager.ReadString("ActiveEvents", "List", "");
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var name in data.Split(';'))
                    if (!string.IsNullOrEmpty(name))
                        _activeEvents.Add(name);
            }
            // 默认至少"高考"活跃
            if (_activeEvents.Count == 0)
                _activeEvents.Add("高考");
        }

        /// <summary>将活跃事件列表持久化到配置</summary>
        public static void SaveActiveEvents()
        {
            ConfigManager.WriteString("ActiveEvents", "List", string.Join(";", _activeEvents));
        }

        /// <summary>事件是否有活跃的倒计时窗口</summary>
        public static bool IsActiveEvent(string eventName) => _activeEvents.Contains(eventName);

        /// <summary>设置事件的活跃状态并持久化</summary>
        public static void SetActive(string eventName, bool active)
        {
            if (active)
                _activeEvents.Add(eventName);
            else
                _activeEvents.Remove(eventName);
            SaveActiveEvents();
        }

        /// <summary>获取所有活跃事件名的快照</summary>
        public static List<string> GetActiveEventNames() => new List<string>(_activeEvents);

        /// <summary>将事件列表持久化到配置</summary>
        public static void SaveCustomEvents()
        {
            string data = string.Join(";", CustomEvents.Select(e => $"{e.Name}|{e.Date:yyyy-MM-dd}"));
            ConfigManager.WriteString("CustomEvents", "List", data);
        }

        /// <summary>获取第一个活跃事件名（向后兼容：WallpaperScheduler 等旧代码使用）</summary>
        public static string GetActiveEventName()
        {
            var names = GetActiveEventNames();
            return names.Count > 0 ? names[0] : "";
        }

        // ── 事件背景图片目录管理 ──

        /// <summary>获取事件专属背景图片的存放目录（与桌面壁纸 wallpaper/ 完全独立）</summary>
        public static string EventBgDir(string eventName)
        {
            string safe = SanitizeFileName(eventName);
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg", "events", safe);
        }

        /// <summary>将图片复制到事件专属目录，返回目标路径</summary>
        public static string CopyToEventBg(string sourcePath, string eventName)
        {
            string dir = EventBgDir(eventName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string fileName = Path.GetFileName(sourcePath);
            string dest = Path.Combine(dir, fileName);
            // 如果目标已存在且路径相同则跳过
            if (!string.Equals(sourcePath, dest, StringComparison.OrdinalIgnoreCase))
            {
                // 重试 3 次：文件可能被 WPF 缓存短暂锁定
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        File.Copy(sourcePath, dest, overwrite: true);
                        break;
                    }
                    catch (IOException) when (i < 2)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        System.Threading.Thread.Sleep(300 * (i + 1));
                    }
                }
            }
            return dest;
        }

        /// <summary>删除事件专属背景目录及其所有文件（带重试机制，释放 WPF BitmapImage 文件锁）</summary>
        public static void DeleteEventBgDir(string eventName)
        {
            string dir = EventBgDir(eventName);
            if (!Directory.Exists(dir)) return;

            // 最多重试 3 次，每次间隔递增
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    return;
                }
                catch (IOException) when (i < 2)
                {
                    // WPF 的 BitmapImage 缓存可能仍持有文件锁，
                    // 强制 GC 回收释放底层资源后再重试
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(200 * (i + 1));
                }
            }
            // 最后一次尝试，失败就抛出去让上层处理
            Directory.Delete(dir, recursive: true);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
