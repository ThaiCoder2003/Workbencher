using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.UIs.ProjectDetailView;
using Workbencher.Windows;

namespace Workbencher.UIs
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ProjectView : UserControl
    {
        private int selectedProjectId = -1;
        private int userId;
        public ObservableCollection<Project> UserProjects { get; set; } = new ObservableCollection<Project>();
        private HubConnection ProjectHub;
        private HubConnection TaskHub;
        public ProjectView(HubConnection projectHub, HubConnection taskHub)
        {
            InitializeComponent();
            ProjectHub = projectHub;
            TaskHub = taskHub;
            RegisterHubHandlers();
            userId = AppSession.Instance.CurrentUserId;
            LstProjects.ItemsSource = UserProjects;
            LoadUserWorkspaceData();
        }
        private void RegisterHubHandlers()
        {
            ProjectHub.On<int>("InvitationReceived", OnInvitationReceived);
            ProjectHub.On<int, int>("ProjectJoinApproved", OnInvitationApproved);
            ProjectHub.On<int, int>("ProjectLeft", OnProjectLeft);
        }

        private void OnInvitationReceived(int invitationId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadUserWorkspaceData();
            });
        }
        private async Task JoinProject(int projectId)
        {
            try
            {
                if (selectedProjectId > -1)
                {
                    await TaskHub.InvokeAsync("LeaveProject", selectedProjectId);
                }

                selectedProjectId = projectId;

                await TaskHub.InvokeAsync("JoinProject", selectedProjectId);

            
            }
            catch (HubException ex)
            {
                MessageBox.Show($"Error joining chat: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnInvitationApproved(int projectId, int _userId)
        {
            if (_userId == userId)
            {
                Dispatcher.Invoke(async () =>
                {

                    LoadUserWorkspaceData();

                    await JoinProject(projectId);

                    PnlTabs.Visibility = Visibility.Visible;
                    WorkspaceFrame.Content = new ProjectDashboardView(selectedProjectId, ProjectHub, TaskHub);

                });
            }
        }
        private void OnProjectLeft(int projectId, int _userId)
        {
            if (_userId == userId)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadUserWorkspaceData();
                    selectedProjectId = -1;
                    PnlTabs.Visibility = Visibility.Visible;
                    WorkspaceFrame.Content = PnlNoProjectSelected;

                });
            }
        }

        public void LoadUserWorkspaceData()
        {
            if (userId == 0)
            {
                MessageBox.Show("User not logged in.");
                return;
            }

            try
            {
                using (var _db = new WorkDbContext()) 
                {
                    var projectIds = _db.ProjectMembers
                        .Where(pm => pm.UserId == userId)
                        .Select(pm => pm.ProjectId)
                        .ToList();

                    var projects = _db.Projects.Where(p => projectIds.Contains(p.Id)).ToList();

                    UserProjects.Clear();
                    foreach (var project in projects)
                    {
                        UserProjects.Add(project);
                    }

                    int inviteCount = _db.Invitations
                        .Count(i => i.ReceiverId == userId && i.Status == "Pending");

                    if (inviteCount > 0)
                    {
                        TxtInviteCount.Text = inviteCount.ToString();
                        BrdInviteBadge.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BrdInviteBadge.Visibility = Visibility.Collapsed;
                    }

                    this.DataContext = this;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading workspaces: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            CreateProjectWindow createWin = new CreateProjectWindow();
            createWin.Owner = Window.GetWindow(this); // Centers it over the application cleanly

            if (createWin.ShowDialog() == true)
            {
                // If they hit create and it passed validation checks, add the returned entity straight to the UI list!
                if (createWin.NewCreatedProject != null)
                {
                    UserProjects.Add(createWin.NewCreatedProject);
                    selectedProjectId = createWin.NewCreatedProject.Id;
                    PnlTabs.Visibility = Visibility.Visible;
                    WorkspaceFrame.Content = new ProjectDashboardView(selectedProjectId, ProjectHub, TaskHub);
                }
            }
        }

        private void ViewInvitations_Click(object sender, RoutedEventArgs e)
        {
            var inviteWin = new InvitationsWindow(ProjectHub);

            inviteWin.Owner = Window.GetWindow(this);
            if (inviteWin.ShowDialog() == true)
            {
                LoadUserWorkspaceData();
            }
        }

        private void Tab_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceFrame.Content = new ProjectDashboardView(selectedProjectId, ProjectHub, TaskHub);
        }

        private void Tab_Tasks_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceFrame.Content = new ProjectTaskAssignView(selectedProjectId, TaskHub);
        }

        private void Tab_Invites_Click(object sender, RoutedEventArgs e)
        {
            OpenInviteTab();
        }

        public void OpenInviteTab()
        {
            // Select the tab
            TabInvite.IsChecked = true;
            WorkspaceFrame.Content = new SendInvitationView(selectedProjectId, ProjectHub); 
        }

        private async void LstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstProjects.SelectedItem is Project selected)
            {
                await JoinProject(selected.Id);
                PnlTabs.Visibility = Visibility.Visible;

                using (var _db = new WorkDbContext())
                {
                    WorkspaceFrame.Content = new ProjectDashboardView(selectedProjectId, ProjectHub, TaskHub);
                }
            }
        }
    }
}
