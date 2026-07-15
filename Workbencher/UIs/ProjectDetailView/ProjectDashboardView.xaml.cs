using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Windows;

namespace Workbencher.UIs.ProjectDetailView
{

    /// <summary>
    /// Interaction logic for ProjectDashboardView.xaml
    /// </summary>
    public partial class ProjectDashboardView : UserControl
    {
        private static T? FindParent<T>(DependencyObject child)
        where T : DependencyObject
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
        private int _projectId;
        private HubConnection ProjectHub;
        private HubConnection TaskHub;
        public ProjectDashboardView(int projectId, HubConnection projectHub, HubConnection taskConnection)
        {
            InitializeComponent();
            _projectId = projectId;
            ProjectHub = projectHub;
            TaskHub = taskConnection;
            RegisterHubHandlers();
            CalculateStatistics();
            LoadProjectInfo();
            LoadMembers();
        }
        private void RegisterHubHandlers()
        {
            ProjectHub.On<int, int>("ProjectJoinApproved", OnInvitationApproved);
            ProjectHub.On<int, int>("ProjectLeft", OnProjectLeft);
            TaskHub.On<int>("TaskAssigned", OnTaskAssigned);
            TaskHub.On<int>("TaskDeleted", OnTaskDeleted);
            TaskHub.On<int>("TaskStatusUpdated", OnTaskStatus);
        }

        // Remove the duplicate OnTaskAssigned method definition
        private void OnTaskAssigned(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                CalculateStatistics();
            });
        }

        private void OnTaskDeleted(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                CalculateStatistics();
            });
        }

        private void OnTaskStatus(int taskId)
        {
            Dispatcher.Invoke(() =>
            {
                CalculateStatistics();
            });
        }
        private void OnInvitationApproved(int projectId, int userId)
        {
            Dispatcher.Invoke(() =>
            {
                if (projectId == _projectId)
                {
                    if (userId != AppSession.Instance.CurrentUserId)
                    {
                        LoadMembers();
                    }
                }
            });
        }
        private void OnProjectLeft(int projectId, int userId)
        {
            Dispatcher.Invoke(() =>
            {
                if (projectId == _projectId)
                {
                    if (userId != AppSession.Instance.CurrentUserId)
                    {
                        LoadMembers();
                    }
                }
            });
        }
        private void LoadProjectInfo()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var project = _db.Projects.FirstOrDefault(p => p.Id == _projectId);
                    if (project != null)
                    {
                        TxtProjectDescription.Text = project.Description;
                        TxtCreatedDate.Text = $"Created on: {project.CreatedAt.ToString("MMMM dd, yyyy")}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateStatistics()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    bool isAdmin = _db.ProjectMembers
                        .Any(pm => pm.ProjectId == _projectId && pm.UserId == AppSession.Instance.CurrentUserId && pm.Role == "Admin");
                    int taskCount = _db.Tasks
                        .Where(t => t.ProjectId == _projectId)
                        .Count();

                    TxtTotalTasks.Text = taskCount.ToString();

                    int completedTaskCount = _db.Tasks
                        .Where(t => t.ProjectId == _projectId && t.Status == "Completed")
                        .Count();

                    if (taskCount > 0) 
                    {
                        double percentage = ((double)completedTaskCount / taskCount) * 100;
                        TxtCompletionPercentage.Text = $"{Math.Round(percentage)}%";
                        ProgressProject.Value = percentage;
                    }

                    else
                    {
                        TxtCompletionPercentage.Text = "0%";
                        ProgressProject.Value = 0;
                    }
                }

                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    TxtTeamSize.Text = $"{members.Count} Members";
                    LvTeamRoster.ItemsSource = members;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenChat_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Window.GetWindow(this);

            mainWindow.OpenProjectChat(_projectId);
        }

        private void BtnInvite_Click(object sender, RoutedEventArgs e)
        {
            var projectView = FindParent<ProjectView>(this);

            if (projectView != null)
            {
                projectView.OpenInviteTab();
            }
        }

        private async void BtnLeave_Click(object sender, RoutedEventArgs e)
        {
            using (var _db = new WorkDbContext())
            {
                var member = _db.ProjectMembers
                    .FirstOrDefault(pm => pm.ProjectId == _projectId && pm.UserId == AppSession.Instance.CurrentUserId);
                if (member != null)
                {
                    var result = MessageBox.Show("Are you sure you want to leave this project?", "Confirm Leave", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Remove yourself from the chat room, then remove yourself from the project
                        _db.ChatRoomMembers.RemoveRange(_db.ChatRoomMembers.Where(crm => crm.ChatRoom.ProjectId == _projectId && crm.UserId == member.UserId));
                        _db.ProjectMembers.Remove(member);
                        await _db.SaveChangesAsync();
                        MessageBox.Show("You have left the project.", "Project Left", MessageBoxButton.OK, MessageBoxImage.Information);
                        var projectView = FindParent<ProjectView>(this);
                        projectView?.LoadUserWorkspaceData();
                    }
                }
                else
                {
                    MessageBox.Show("You are not a member of this project.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}