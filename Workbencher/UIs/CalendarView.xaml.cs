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
            LoadWorkspaceTasks();
        }

        private void LoadWorkspaceTasks()
        {
            try
            {
                _userId = AppSession.Instance.CurrentUserId;
                using (var _db = new WorkDbContext())
                {
                    var authorizedProjects = _db.ProjectMembers
                        .Where(pm => pm.UserId == _userId)
                        .Select(pm => pm.ProjectId)
                        .ToList();
                    var tasks = _db.Tasks
                        .Where(t => (authorizedProjects.Contains(t.ProjectId.Value) && t.AssignedToUserId == _userId) || (_userId == t.CreatedByUserId && t.ProjectId == null))
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
            foreach (var task in AllTasks)
            {
                Console.WriteLine(task.Deadline);
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
            // Get the local year, month, and day the user clicked on
            int targetYear = selected.Year;
            int targetMonth = selected.Month;
            int targetDay = selected.Day;

            // Display localized date header (e.g., "Thứ Tư, tháng 7 21, 2026")
            TxtSelectedDate.Text = selected.ToString("dddd, MMMM dd, yyyy");

            // Compare date parts directly—bypassing any automatic hour/timezone shifts!
            var dateTasks = AllTasks
                .Where(t => t.Deadline.Year == targetYear &&
                            t.Deadline.Month == targetMonth &&
                            t.Deadline.Day == targetDay)
                .ToList();

            TxtTaskCount.Text = $"{dateTasks.Count} active deadline{(dateTasks.Count == 1 ? "" : "s")} found";
            LstSelectedDayTasks.ItemsSource = dateTasks;
        }
    }
}
