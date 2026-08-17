# ⏳ Gaokao Countdown (Custom Events Supported)

> An elegant Windows desktop countdown tool for the Gaokao exam and other custom events.  
> Automatically calculates remaining days, hours, minutes and seconds, with multiple animation effects and a click-through window.

[![中文](https://img.shields.io/badge/lang-中文-blue.svg)](./README.md)
[![English](https://img.shields.io/badge/lang-English-green.svg)](./README.en.md)

[![Language](https://img.shields.io/badge/language-C%23-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Framework](https://img.shields.io/badge/.NET-Framework%204.8-orange.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MulanPSL2-green.svg)](./LICENSE)

---

## ✨ Highlights

- **📅 Automatic date calculation**  
  Smart detection: if this year's Gaokao (June 7) has not passed, it counts down to this year; otherwise it automatically switches to next year. (Blessing when it hits zero ~)

- **🎞️ Six animation modes**  
  No animation, breathing scale, meteor shower, 3D text flip, cyberpunk, and ice crystal. In flip mode, the digits and Chinese labels follow font and color settings together, with digit cards featuring gradient backgrounds, rounded corners, glow borders and a center hinge line; the breathing color glow is dynamically brightened based on the current text color, so it never overrides your palette; in meteor shower mode, gold semi-transparent stars with light tails drift down at random positions, faint star dots dot the background, and occasionally a bright comet sweeps across (best with a dark background — when the text color is the default white, it automatically switches to a starry blue-violet palette); in cyberpunk mode, the text glows with a neon cyan base color, with a CRT scanline overlay, and every 1.5–3.5 seconds a 5-phase glitch sequence triggers (RGB triple-channel split → horizontal tearing → magenta shift → color tinting → convergence), accompanied by glitch tear bands and occasional vertical misalignment, plus a neon flicker simulating a faulty tube in the idle state; in ice crystal mode, the text has a blue-white frozen glow, 8 snowflakes (❄❅❆) drift down from the top of the window with gentle swaying, and every 4–8 seconds a tilted ice-crack effect triggers — the text is split along a tilted crack line using PathGeometry clipping into upper and lower halves, which slowly separate (400ms progressive offset) → shudder → heal, overlaid with a tilted jagged white crack line, branch cracks and cross-star ice shards (best with a dark background). When the countdown reaches zero, regardless of the selected animation mode, the blessing message is typed out character by character (ice-blue glow + blinking cursor ▎), giving the zero moment a sense of ceremony.

- **🎨 Fully customizable appearance**  
  Adjust opacity, font size, text color and font family freely — changes take effect immediately and are saved automatically. A "Reset to Default Style" button resets all appearance settings in one click and centers the countdown window on screen.

- **🖱️ Click-through window**  
  When enabled, mouse clicks pass through the countdown window to the apps behind it, so it never gets in the way.

- **🔒 Window lock**  
  One-click lock to prevent accidental dragging and misoperation.

- **📌 Always on top**  
  The countdown window stays on top of all windows, keeping important dates visible at all times.

- **🖼️ Background image**  
  Pick a local image as the countdown background; its opacity follows the slider, and each event can have its own background.

- **📋 System tray resident**  
  Automatically stays in the system tray after launch. Right-click menu to open settings, show/hide the countdown, or quit. A startup balloon tip points you to the settings entry (can be disabled via "Show startup tip" in Behavior settings).

- **🌐 Bilingual UI**  
  The interface supports Chinese / English, following the system language automatically, or switched manually in Settings — changes apply immediately.

- **🚀 Auto start on boot**  
  Optionally launch with the system so important dates are always reminded.

- **📝 Multi-event management + multi-window countdown**  
  Add, edit and delete multiple custom events (e.g. postgraduate exam, birthday, project deadline...). Each event has its own style, background and position. Each event in the list has a checkbox — checking it immediately shows that event's countdown window on the desktop; unchecking hides it — supporting multiple countdown windows displayed simultaneously. Double-clicking a list item also toggles the window. The active-event list is persisted automatically and restored on next launch. New events start with the default style and do not inherit the current event's appearance; text color, font size/family, opacity, animation and lock state are all saved per event, so switching events never leaks styles between them. By default, only the "Gaokao" countdown window is shown.

- **🎓 Exam mode**  
  A fullscreen black-background exam countdown: set the exam duration before starting with the mouse wheel, touch swipe, or by dragging the left mouse button up/down (default 02:30:00). Start / Pause / Close buttons, and Esc also closes the window. During the exam, the current time is shown in the bottom-left corner and the system is kept awake (no sleep / display-off on whiteboards); when the countdown ends it rings three times and shows "Time's Up!".

- **📝 Homework to-dos**  
  The exam mode includes a homework note and to-do list: free-form notes, to-dos that can be added (Enter works too), checked off with a strikethrough to mark done, and one-click deletion of all completed items (with a fade-and-slide-out animation). Data is saved automatically to its own `ExamNotes.ini`.

- **🍅 Pomodoro / Study Timer**  
  Built into exam mode: focus / break durations are customizable (1-180 minutes, default 25/5), applied instantly and saved automatically; start / pause / reset, with a three-tone chime and next-phase hint when the countdown ends.

- **💬 Quote of the Day**  
  A checkbox in exam mode enables a daily motivational quote: once a day at startup a beautiful rounded-gradient bubble fades in and out automatically (no focus steal), rotating through 365 built-in quotes by day. Quotes live in `quotes.json` next to the program — add or remove them freely; if the file is missing or corrupted it falls back to the built-in list.

- **🖥️ High-DPI support**  
  PerMonitorV2 DPI awareness: crisp text on high-DPI displays (125%/150%), with live re-scaling when the window is dragged across monitors.

- **📂 Wallpaper library**  
  Built-in wallpaper library: preview images and set one as the Windows desktop wallpaper with one click. Supports Fill / Fit / Stretch / Tile / Center display styles (add wallpapers yourself).

- **🖼️ Automatic wallpaper switching**  
  Switch the desktop wallpaper automatically based on remaining countdown days: configure "N days left → wallpaper" rules per event (up to 20 rules per event). A 30-second background check fires each rule only once when the threshold is crossed; rules whose wallpaper file was deleted are skipped automatically.

- **🔁 Single instance + command pipe**  
  A mutex guarantees only one instance runs; a second double-click shows a notice and exits. A named-pipe channel lets you run `gokao_optimized.exe --event "Event Name"` (or `-e "Event Name"`) to tell the running instance to activate that event's countdown window in one go.

- **📜 Logging**  
  Runtime exceptions are written to the `logs/` directory (error logs + app logs) for easier troubleshooting.

- **💾 Config persistence**  
  Window position, style settings, event list and active events are saved automatically. Global settings live in `usersetting.ini`; each event gets its own `<event-name>.ini`, restored on next launch. The countdown windows that were visible last time are automatically recreated.

- **🎛️ Unified global styles**  
  Control styles for buttons, GroupBox, CheckBox, Slider, etc. are extracted to a global `Themes/Styles.xaml`, shared across all windows via a unified rounded button template and interaction feedback, reducing code duplication.

---

## 📷 Screenshots


![image.png](https://raw.gitcode.com/user-images/assets/10133549/98c8c8c4-7f1c-4755-8be5-38ce6ce767be/image.png 'image.png')

# Window background support
![image.png](https://raw.gitcode.com/user-images/assets/10133549/3b7252b0-231e-4602-99c1-7928d1f02854/image.png 'image.png')

# Wallpaper switcher
![image.png](https://raw.gitcode.com/user-images/assets/10133549/1df96667-9f6a-4992-a4c7-5a88972ca56d/image.png 'image.png')


![image.png](https://raw.gitcode.com/user-images/assets/10133549/f9ded797-146c-4613-b214-d0e7c2aa93b1/image.png 'image.png')
<span style="font-size:14px;"><ins>PS: This release doesn't include any third-party images</ins></span>
![image.png](https://raw.gitcode.com/user-images/assets/10133549/08171031-f52e-4d3c-9045-8a00edda5796/image.png 'image.png')

![image.png](https://raw.gitcode.com/user-images/assets/10133549/f9f220ad-3b23-4684-b956-cd0126d57054/image.png 'image.png')
---

## Quick Start
![image.png](https://raw.gitcode.com/user-images/assets/10133549/c2d238a5-879b-4e4d-8566-cd300479b057/image.png 'image.png')

### Requirements
- Windows 7/8/10/11  
- [.NET Framework 4.8 or higher](https://dotnet.microsoft.com/en-us/download/dotnet-framework)

### Install & Use
1. Download the latest Release and unzip it.
2. Double-click `gokao_optimized.exe` to run.
3. A tray icon appears, and a balloon tip says "Settings can be opened from the system tray right-click menu".
4. Right-click the tray icon → **Settings** to adjust opacity, font, color, animation, etc.
5. Check "Click-through window" to let mouse clicks pass through the countdown window to the apps behind it.
6. Check the checkbox next to an event in the event list to show that event's countdown window on the desktop; double-click a list item to quickly toggle window visibility.
7. Open the wallpaper library window and configure "Automatic wallpaper switching" rules at the bottom: pick an event, enter the days-left threshold, choose a wallpaper and click Add — the desktop wallpaper switches automatically once the threshold is crossed.

### Command-line arguments
| Argument | Description |
| ---- | ---- |
| (none) | Double-click the exe to run; if an instance is already running, a notice is shown and the new process exits |
| `--event "Event Name"` / `-e "Event Name"` | Sends a command to the running instance to activate that event's countdown window (via named pipe) |

Example: `gokao_optimized.exe --event "Postgraduate Exam"`

### Development
```bash
# Clone the repository
git clone https://gitcode.com/FIZ_xingchen/gokao.git

# Open gokao_optimized.slnx with Visual Studio 2022+
# Build the solution to compile and run
```

---

## 📁 Project Structure

```
gokao_optimized/
├── App.config                      Application config file
├── App.xaml / App.xaml.cs          Application entry (single-instance mutex, global exception handling, global style merge)
├── Models/
│   └── CustomEvent.cs              Custom event model (name + date + active state)
├── Views/                          Window views
│   ├── MainWindow.xaml(.cs)        Settings main window
│   ├── CountdownWindow.xaml(.cs)   Countdown display window (multi-instance, borderless, click-through, topmost)
│   ├── ExamMode.xaml(.cs)          Exam mode entry window (Pomodoro / Quote-of-the-day toggle)
│   ├── ExamCountdownWindow.xaml(.cs) Fullscreen exam countdown (wheel / swipe / left-drag to set duration)
│   ├── ExamNotesWindow.xaml(.cs)    Homework notes / to-dos window (persisted separately)
│   ├── PomodoroWindow.xaml(.cs)     Pomodoro / study timer (customizable focus/break durations)
│   ├── QuoteBubbleWindow.xaml(.cs)  Quote-of-the-day bubble (rounded gradient card, auto fade-out)
│   └── Windowsucai.xaml(.cs)       Wallpaper library window
├── Controls/
│   └── FlipDigitControl.xaml(.cs)  3D flip digit control (gradient card + rounded corners + glow border + hinge line)
├── Themes/
│   └── Styles.xaml                  Global style dictionary (unified Button/GroupBox/CheckBox/Slider templates)
├── Services/                       Service utilities
│   ├── ConfigManager.cs            INI config read/write (encoding compatible, logs failures)
│   ├── EventManager.cs             Event management (list persistence, active-event tracking, background dir management)
│   ├── AutoStartManager.cs         Auto-start management
│   ├── LanguageManager.cs          Bilingual UI language management (switches instantly)
│   ├── TrayIconManager.cs          System tray management (cached icon)
│   ├── WallpaperScheduler.cs       Wallpaper auto-switch scheduler (days-left rule checks)
│   ├── DailyQuoteManager.cs        Quote-of-the-day (quotes.json loading, daily rotation, once per day)
│   ├── SingleInstanceServer.cs     Single-instance command pipe (--event switching)
│   ├── WindowHelper.cs             Window show helper (restores minimized windows)
│   └── LogManager.cs               Logging (daily files)
├── Properties/                     Assembly properties
├── ico/                            Icon resources
├── daojishi.ico                    App icon (project root)
├── wallpaper/                      Desktop wallpaper library
├── bg/events/                      Per-event background images (generated at runtime)
├── logs/                           Runtime logs (generated at runtime)
├── usersetting.ini                 Global config file (generated at runtime)
├── gokao_optimized.csproj          Project file
└── gokao_optimized.slnx            Solution file
```

---

## ⚙️ Configuration

The program stores settings as Windows INI files, all located in the program directory:

| File | Description |
| ---- | ----------- |
| `usersetting.ini` | Global config: window position, event list, active-event list (`[ActiveEvents]` section), global behavior settings; also the Pomodoro durations (`[Pomodoro]` section), the quote-of-the-day toggle / last-shown date (`[DailyQuote]` section) and the startup-tip switch (`[State]` section `ShowStartupTip`, enabled by default) |
| `<event-name>.ini` | One per event (e.g. `高考.ini`): that event's style, background, position and wallpaper-switch rules (`[WallpaperSchedule]` section) |
| `quotes.json` | Daily motivational quotes (JSON array of strings; edit freely; falls back to the built-in list if missing/corrupted) |
| `ExamNotes.ini` | Homework to-do data: note text (`[Note]` section) and to-do list (`[Todo]` section, with done state) |
| `bg/events/<event-name>/` | Per-event background image directory |
| `logs/` | Runtime logs (`error_YYYYMMDD.log` / `app_YYYYMMDD.log`) |
| ini | Once deleted it's gone — delete with caution; event import is supported |

> Event style config is isolated from the global config: text color, font size/family, opacity, animation and lock state are written only to the event config; the global `usersetting.ini` keeps only the event list, active-event list and window behaviors (topmost, click-through, etc.). Each event has its own config file; missing keys are filled with defaults on event switch, so the previous event's appearance never leaks over. The active-event list records which events have visible countdown windows and is automatically restored on next launch.

---

# Credits
[GuZhi(2401_86556713)] : provided many bug reports and visual improvements

## 📄 License

This project is licensed under the **Mulan Permissive Software License, Version 2 (MulanPSL-2.0)**.  
See [LICENSE](./LICENSE) for the full license text.
