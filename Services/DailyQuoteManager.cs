using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;
using gokao.Views;

namespace gokao
{
    /// <summary>
    /// 每日一句励志语录管理器：勾选后每次开机（程序启动）当天弹出一次励志气泡，
    /// 按一年中的第几天轮换语录，保证每天一句不一样；已弹过的日期不再重复弹出。
    /// 语录优先读取程序目录下的 quotes.json（用户可手动增删），缺失/损坏时回退内置列表。
    /// 配置存于 usersetting.ini 的 [DailyQuote] 节（Enabled=是否启用，LastShownDate=上次弹出日期）。
    /// </summary>
    public static class DailyQuoteManager
    {
        private const string Section = "DailyQuote";
        private const string KeyEnabled = "Enabled";
        private const string KeyLastShown = "LastShownDate";
        private const string QuoteFileName = "quotes.json";

        /// <summary>内置励志语录（quotes.json 缺失/损坏时的兜底，按天轮换保证每天不同）</summary>
        private static readonly string[] DefaultQuotes =
        {
            "乾坤未定，你我皆是黑马。",
            "星光不问赶路人，时光不负有心人。",
            "你若盛开，蝴蝶自来；你若努力，天自安排。",
            "宝剑锋从磨砺出，梅花香自苦寒来。",
            "路虽远，行则将至；事虽难，做则必成。",
            "所有的努力，都会在你看不见的地方生根发芽。",
            "今天多一份努力，明天少一份遗憾。",
            "坚持很难，但坚持到底一定很酷。",
            "没有白走的路，每一步都算数。",
            "既然选择了远方，便只顾风雨兼程。",
            "滴水穿石，不是力量大，而是功夫深。",
            "书山有路勤为径，学海无涯苦作舟。",
            "不怕慢，就怕站；只要不放弃，终会抵达。",
            "每一个优秀的人，都有一段沉默的时光。",
            "天赋决定上限，努力决定下限。",
            "你要悄悄拔尖，然后惊艳所有人。",
            "少年不惧岁月长，彼方尚有荣光在。",
            "努力到无能为力，拼搏到感动自己。",
            "此刻打盹，你将做梦；此刻学习，你将圆梦。",
            "逆水行舟，不进则退。",
            "熬过无人问津的日子，才有诗和远方。",
            "别让今天的懒，成为明天的难。",
            "最怕你一生碌碌无为，还安慰自己平凡可贵。",
            "将来的你，一定会感谢现在拼命的自己。",
            "奋斗是青春最亮丽的底色。",
            "积跬步以至千里，积小流以成江海。",
            "心之所向，素履以往；生如逆旅，一苇以航。",
            "只有千锤百炼，才能成为好钢。",
            "做自己的太阳，无需凭借谁的光。",
            "今日事今日毕，明日复明日，明日何其多。",
            "千磨万击还坚劲，任尔东西南北风。",
            "长风破浪会有时，直挂云帆济沧海。"
        };

        public static bool IsEnabled() => ConfigManager.ReadBool(Section, KeyEnabled, false);

        public static void SetEnabled(bool enabled) => ConfigManager.WriteBool(Section, KeyEnabled, enabled);

        /// <summary>
        /// 读取语录列表：优先读程序目录下的 quotes.json（用户可手动增删），
        /// 文件缺失、内容为空或解析失败时回退内置列表，保证功能始终可用。
        /// </summary>
        private static string[] LoadQuotes()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, QuoteFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var list = new JavaScriptSerializer().Deserialize<string[]>(json);
                    if (list != null && list.Length > 0)
                        return list;
                    LogManager.Info($"[DailyQuote] quotes.json 内容为空，使用内置语录");
                }
            }
            catch (Exception ex)
            {
                LogManager.Info($"[DailyQuote] 读取 quotes.json 失败，使用内置语录: {ex.Message}");
            }
            return DefaultQuotes;
        }

        /// <summary>按一年中的第几天取语录，跨年自动从头轮换，保证每天不同</summary>
        private static string QuoteOfToday()
        {
            var quotes = LoadQuotes();
            int idx = DateTime.Now.DayOfYear % quotes.Length;
            return quotes[idx];
        }

        /// <summary>今天是否已弹过（防止同一天多次启动重复弹出）</summary>
        private static bool ShownToday()
        {
            string last = ConfigManager.ReadString(Section, KeyLastShown, "");
            return last == DateTime.Now.ToString("yyyy-MM-dd");
        }

        /// <summary>启动时调用：启用且今天未弹过 → 弹出美化励志气泡并记录日期</summary>
        public static void ShowIfDue()
        {
            try
            {
                if (!IsEnabled() || ShownToday()) return;
                ConfigManager.WriteString(Section, KeyLastShown, DateTime.Now.ToString("yyyy-MM-dd"));
                var win = new QuoteBubbleWindow(QuoteOfToday());
                win.Show();
            }
            catch (Exception ex)
            {
                LogManager.Log(ex, "每日一句弹出失败");
            }
        }
    }
}
