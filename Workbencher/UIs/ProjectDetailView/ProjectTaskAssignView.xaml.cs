using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Windows;

namespace Workbencher.UIs.ProjectDetailView
{
    /// <summary>
    /// Interaction logic for ProjectTaskAssignView.xaml
    /// </summary>
    public partial class ProjectTaskAssignView : UserControl
    {
        private int _projectId;
        private HubConnection TaskHub;
        public ObservableCollection<TaskItem> ProjectTasks { get; set; } = new ObservableCollection<TaskItem>();
        public ProjectTaskAssignView(int projectId, HubConnection taskHub)
        {
            InitializeComponent();
            _projectId = projectId;
            TaskHub = taskHub;
            DgTasks.ItemsSource = ProjectTasks;
            RegisterHubHandlers();
            CheckAdmin();
            LoadMembers();
            LoadTasks();
        }
        private void RegisterHubHandlers()
        {
            TaskHub.On<int>("TaskAssigned", OnTaskAssigned);
            TaskHub.On<int>("TaskEdited", OnTaskEdited);
            TaskHub.On<int>("TaskDeleted", OnTaskDeleted);
            TaskHub.On<int>("TaskStatusUpdated", OnTaskStatus);
        }

        // Remove the duplicate OnTaskAssigned method definition
        private void OnTaskAssigned(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadTasks();
            });
        }

        private void OnTaskEdited(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadTasks();
            });
        }

        private void OnTaskDeleted(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadTasks();
            });
        }

        private void OnTaskStatus(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadTasks();
            });
        }
        private async void CheckAdmin()
        {
            using (var _db = new WorkDbContext()) 
            {
                var role = await _db.ProjectMembers
                    .Where(pm => pm.ProjectId == _projectId &&
                                 pm.UserId == AppSession.Instance.CurrentUserId)
                    .Select(pm => pm.Role)
                    .FirstOrDefaultAsync();

                Assign_Panel.Visibility =
                    role == "Admin"
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void LoadMembers()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var members = _db.ProjectMembers
                        .Where(pm => pm.ProjectId == _projectId)
                        .Include(pm => pm.User)
                        .ToList();

                    CbAssignee.ItemsSource = members;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTasks()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var tasks = _db.Tasks
                        .Where(task => task.ProjectId == _projectId)
                        .Include(task => task.AssignedToUser)
                        .ToList();

                    ProjectTasks.Clear();

                    foreach (var task in tasks) 
                    {
                        ProjectTasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AssignTask_Click(object sender, EventArgs e)
        {
            string title = TxtTitle.Text.Trim();
            string description = TxtDescription.Text.Trim();

            if (String.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("You must enter a title!", "No Title", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int targetAssignee = (int)CbAssignee.SelectedValue;
                DateTime deadline;
                if (DPDeadline.SelectedDate.HasValue)
                {
                    // PROPER CONVERSION: Specify local kind, then convert to UTC
                    deadline = DateTime.SpecifyKind(DPDeadline.SelectedDate.Value, DateTimeKind.Local).ToUniversalTime();
                }
                else
                {
                    deadline = DateTime.UtcNow.AddDays(7);
                }

                await TaskHub.InvokeAsync("AssignTask", targetAssignee, title, String.IsNullOrWhiteSpace(description) ? null : description, _projectId, deadline);

                // Clear inputs
                TxtTitle.Clear();
                TxtDescription.Clear();
                DPDeadline.SelectedDate = null;
                CbAssignee.SelectedIndex = -1;
            }
            catch (HubException ex)
            {
                MessageBox.Show($"Error assigning task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is TaskItem selectedTask)
            {
                // Open our standard EditWindow. Since the admin is editing, isReadOnly is false.
                var editWin = new EditWindow(selectedTask, isReadOnly: false, taskHub: TaskHub);
                editWin.Owner = Window.GetWindow(this);

                if (editWin.ShowDialog() == true)
                {
                    LoadTasks(); // Reload changes upon window context updates
                }
            }
        }
        private async void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is TaskItem selectedTask)
            {
                var result = MessageBox.Show($"Are you sure you want to permanently delete task '{selectedTask.Title}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await TaskHub.InvokeAsync("DeleteTask", selectedTask.Id);
                    }

                    catch (HubException ex)
                    {
                        MessageBox.Show($"Failed dropping target item record: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
