using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace gokao.Views
{
    /// <summary>
    /// 便签/待办窗口：考试模式下的记事工具。
    /// 便签为自由多行文本，待办支持添加/勾选/删除；
    /// 数据持久化到程序目录下的 ExamNotes.ini（独立于 usersetting.ini）。
    /// </summary>
    public partial class ExamNotesWindow : Window
    {
        private static readonly string NotesPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExamNotes.ini");

        private const string NoteSection = "Note";
        private const string NoteKey = "Text";
        private const string TodoSection = "Todo";
        private const string TodoCountKey = "Count";

        /// <summary>待办项：Text 显示内容，Done 是否已完成（勾选）</summary>
        public class TodoItem
        {
            public string Text { get; set; }
            public bool Done { get; set; }
        }

        private readonly ObservableCollection<TodoItem> _todos = new ObservableCollection<TodoItem>();

        public ExamNotesWindow()
        {
            InitializeComponent();
            LoadNotes();
            todoList.ItemsSource = _todos;
        }

        // ── 加载 ──

        private void LoadNotes()
        {
            noteText.Text = ConfigManager.ReadString(NotesPath, NoteSection, NoteKey, "");
            int count = 0;
            int.TryParse(ConfigManager.ReadString(NotesPath, TodoSection, TodoCountKey, "0"), out count);
            for (int i = 0; i < count; i++)
            {
                string text = ConfigManager.ReadString(NotesPath, TodoSection, $"Item{i}", "");
                if (string.IsNullOrEmpty(text)) continue;
                _todos.Add(new TodoItem
                {
                    Text = text,
                    Done = ConfigManager.ReadBool(NotesPath, TodoSection, $"Done{i}", false)
                });
            }
        }

        // ── 保存 ──

        private void SaveNote()
        {
            ConfigManager.WriteString(NotesPath, NoteSection, NoteKey, noteText.Text);
        }

        /// <summary>保存待办列表：写入新项后清空超出部分的旧键，避免残留脏数据</summary>
        private void SaveTodos()
        {
            int oldCount = 0;
            int.TryParse(ConfigManager.ReadString(NotesPath, TodoSection, TodoCountKey, "0"), out oldCount);
            ConfigManager.WriteString(NotesPath, TodoSection, TodoCountKey, _todos.Count.ToString());
            for (int i = 0; i < _todos.Count; i++)
            {
                ConfigManager.WriteString(NotesPath, TodoSection, $"Item{i}", _todos[i].Text);
                ConfigManager.WriteBool(NotesPath, TodoSection, $"Done{i}", _todos[i].Done);
            }
            for (int i = _todos.Count; i < oldCount; i++)
            {
                ConfigManager.WriteString(NotesPath, TodoSection, $"Item{i}", "");
                ConfigManager.WriteString(NotesPath, TodoSection, $"Done{i}", "");
            }
        }

        // ── 事件 ──

        private void NoteText_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveNote(); // 便签失焦即保存
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            AddTodo();
        }

        private void TodoInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTodo();
                e.Handled = true;
            }
        }

        private void AddTodo()
        {
            string text = todoInput.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            _todos.Add(new TodoItem { Text = text, Done = false });
            todoInput.Clear();
            todoInput.Focus();
            SaveTodos();
        }

        private void Todo_Checked(object sender, RoutedEventArgs e) => SaveTodos();

        private void Todo_Unchecked(object sender, RoutedEventArgs e) => SaveTodos();

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            // 删除全部已勾选（完成）的事项：先播放"变淡+滑出"动画，动画结束后再移除
            var doneItems = _todos.Where(t => t.Done).ToList();
            if (doneItems.Count == 0) return;

            var duration = TimeSpan.FromMilliseconds(220);
            foreach (var item in doneItems)
            {
                if (todoList.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)
                {
                    var slide = new TranslateTransform();
                    container.RenderTransform = slide;
                    // 变淡 + 向左滑出
                    container.BeginAnimation(OpacityProperty,
                        new DoubleAnimation(1.0, 0.0, duration));
                    slide.BeginAnimation(TranslateTransform.XProperty,
                        new DoubleAnimation(0.0, -60.0, duration));
                }
            }

            await Task.Delay(260);
            foreach (var item in doneItems) _todos.Remove(item);
            SaveTodos();
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveNote();
            SaveTodos();
            base.OnClosed(e);
        }
    }
}
