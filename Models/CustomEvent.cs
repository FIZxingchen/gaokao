using System;
using System.ComponentModel;

namespace gokao.Models
{
    /// <summary>
    /// 自定义事件模型：名称 + 目标日期，支持属性变更通知用于列表绑定。
    /// </summary>
    public class CustomEvent : INotifyPropertyChanged
    {
        private string _name;
        private DateTime _date;
        private bool _isActive;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); OnPropertyChanged(nameof(DisplayName)); }
        }

        /// <summary>该事件是否有活跃的倒计时窗口（对应列表中的复选框）</summary>
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public string DisplayName => $"{Name} ({Date:yyyy-MM-dd})";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
