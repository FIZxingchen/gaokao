using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace gokao
{
    /// <summary>
    /// 语言管理器：支持 简体中文 / English 双语文案。
    /// 默认"跟随系统"（国内系统显示中文，国外系统显示英文），
    /// 也可在设置界面手动指定语言，选择持久化到 usersetting.ini。
    /// 通过索引器绑定 XAML 文案，切换语言时刷新全部绑定。
    /// </summary>
    public sealed class LanguageManager : INotifyPropertyChanged
    {
        public static LanguageManager Instance { get; } = new LanguageManager();

        private const string Section = "General";
        private const string Key = "Language"; // Auto / zh / en

        private readonly Dictionary<string, string> _zh = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _isEnglish;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>语言切换后触发，供托盘菜单等代码创建的文案刷新</summary>
        public event EventHandler LanguageChanged;

        private LanguageManager()
        {
            FillZh();
            FillEn();
            ApplySaved();
        }

        public bool IsEnglish => _isEnglish;

        /// <summary>索引器：XAML 绑定用 {Binding [key], Source={x:Static local:LanguageManager.Instance}}</summary>
        public string this[string key]
        {
            get
            {
                var table = _isEnglish ? _en : _zh;
                if (table.TryGetValue(key, out string value)) return value;
                if (_zh.TryGetValue(key, out value)) return value;
                return key;
            }
        }

        public string Get(string key) => this[key];

        /// <summary>设置语言：Auto（跟随系统）/ zh / en，写入配置并刷新全部绑定</summary>
        public void SetLanguage(string language)
        {
            ConfigManager.WriteString(Section, Key, language ?? "Auto");
            ApplySaved();
            // 通知所有绑定刷新（含索引器绑定）
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplySaved()
        {
            string saved = ConfigManager.ReadString(Section, Key, "Auto");
            if (saved == "zh")
                _isEnglish = false;
            else if (saved == "en")
                _isEnglish = true;
            else
                _isEnglish = !IsChineseSystem();
        }

        /// <summary>国内/国外判断：操作系统界面语言以 zh 开头视为国内</summary>
        private static bool IsChineseSystem()
        {
            CultureInfo ci = CultureInfo.InstalledUICulture ?? CultureInfo.CurrentUICulture;
            string name = (ci?.Name ?? "").ToLowerInvariant();
            return name.StartsWith("zh");
        }

        private void FillZh()
        {
            // ── 设置窗口 MainWindow ──
            _zh["WinTitle"] = "倒计时设置";
            _zh["GroupAppearance"] = "外观设置";
            _zh["Opacity"] = "透明度";
            _zh["FontSize"] = "字体大小";
            _zh["Font"] = "字体";
            _zh["TextColor"] = "文字颜色";
            _zh["ChooseColor"] = "选择颜色";
            _zh["ResetStyle"] = "恢复默认样式";
            _zh["BgImage"] = "背景图片";
            _zh["ChooseImage"] = "选择图片";
            _zh["Clear"] = "清除";
            _zh["NoBg"] = "无背景";
            _zh["Animation"] = "动画效果";
            _zh["AnimNone"] = "无动画";
            _zh["AnimBreath"] = "呼吸缩放";
            _zh["AnimFlow"] = "流星雨";
            _zh["AnimFlowHint"] = "流星雨效果，配暗色背景使用";
            _zh["AnimFlip"] = "文字翻页";
            _zh["AnimCyber"] = "赛博风";
            _zh["AnimCyberHint"] = "故障/赛博朋克效果，RGB 分离 + 扫描线 + 随机故障闪烁，配暗色背景使用";
            _zh["AnimIce"] = "冰晶";
            _zh["AnimIceHint"] = "蓝白色冰冻光晕 + 缓慢呼吸闪烁 + 随机冰裂纹效果，配暗色背景使用";
            _zh["GroupBehavior"] = "行为设置";
            _zh["LockWindow"] = "锁定窗口（禁止拖动）";
            _zh["Topmost"] = "窗口置顶（始终在最前）";
            _zh["AutoStart"] = "开机自动启动";
            _zh["ClickThrough"] = "窗口可透过（鼠标穿透点击后方程序）";
            _zh["StartupTipOption"] = "启动时提示设置入口（托盘气泡）";
            _zh["GroupEvents"] = "自定义事件";
            _zh["AddEvent"] = "添加事件";
            _zh["DeleteEvent"] = "删除事件";
            _zh["Name"] = "名称：";
            _zh["Date"] = "日期：";
            _zh["SaveEvent"] = "保存修改";
            _zh["ApplyEvent"] = "应用选中";
            _zh["StartCountdown"] = "🚀 启动倒计时";
            _zh["StopCountdown"] = "⏹ 关闭倒计时";
            _zh["WallpaperSwitcher"] = "📂 壁纸切换库";
            _zh["Exam Mode"] = "考试模式";
            _zh["ExamCountdown"] = "考试倒计时";
            _zh["ExamDesc"] = "点击下方按钮进入全屏考试倒计时";
            _zh["ExamSlogan"] = "诚信考试，相信自己！";
            _zh["ExamStart"] = "开始计时";
            _zh["ExamPause"] = "暂停计时";
            _zh["ExamClose"] = "关闭窗口";
            _zh["ExamScrollHint"] = "在小时/分钟数字上滚动、滑动或按住左键拖动，时分互不影响";
            _zh["ExamTimeUp"] = "时间到！";
            _zh["ExamTimeUpFinish"] = "考试已结束";
            _zh["ExamNotes"] = "作业待办";
            _zh["ExamNotesTitle"] = "作业待办";
            _zh["ExamNoteLabel"] = "作业便签";
            _zh["ExamTodoLabel"] = "待办事项";
            _zh["ExamTodoAdd"] = "添加";
            _zh["ExamTodoDelete"] = "删除勾选";
            _zh["ExamTodoInputHint"] = "输入待办内容，回车或点击添加";
            // ── 番茄钟 PomodoroWindow ──
            _zh["Pomodoro"] = "🍅 番茄钟 / 学习计时";
            _zh["PomodoroTitle"] = "番茄钟";
            _zh["PomodoroFocus"] = "专注";
            _zh["PomodoroBreak"] = "休息";
            _zh["PomodoroMinutes"] = "分钟";
            _zh["PomodoroStart"] = "开始";
            _zh["PomodoroPause"] = "暂停";
            _zh["PomodoroReset"] = "重置";
            _zh["PomodoroFocusDone"] = "专注结束，休息一下吧！";
            _zh["PomodoroAutoBreakHint"] = "努力后，就休息一下吧~";
            _zh["PomodoroBreakDone"] = "休息结束，继续加油！";
            _zh["PomodoroFocusMinutesLabel"] = "专注(分钟):";
            _zh["PomodoroBreakMinutesLabel"] = "休息(分钟):";
            _zh["PomodoroApply"] = "应用";
            _zh["PomodoroInvalid"] = "请输入 1~180 之间的整数分钟";
            _zh["PomodoroApplied"] = "时长已应用";
            // ── 每日一句 DailyQuoteManager / QuoteBubbleWindow ──
            _zh["DailyQuote"] = "💬 每日一句励志语录（每天开机时弹出）";
            _zh["DailyQuoteTitle"] = "✦ 每日一句";
            _zh["DailyQuoteFooter"] = "—— 高考倒计时";
            _zh["Language"] = "语言";
            _zh["LangAuto"] = "跟随系统";
            _zh["LangZh"] = "简体中文";
            _zh["LangEn"] = "English";

            // ── 设置窗口代码（弹窗/对话框） ──
            _zh["NewEvent"] = "新事件";
            _zh["NewEventNameFormat"] = "{0}{1}";
            _zh["Unnamed"] = "未命名";
            _zh["ConfirmDeleteEvent"] = "确定删除事件\"{0}\"吗？";
            _zh["ConfirmDeleteTitle"] = "确认删除";
            _zh["InvalidDateMsg"] = "请选择有效日期";
            _zh["InfoTitle"] = "提示";
            _zh["EventApplied"] = "已应用事件：{0}";
            _zh["SuccessTitle"] = "成功";
            _zh["OpenBgDialogTitle"] = "选择背景图片";
            _zh["OpenWallpaperDialogTitle"] = "选择壁纸";
            _zh["ImgFileFilter"] = "图片文件";

            // ── 壁纸切换器 Windowsucai ──
            _zh["SucaiTitle"] = "壁纸切换器";
            _zh["WallpaperLib"] = "壁纸库";
            _zh["RefreshList"] = "刷新列表";
            _zh["AddWallpaper"] = "添加壁纸";
            _zh["StyleMode"] = "设置方式:";
            _zh["Fill"] = "填充";
            _zh["Fit"] = "适应";
            _zh["Stretch"] = "拉伸";
            _zh["Tile"] = "平铺";
            _zh["Center"] = "居中";
            _zh["Ready"] = "就绪";
            _zh["Loading"] = "加载中...";
            _zh["AddedCount"] = "已添加 {0} 张壁纸";
            _zh["ExistsOrInvalid"] = "所选文件已存在或无效";
            _zh["DirNotExist"] = "壁纸目录不存在";
            _zh["TotalCount"] = "共 {0} 张壁纸";
            _zh["LoadFailed"] = "加载失败：{0}";
            _zh["DeleteWallpaper"] = "确定删除壁纸「{0}」吗？\n此操作不可恢复。";
            _zh["DeleteConfirmTitle"] = "删除确认";
            _zh["Deleted"] = "已删除：{0}";
            _zh["DeleteFailed"] = "删除失败：{0}";
            _zh["ErrorTitle"] = "错误";
            _zh["SetWallpaperDone"] = "已设置壁纸：{0}";
            _zh["SetWallpaperFailed"] = "设置壁纸失败";
            _zh["DeleteWallpaperTip"] = "删除此壁纸";

            // ── 倒计时自动切换壁纸 WallpaperScheduler ──
            _zh["WsScheduleTitle"] = "倒计时自动切换壁纸";
            _zh["WsEvent"] = "事件";
            _zh["WsDaysLeft"] = "剩余天数";
            _zh["WsDays"] = "天";
            _zh["WsPickWallpaper"] = "选择壁纸";
            _zh["WsAddRule"] = "添加规则";
            _zh["WsDeleteRule"] = "删除选中";
            _zh["WsRuleFormat"] = "剩余 {0} 天 → {1}";
            _zh["WsRuleAdded"] = "已添加规则";
            _zh["WsRuleDeleted"] = "已删除规则";
            _zh["WsRuleDaysInvalid"] = "请输入有效的天数";
            _zh["WsRuleNoWallpaper"] = "请先选择壁纸";
            _zh["WsNoRules"] = "暂无规则";
            _zh["WsRuleLimit"] = "规则最多 20 条";

            // ── 倒计时窗口 CountdownWindow ──
            _zh["CountdownWinTitle"] = "倒计时";
            _zh["CountdownTitleFormat"] = "{0}倒计时";
            _zh["CountdownTextFormat"] = "{0}倒计时：\n{1}天 {2}时 {3}分 {4}秒";
            _zh["GaokaoCheer"] = "高考加油~";
            _zh["EventStarted"] = "{0}已开始！";
            _zh["LabelDays"] = "天";
            _zh["LabelHours"] = "时";
            _zh["LabelMinutes"] = "分";
            _zh["LabelSeconds"] = "秒";

            // ── 托盘 TrayIconManager ──
            _zh["TrayText"] = "高考倒计时";
            _zh["TraySettings"] = "设置";
            _zh["TrayToggle"] = "显示/隐藏倒计时";
            _zh["TrayExit"] = "退出";
            _zh["StartupTip"] = "设置可在系统托盘右键打开";

            // ── 启动提示 App ──
            _zh["AlreadyRunning"] = "高考倒计时已经在运行中。";
            _zh["StartupFailed"] = "程序启动失败：{0}";
            _zh["CrashTitle"] = "程序异常提示";
            _zh["CrashBody1"] = "当前程序因未知原因出现问题，已记录日志。";
            _zh["CrashLogDir"] = "日志目录：{0}";
            _zh["CrashBody2"] = "可查看该目录下的 error_日期.log 文件，\n或将日志文件发送给作者以协助排查问题。";
            _zh["CrashBody3"] = "错误信息：{0}";
        }

        private void FillEn()
        {
            // ── Settings window MainWindow ──
            _en["WinTitle"] = "Countdown Settings";
            _en["GroupAppearance"] = "Appearance";
            _en["Opacity"] = "Opacity";
            _en["FontSize"] = "Font Size";
            _en["Font"] = "Font";
            _en["TextColor"] = "Text Color";
            _en["ChooseColor"] = "Choose Color";
            _en["ResetStyle"] = "Reset Style";
            _en["BgImage"] = "Background";
            _en["ChooseImage"] = "Choose Image";
            _en["Clear"] = "Clear";
            _en["NoBg"] = "No Background";
            _en["Animation"] = "Animation";
            _en["AnimNone"] = "No Animation";
            _en["AnimBreath"] = "Breathing";
            _en["AnimFlow"] = "Meteor Shower";
            _en["AnimFlowHint"] = "Meteor Shower effect - best with a dark background";
            _en["AnimFlip"] = "Flip";
            _en["AnimCyber"] = "Cyberpunk";
            _en["AnimCyberHint"] = "Glitch/Cyberpunk effect - RGB split + scanlines + random glitch flicker, best with a dark background";
            _en["AnimIce"] = "Ice Crystal";
            _en["AnimIceHint"] = "Blue-white frozen glow + slow breathing shimmer + random ice crack effects, best with a dark background";
            _en["GroupBehavior"] = "Behavior";
            _en["LockWindow"] = "Lock Window (Disable Dragging)";
            _en["Topmost"] = "Always on Top";
            _en["AutoStart"] = "Start on Boot";
            _en["ClickThrough"] = "Click-Through (Pass Mouse to Background)";
            _en["StartupTipOption"] = "Show startup tip (tray balloon)";
            _en["GroupEvents"] = "Custom Events";
            _en["AddEvent"] = "Add Event";
            _en["DeleteEvent"] = "Delete Event";
            _en["Name"] = "Name:";
            _en["Date"] = "Date:";
            _en["SaveEvent"] = "Save";
            _en["ApplyEvent"] = "Apply";
            _en["StartCountdown"] = "🚀 Start Countdown";
            _en["StopCountdown"] = "⏹ Stop Countdown";
            _en["WallpaperSwitcher"] = "📂 Wallpaper Switcher";
            _en["Exam Mode"] = "Exam Mode";
            _en["ExamCountdown"] = "Exam Countdown";
            _en["ExamDesc"] = "Click the button below to enter the fullscreen exam countdown";
            _en["ExamSlogan"] = "Be honest in the exam - believe in yourself!";
            _en["ExamStart"] = "Start";
            _en["ExamPause"] = "Pause";
            _en["ExamClose"] = "Close";
            _en["ExamScrollHint"] = "Scroll, swipe, or drag with the left mouse button on the hours/minutes digits; hours and minutes adjust independently";
            _en["ExamTimeUp"] = "Time's Up!";
            _en["ExamTimeUpFinish"] = "Time's Up! Stop Writing.";
            _en["ExamNotes"] = "Homework To-dos";
            _en["ExamNotesTitle"] = "Homework To-dos";
            _en["ExamNoteLabel"] = "Homework Notes";
            _en["ExamTodoLabel"] = "To-dos";
            _en["ExamTodoAdd"] = "Add";
            _en["ExamTodoDelete"] = "Delete Checked";
            _en["ExamTodoInputHint"] = "Type a to-do, press Enter or click Add";
            // ── Pomodoro PomodoroWindow ──
            _en["Pomodoro"] = "🍅 Pomodoro / Study Timer";
            _en["PomodoroTitle"] = "Pomodoro";
            _en["PomodoroFocus"] = "Focus";
            _en["PomodoroBreak"] = "Break";
            _en["PomodoroMinutes"] = "min";
            _en["PomodoroStart"] = "Start";
            _en["PomodoroPause"] = "Pause";
            _en["PomodoroReset"] = "Reset";
            _en["PomodoroFocusDone"] = "Focus done - take a break!";
            _en["PomodoroAutoBreakHint"] = "You've worked hard - time for a break~";
            _en["PomodoroBreakDone"] = "Break over - keep going!";
            _en["PomodoroFocusMinutesLabel"] = "Focus (min):";
            _en["PomodoroBreakMinutesLabel"] = "Break (min):";
            _en["PomodoroApply"] = "Apply";
            _en["PomodoroInvalid"] = "Enter an integer between 1 and 180 minutes";
            _en["PomodoroApplied"] = "Duration applied";
            // ── Daily quote DailyQuoteManager / QuoteBubbleWindow ──
            _en["DailyQuote"] = "💬 Daily motivational quote (show at startup)";
            _en["DailyQuoteTitle"] = "✦ Quote of the Day";
            _en["DailyQuoteFooter"] = "— Gaokao Countdown";
            _en["Language"] = "Language";
            _en["LangAuto"] = "Auto (System)";
            _en["LangZh"] = "简体中文";
            _en["LangEn"] = "English";

            // ── Settings window code-behind ──
            _en["NewEvent"] = "New Event";
            _en["NewEventNameFormat"] = "{0} {1}";
            _en["Unnamed"] = "Unnamed";
            _en["ConfirmDeleteEvent"] = "Delete event \"{0}\"?";
            _en["ConfirmDeleteTitle"] = "Confirm Delete";
            _en["InvalidDateMsg"] = "Please select a valid date";
            _en["InfoTitle"] = "Notice";
            _en["EventApplied"] = "Event applied: {0}";
            _en["SuccessTitle"] = "Success";
            _en["OpenBgDialogTitle"] = "Choose Background Image";
            _en["OpenWallpaperDialogTitle"] = "Choose Wallpaper";
            _en["ImgFileFilter"] = "Image Files";

            // ── Wallpaper switcher Windowsucai ──
            _en["SucaiTitle"] = "Wallpaper Switcher";
            _en["WallpaperLib"] = "Wallpaper Library";
            _en["RefreshList"] = "Refresh";
            _en["AddWallpaper"] = "Add Wallpaper";
            _en["StyleMode"] = "Style:";
            _en["Fill"] = "Fill";
            _en["Fit"] = "Fit";
            _en["Stretch"] = "Stretch";
            _en["Tile"] = "Tile";
            _en["Center"] = "Center";
            _en["Ready"] = "Ready";
            _en["Loading"] = "Loading...";
            _en["AddedCount"] = "Added {0} wallpaper(s)";
            _en["ExistsOrInvalid"] = "Selected files already exist or are invalid";
            _en["DirNotExist"] = "Wallpaper directory does not exist";
            _en["TotalCount"] = "{0} wallpaper(s) in total";
            _en["LoadFailed"] = "Load failed: {0}";
            _en["DeleteWallpaper"] = "Delete wallpaper \"{0}\"?\nThis cannot be undone.";
            _en["DeleteConfirmTitle"] = "Delete Confirmation";
            _en["Deleted"] = "Deleted: {0}";
            _en["DeleteFailed"] = "Delete failed: {0}";
            _en["ErrorTitle"] = "Error";
            _en["SetWallpaperDone"] = "Wallpaper set: {0}";
            _en["SetWallpaperFailed"] = "Failed to set wallpaper";
            _en["DeleteWallpaperTip"] = "Delete this wallpaper";

            // ── Auto wallpaper switch WallpaperScheduler ──
            _en["WsScheduleTitle"] = "Auto Wallpaper by Countdown";
            _en["WsEvent"] = "Event";
            _en["WsDaysLeft"] = "Days Left";
            _en["WsDays"] = "days";
            _en["WsPickWallpaper"] = "Pick";
            _en["WsAddRule"] = "Add Rule";
            _en["WsDeleteRule"] = "Delete";
            _en["WsRuleFormat"] = "{0} days left → {1}";
            _en["WsRuleAdded"] = "Rule added";
            _en["WsRuleDeleted"] = "Rule deleted";
            _en["WsRuleDaysInvalid"] = "Enter a valid number of days";
            _en["WsRuleNoWallpaper"] = "Pick a wallpaper first";
            _en["WsNoRules"] = "No rules";
            _en["WsRuleLimit"] = "Up to 20 rules";

            // ── Countdown window CountdownWindow ──
            _en["CountdownWinTitle"] = "Countdown";
            _en["CountdownTitleFormat"] = "{0} Countdown";
            _en["CountdownTextFormat"] = "{0} Countdown:\n{1}d {2}h {3}m {4}s";
            _en["GaokaoCheer"] = "Good luck on the Gaokao!";
            _en["EventStarted"] = "{0} has started!";
            _en["LabelDays"] = "D";
            _en["LabelHours"] = "H";
            _en["LabelMinutes"] = "M";
            _en["LabelSeconds"] = "S";

            // ── Tray TrayIconManager ──
            _en["TrayText"] = "Gaokao Countdown";
            _en["TraySettings"] = "Settings";
            _en["TrayToggle"] = "Show/Hide Countdown";
            _en["TrayExit"] = "Exit";
            _en["StartupTip"] = "Right-click the tray icon for settings";

            // ── Startup App ──
            _en["AlreadyRunning"] = "Gaokao Countdown is already running.";
            _en["StartupFailed"] = "Failed to start: {0}";
            _en["CrashTitle"] = "Unexpected Error";
            _en["CrashBody1"] = "The program ran into an unexpected problem; details have been logged.";
            _en["CrashLogDir"] = "Log directory: {0}";
            _en["CrashBody2"] = "Check the error_date.log file in that directory,\nor send it to the author for troubleshooting.";
            _en["CrashBody3"] = "Error: {0}";
        }
    }
}
