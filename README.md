# ⏳ 高考倒计时（支持自定义事件）

> 一款优雅的 Windows 桌面倒计时工具，支持高考及其他自定义事件倒计时。  
> 自动计算剩余天数、小时、分钟和秒数，多种动画效果，窗口可穿透。

[![中文](https://img.shields.io/badge/lang-中文-blue.svg)](./README.md)
[![English](https://img.shields.io/badge/lang-English-green.svg)](./README.en.md)

[![Language](https://img.shields.io/badge/language-C%23-blue.svg)](https://docs.microsoft.com/zh-cn/dotnet/csharp/)
[![Framework](https://img.shields.io/badge/.NET-Framework%204.8-orange.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MulanPSL2-green.svg)](./LICENSE)

---

## ✨ 功能亮点

- **📅 自动推算日期**  
  智能识别：若今年高考（6月7日）未到则显示今年倒计时，已过则自动切换到明年。(归零祝福~)

- **🎞️ 六种动画模式**  
  无动画、呼吸缩放、流星雨、3D 文字翻页、赛博风、冰晶。翻页模式下数字和中文标签同步跟随字体、颜色设置，数字卡片带渐变背景、圆角、微光边框与中央铰链线；呼吸缩放的颜色闪烁基于当前文字颜色动态提亮，不会覆盖你的配色；流星雨模式下带光尾的黄色半透明星星随机飘落、背景星点点缀、偶尔有亮流星斜向划过（配暗色背景使用效果最佳，默认白色文字时自动切换为星辰蓝紫配色）；赛博风模式下文字以霓虹青为底色带霓虹辉光发光、CRT 扫描线遮罩，每 1.5~3.5 秒触发一次 5 阶段故障闪烁序列（RGB 三通道分离 → 水平撕裂 → 品红偏移 → 色彩染色 → 收敛回归），伴有故障撕裂带和偶尔的垂直错行，日常态还有霓虹微闪模拟灯管接触不良；冰晶模式下文字带蓝白色冰冻辉光，8 片雪花（❄❅❆）从窗口顶部缓缓飘落并左右摇摆，每 4~8 秒随机触发倾斜冰裂纹效果——文字沿倾斜裂纹线用 PathGeometry 裁剪分裂为上下两半，缓慢裂开（400ms 渐进偏移）→ 震颤 → 愈合，叠加倾斜锯齿白色裂纹线、分叉裂纹与十字星冰晶碎屑（配暗色背景使用效果最佳）。倒计时归零时，无论选择哪种动画模式，祝福语均以打字机效果逐字打出（冰蓝色辉光 + 闪烁光标 ▎），赋予归零时刻仪式感。

- **🎨 完全自定义外观**  
  透明度、字体大小、文字颜色、字体随意调整，更改即时生效并自动保存；提供「恢复默认样式」按钮，一键重置所有外观设置，并将倒计时窗口居中到屏幕中央。

- **🖱️ 窗口可穿透**  
  开启后鼠标点击倒计时窗口会穿透到后方程序，不影响日常操作。

- **🔒 窗口锁定**  
  一键锁定，防止误拖拽和误操作。

- **📌 窗口置顶**  
  倒计时窗口始终在最前，重要日期时刻可见。

- **🖼️ 背景图片**  
  支持选择本地图片作为倒计时背景，透明度同步跟随滑块调节，每个事件可独立设置背景。

- **📋 系统托盘驻留**  
  启动后自动驻留系统托盘，右键菜单可打开设置、显示/隐藏倒计时、退出程序。启动时气泡提示告知设置入口位置（可在行为设置中关闭「启动时提示设置入口」）。

- **🌐 双语界面**  
  界面支持中文 / English 自动跟随系统语言，也可在设置中手动切换，切换即时生效。

- **🚀 开机自启动**  
  可选择随系统启动，时刻提醒重要日期。

- **📝 多事件管理 + 多窗口倒计时**  
  支持添加、编辑、删除多个自定义事件（如考研、生日、项目截止日等），每个事件拥有独立的样式、背景与位置配置。事件列表中每个事件前有复选框，勾选后该事件的倒计时窗口立即出现在桌面，取消则隐藏——支持同时显示多个事件的倒计时窗口。双击事件列表项也可快速切换窗口显隐。活跃事件列表自动持久化，重启后恢复上次显示的窗口。新建事件默认使用默认样式，不继承当前事件外观；颜色、字号、字体、透明度、动画、锁定均按事件独立保存，切换事件互不串带。默认仅"高考"倒计时窗口显示。

- **🎓 考试模式**  
  全屏黑底白字考试倒计时：滚轮、触屏滑动或按住左键上下拖动均可设置考试时长（默认 02:30:00），支持开始 / 暂停 / 关闭，按 Esc 也可关闭。考试期间左下角实时显示当前时间，并自动阻止系统睡眠/关屏（白板不熄屏）；倒计时结束自动响铃三声并显示「考试已结束」。

- **📝 作业待办**  
  考试模式内置作业便签与待办清单：便签自由记录、待办支持添加（回车亦可）/勾选划线表示完成/一键删除全部完成项（带变淡滑出动画），数据自动保存到独立的 `ExamNotes.ini`。

- **🍅 番茄钟 / 学习计时**  
  考试模式内置番茄钟：专注 / 休息时长可自定义（1~180 分钟，默认 25/5 分钟），应用后即时生效并自动保存；支持开始 / 暂停 / 重置，倒计时结束响铃三声并提示下一阶段。

- **💬 每日一句励志语录**  
  考试模式可勾选「每日一句励志语录」：开启后每天开机（程序启动）弹出一次美化励志气泡（圆角渐变卡片、自动淡出关闭、不抢焦点），内置 365 句按天轮换、每天不重样；语录存于程序目录 `quotes.json`，用户可手动增删，文件损坏自动回退内置列表。

- **🖥️ 高分屏适配**  
  声明 PerMonitorV2 DPI 感知：高分屏（125%/150%）下文字清晰不模糊，窗口跨屏拖动时按所在显示器实时缩放。

- **📂 素材库**  
  内置壁纸素材库，预览图片并一键设为 Windows 桌面壁纸，支持填充 / 适应 / 拉伸 / 平铺 / 居中多种显示样式（壁纸需自行添加）。

- **🖼️ 壁纸自动切换**  
  按倒计时剩余天数自动切换桌面壁纸：为每个事件独立配置「剩余 N 天 → 壁纸」规则（每事件最多 20 条），30 秒自动巡检，跨过阈值时只切换一次；壁纸文件被删除时自动跳过该规则，不影响其他规则生效。

- **🔁 单实例运行 + 命令管道**  
  互斥锁保证程序只运行一个实例，重复双击会提示后退出；内置命名管道通信，支持 `gokao_optimized.exe --event 事件名`（或 `-e 事件名`）向已运行实例发送命令，一键激活该事件的倒计时窗口。

- **📜 日志系统**  
  运行异常自动写入 `logs/` 目录（错误日志 + 应用日志），便于排查问题。

- **💾 配置持久化**  
  窗口位置、样式设置、事件列表、活跃事件自动保存。全局配置存于 `usersetting.ini`，每个事件独立保存一份 `<事件名>.ini`，下次启动自动恢复上次显示的倒计时窗口。

- **🎛️ 全局样式统一**  
  按钮、GroupBox、CheckBox、Slider 等控件样式提取到全局 `Themes/Styles.xaml`，所有窗口共享统一的圆角按钮模板与交互动效，减少重复代码。

---

## 📷 界面预览
![image.png](https://raw.gitcode.com/user-images/assets/10133549/b875b84b-be91-42e3-a1db-957f3a5165b1/image.png 'image.png')

![50d957534dbb4c05c27a51ec2b9b6b6d.png](https://raw.gitcode.com/user-images/assets/10133549/e098e25d-3ec3-4176-91a6-6f4efe40880a/50d957534dbb4c05c27a51ec2b9b6b6d.png '50d957534dbb4c05c27a51ec2b9b6b6d.png')

#支持窗口背景
![08de936ee0abd1cea1883507f0420e26.png](https://raw.gitcode.com/user-images/assets/10133549/227d40a8-516b-423e-abb9-52d030b6be48/08de936ee0abd1cea1883507f0420e26.png '08de936ee0abd1cea1883507f0420e26.png')

#壁纸切换器
![39e2b7503073828047b3164ec7456cf1.png](https://raw.gitcode.com/user-images/assets/10133549/d3bab935-c8f7-4484-9b70-74b5af99c6fd/39e2b7503073828047b3164ec7456cf1.png '39e2b7503073828047b3164ec7456cf1.png')

![772eb537b2c5061fdf53e2f44a10dfa0.png](https://raw.gitcode.com/user-images/assets/10133549/4477bf62-bf62-4c94-92b1-16b1d19b46ff/772eb537b2c5061fdf53e2f44a10dfa0.png '772eb537b2c5061fdf53e2f44a10dfa0.png')
![image.png](https://raw.gitcode.com/user-images/assets/10133549/32b747ae-c154-424d-b7be-6999272bc4f3/image.png 'image.png')
<span style="font-size:14px;"><ins>PS:本分发不包含任何第三方图片</ins></span>

![image.png](https://raw.gitcode.com/user-images/assets/10133549/bb0d1e19-2899-44e9-872a-323eb01d03e3/image.png 'image.png')

![afeb18fb01f16aedeb82da693df3d92d.png](https://raw.gitcode.com/user-images/assets/10133549/b7d929d2-7d95-427f-8dc5-3b3592cfd7dc/afeb18fb01f16aedeb82da693df3d92d.png 'afeb18fb01f16aedeb82da693df3d92d.png')
---

##  快速开始
![image.png](https://raw.gitcode.com/user-images/assets/10133549/4fbc78a4-3f39-4ccf-a0d0-b2f3058b029c/image.png 'image.png')

### 运行环境
- Windows 7/8/10/11  
- [.NET Framework 4.8 或更高版本](https://dotnet.microsoft.com/zh-cn/download/dotnet-framework)

### 安装与使用
1. 下载最新的 Release 版本并解压。
2. 双击 `gokao_optimized.exe` 即可运行。
3. 启动后系统托盘出现图标，并弹出提示「设置可在系统托盘右键打开」。
4. 右键托盘图标 → **设置**，调整透明度、字体、颜色、动画等。
5. 勾选「窗口可透过」可让鼠标穿透倒计时窗口操作后方程序。
6. 在事件列表中勾选事件前的复选框即可在桌面显示该事件的倒计时窗口，双击事件列表项也可快速切换窗口显隐。
7. 打开壁纸素材库窗口，可在底部配置「倒计时自动切换壁纸」规则：选择事件、填写剩余天数、选取壁纸后点击添加，剩余天数跨过阈值时桌面壁纸自动切换。

### 命令行参数
| 参数 | 说明 |
| ---- | ---- |
| （无参数） | 双击 exe 直接运行；若已有实例在运行则提示后退出 |
| `--event 事件名` / `-e 事件名` | 向已运行实例发送命令，激活该事件的倒计时窗口（通过命名管道通信） |

示例：`gokao_optimized.exe --event 考研`

### 开发调试
```bash
# 克隆仓库
git clone https://gitcode.com/FIZ_xingchen/gokao.git

# 用 Visual Studio 2022+ 打开 gokao_optimized.slnx
# 生成解决方案即可编译运行
```

---

## 📁 项目结构

```
gokao_optimized/
├── App.config                      应用程序配置文件
├── App.xaml / App.xaml.cs          应用程序入口（单实例互斥、全局异常捕获、全局样式合并）
├── Models/
│   └── CustomEvent.cs              自定义事件模型（名称 + 日期 + 活跃状态）
├── Views/                          窗口视图
│   ├── MainWindow.xaml(.cs)        设置主窗口
│   ├── CountdownWindow.xaml(.cs)   倒计时显示窗口（多实例、无边框、可穿透、置顶）
│   ├── ExamMode.xaml(.cs)          考试模式入口窗口（含番茄钟/每日一句开关）
│   ├── ExamCountdownWindow.xaml(.cs) 全屏考试倒计时窗口（滚轮/滑动/左键拖动设置时长）
│   ├── ExamNotesWindow.xaml(.cs)   作业便签/待办窗口（便签 + 待办清单，独立持久化）
│   ├── PomodoroWindow.xaml(.cs)    番茄钟/学习计时窗口（专注/休息时长可自定义）
│   ├── QuoteBubbleWindow.xaml(.cs) 每日一句励志气泡窗口（圆角渐变卡片，自动淡出）
│   └── Windowsucai.xaml(.cs)       壁纸素材库窗口
├── Controls/
│   └── FlipDigitControl.xaml(.cs)  3D 翻页数字控件（渐变卡片+圆角+微光边框+铰链线）
├── Themes/
│   └── Styles.xaml                  全局样式字典（按钮/GroupBox/CheckBox/Slider 统一模板）
├── Services/                       服务工具
│   ├── ConfigManager.cs            INI 配置读写（编码兼容、失败写日志）
│   ├── EventManager.cs             事件管理（列表持久化、活跃事件追踪、背景目录管理）
│   ├── AutoStartManager.cs         开机自启管理
│   ├── LanguageManager.cs          中英文界面语言管理（动态切换即时生效）
│   ├── TrayIconManager.cs          系统托盘管理（图标静态缓存）
│   ├── WallpaperScheduler.cs       壁纸自动切换调度（剩余天数→壁纸规则巡检）
│   ├── DailyQuoteManager.cs        每日一句管理（quotes.json 读取、按天轮换、每天一次）
│   ├── SingleInstanceServer.cs     单实例命令管道（--event 参数切换事件）
│   ├── WindowHelper.cs             窗口显示辅助（最小化唤回）
│   └── LogManager.cs               日志管理（按天分文件）
├── Properties/                     程序集属性
├── ico/                            图标资源
├── daojishi.ico                    程序图标（根目录）
├── wallpaper/                      桌面壁纸素材库
├── bg/events/                      事件专属背景图片（运行时生成）
├── logs/                           运行日志（运行时生成）
├── usersetting.ini                 全局配置文件（运行时生成）
├── gokao_optimized.csproj          项目文件
└── gokao_optimized.slnx            解决方案文件
```

---

## ⚙️ 配置说明

程序使用 Windows INI 格式保存配置，所有配置文件均位于程序目录下：

| 文件 | 说明 |
| ---- | ---- |
| `usersetting.ini` | 全局配置：窗口位置、事件列表、活跃事件列表（`[ActiveEvents]` 节）、全局行为设置；另含番茄钟时长（`[Pomodoro]` 节）、每日一句开关/上次弹出日期（`[DailyQuote]` 节）与启动提示开关（`[State]` 节 `ShowStartupTip`，默认开启） |
| `<事件名>.ini` | 每个事件独立一份（如 `高考.ini`）：该事件专属的样式、背景、位置与壁纸切换规则（`[WallpaperSchedule]` 节） |
| `quotes.json` | 每日一句励志语录（JSON 字符串数组，可手动增删；缺失/损坏时自动回退内置列表） |
| `ExamNotes.ini` | 作业待办数据：便签文本（`[Note]` 节）与待办清单（`[Todo]` 节，含完成状态） |
| `bg/events/<事件名>/` | 事件专属背景图片目录 |
| `logs/` | 运行日志目录（`error_日期.log` / `app_日期.log`） |
|ini|删了就没有了，慎删,支持事件导入|

> 事件样式配置与全局隔离：颜色、字号、字体、透明度、动画、锁定等只写入事件配置；全局 `usersetting.ini` 仅保留事件列表、活跃事件列表与窗口行为（置顶、鼠标穿透等）。每个事件拥有独立的配置文件，切换事件时缺失的配置键自动补默认值，不会串带上一事件的外观。活跃事件列表记录哪些事件有倒计时窗口在桌面显示，重启后自动恢复。

---

# 感谢名单
[GuZhi(2401_86556713)] : 为我提供多处错误和视觉优化~
[Codely AI] : 全局样式提取、多窗口倒计时、赛博风动画、性能优化

## 📄 许可证

本项目使用 **木兰宽松许可证，第2版 (MulanPSL-2.0)**。  
完整的许可证文本见 [LICENSE](./LICENSE)。
