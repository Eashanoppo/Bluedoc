using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BlueDoc.UI.Common
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Running
    }

    public class NotificationItem : ViewModelBase
    {
        private string _title;
        private string _message;
        private NotificationType _type;
        private bool _isDismissed;
        private bool _isIndeterminate;

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public NotificationType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        public DateTime Timestamp { get; } = DateTime.Now;

        public string TimeDisplay => Timestamp.ToString("HH:mm:ss");

        public bool IsRunning => Type == NotificationType.Running;

        public bool IsDismissed
        {
            get => _isDismissed;
            set => SetProperty(ref _isDismissed, value);
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }

        public ICommand DismissCommand { get; set; }

        public void CompleteSuccess(string title, string message, int durationSeconds = 4)
        {
            Title = title;
            Message = message;
            Type = NotificationType.Success;
            IsIndeterminate = false;
            NotificationService.Instance.ScheduleDismiss(this, durationSeconds);
        }

        public void CompleteWarning(string title, string message, int durationSeconds = 5)
        {
            Title = title;
            Message = message;
            Type = NotificationType.Warning;
            IsIndeterminate = false;
            NotificationService.Instance.ScheduleDismiss(this, durationSeconds);
        }

        public void CompleteError(string title, string message, int durationSeconds = 6)
        {
            Title = title;
            Message = message;
            Type = NotificationType.Error;
            IsIndeterminate = false;
            NotificationService.Instance.ScheduleDismiss(this, durationSeconds);
        }
    }

    public class NotificationService : ViewModelBase
    {
        private static readonly Lazy<NotificationService> _instance =
            new Lazy<NotificationService>(() => new NotificationService());

        public static NotificationService Instance => _instance.Value;

        private int _unreadCount;
        private bool _isNotificationCenterOpen;

        public ObservableCollection<NotificationItem> ActiveToasts { get; } =
            new ObservableCollection<NotificationItem>();

        public ObservableCollection<NotificationItem> History { get; } =
            new ObservableCollection<NotificationItem>();

        public int UnreadCount
        {
            get => _unreadCount;
            private set
            {
                if (SetProperty(ref _unreadCount, value))
                {
                    OnPropertyChanged(nameof(HasUnread));
                }
            }
        }

        public bool HasUnread => UnreadCount > 0;

        public bool IsNotificationCenterOpen
        {
            get => _isNotificationCenterOpen;
            set
            {
                if (SetProperty(ref _isNotificationCenterOpen, value) && value)
                {
                    MarkAllAsRead();
                }
            }
        }

        public ICommand ToggleNotificationCenterCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        private NotificationService()
        {
            ToggleNotificationCenterCommand = new RelayCommand(() =>
            {
                IsNotificationCenterOpen = !IsNotificationCenterOpen;
            });

            ClearHistoryCommand = new RelayCommand(ClearHistory);
        }

        public void ShowSuccess(string title, string message, int durationSeconds = 4)
        {
            Show(title, message, NotificationType.Success, durationSeconds);
        }

        public void ShowWarning(string title, string message, int durationSeconds = 5)
        {
            Show(title, message, NotificationType.Warning, durationSeconds);
        }

        public void ShowError(string title, string message, int durationSeconds = 6)
        {
            Show(title, message, NotificationType.Error, durationSeconds);
        }

        public void ShowInfo(string title, string message, int durationSeconds = 4)
        {
            Show(title, message, NotificationType.Info, durationSeconds);
        }

        public NotificationItem ShowRunning(string title, string message)
        {
            var item = new NotificationItem
            {
                Title = title,
                Message = message,
                Type = NotificationType.Running,
                IsIndeterminate = true
            };
            item.DismissCommand = new RelayCommand(() => Dismiss(item));

            RunOnUI(() =>
            {
                ActiveToasts.Insert(0, item);
                History.Insert(0, item);
                UnreadCount++;
            });

            return item;
        }

        public void Show(string title, string message, NotificationType type, int durationSeconds = 4)
        {
            var item = new NotificationItem
            {
                Title = title,
                Message = message,
                Type = type
            };
            item.DismissCommand = new RelayCommand(() => Dismiss(item));

            RunOnUI(() =>
            {
                ActiveToasts.Insert(0, item);
                History.Insert(0, item);
                UnreadCount++;
            });

            if (durationSeconds > 0)
            {
                ScheduleDismiss(item, durationSeconds);
            }
        }

        public void ScheduleDismiss(NotificationItem item, int durationSeconds)
        {
            Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
            {
                RunOnUI(() => Dismiss(item));
            });
        }

        public void Dismiss(NotificationItem item)
        {
            if (item == null) return;
            RunOnUI(() =>
            {
                item.IsDismissed = true;
                ActiveToasts.Remove(item);
            });
        }

        public void ClearHistory()
        {
            RunOnUI(() =>
            {
                History.Clear();
                UnreadCount = 0;
            });
        }

        public void MarkAllAsRead()
        {
            UnreadCount = 0;
        }

        private static void RunOnUI(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(action);
            }
            else
            {
                action?.Invoke();
            }
        }
    }
}

