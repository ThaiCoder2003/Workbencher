using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace Workbencher.UIs
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class DashboardView : UserControl, INotifyPropertyChanged
    {
        private int _userId;
        private int _pendingTaskCount;
        private int _projectCount;
        public int ProjectCount
        {
            get => _projectCount;
            set { _projectCount = value; OnPropertyChanged(); }
        }
        public int PendingTaskCount
        {
            get => _pendingTaskCount;
            set { _pendingTaskCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<TaskItem> UrgentTasks { get; set; } = new ObservableCollection<TaskItem>();
        public DashboardView()
        {
            InitializeComponent();
            DataContext = this;
            this.Loaded += (s, e) => LoadData();
        }
        private void LoadData()
        {
            _userId = AppSession.Instance.CurrentUserId;
            try
            {
                using (var _db = new WorkDbContext())
                {
                    ProjectCount = _db.ProjectMembers
                        .Where(prm => prm.UserId == _userId)
                        .Count();
                    PendingTaskCount = _db.Tasks.Count(t => t.Status != "Completed" && ((t.ProjectId != null && t.AssignedToUserId == _userId) || (t.CreatedByUserId == _userId && t.ProjectId == null)));
                    DateTime closeToDeadline = DateTime.UtcNow.AddDays(3);

                    var urgentTasks =
                        _db.Tasks
                            .Where(t => t.Status != "Completed" && t.Deadline <= closeToDeadline)
                            .OrderBy(t => t.Deadline)
                            .Take(5)
                            .ToList();

                    UrgentTasks.Clear();
                    foreach (var task in urgentTasks)
                    {
                        UrgentTasks.Add(task);
                    }
                }

                this.DataContext = this; 
            } catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is TaskItem task)
            {
                try
                {
                    using (var _db = new WorkDbContext())
                    {
                        var taskToUpdate = _db.Tasks.Find(task.Id);
                        if (taskToUpdate != null)
                        {
                            taskToUpdate.Status = "Completed";
                            _db.SaveChanges();
                            LoadData(); // Refresh the dashboard data

                            // Life update the number of pending tasks in the UI
                            PendingTaskCount = _db.Tasks.Count(t => t.Status != "Completed");
                            this.DataContext = null; // Reset the DataContext to force UI update
                            this.DataContext = this; // Reassign the DataContext to refresh the UI
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating task status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}