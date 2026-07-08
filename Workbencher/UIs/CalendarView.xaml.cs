using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
namespace Workbencher.UIs
{
    /// <summary>
    /// Interaction logic for CalendarView.xaml
    /// </summary>
    public partial class CalendarView : UserControl
    {
        private int _userId;
        public ObservableCollection<TaskItem> AllTasks { get; set; } = new ObservableCollection<TaskItem>();
        public CalendarView()
        {
            InitializeComponent();
            _userId = AppSession.Instance.CurrentUserId;
            LoadWorkspaceTasks();
        }

        private void LoadWorkspaceTasks()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var tasks = _db.Tasks
                        .Where(t => t.AssignedToUserId == _userId || (t.CreatedByUserId == _userId && t.ProjectId == null))
                        .OrderBy(t => t.Deadline)
                        .ToList();
                    AllTasks.Clear();
                    foreach (var task in tasks)
                    {
                        AllTasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateSelectedDisplay(DateTime.Today);
        }

        private void MainWorkspaceCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWorkspaceCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MainWorkspaceCalendar.SelectedDate.Value;
                UpdateSelectedDisplay(selectedDate);
            }
        }

        private void UpdateSelectedDisplay(DateTime selected)
        {
            TxtSelectedDate.Text = selected.ToString("dddd, MMMM dd, yyyy");

            var dateTasks = AllTasks
                .Where(t => t.Deadline.Date == selected.Date)
                .ToList();

            TxtTaskCount.Text = $"{dateTasks.Count} active deadline{(dateTasks.Count == 1 ? "" : "s")} found";

            LstSelectedDayTasks.ItemsSource = dateTasks;
        }
    }
}
