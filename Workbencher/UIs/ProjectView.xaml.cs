using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualBasic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Config;
using Workbencher.Windows;
using Workbencher.UIs.ProjectDetailView;

namespace Workbencher.UIs
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ProjectView : UserControl
    {
        private int selectedProjectId;
        private int userId;
        private bool isAdmin;
        public ObservableCollection<Project> UserProjects { get; set; } = new ObservableCollection<Project>();
        public ProjectView()
        {
            InitializeComponent();
            userId = AppSession.Instance.CurrentUserId;
            LstProjects.ItemsSource = UserProjects;
            LoadUserWorkspaceData();
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
                }
            }
        }

        private void ViewInvitations_Click(object sender, RoutedEventArgs e)
        {
            var inviteWin = new InvitationsWindow();

            inviteWin.Owner = Window.GetWindow(this);
            if (inviteWin.ShowDialog() == true)
            {
                LoadUserWorkspaceData();
            }
        }

        private void Tab_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceFrame.Content = new ProjectDashboardView(selectedProjectId);
        }

        private void Tab_Tasks_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceFrame.Content = new ProjectTaskAssignView(selectedProjectId);
        }

        private void Tab_Invites_Click(object sender, RoutedEventArgs e)
        {
            OpenInviteTab();
        }

        public void OpenInviteTab()
        {
            // Select the tab
            TabInvite.IsChecked = true;
            WorkspaceFrame.Content = new SendInvitationView(selectedProjectId);
        }

        private void LstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstProjects.SelectedItem is Project selected)
            {
                selectedProjectId = selected.Id;
                PnlTabs.Visibility = Visibility.Visible;

                using (var _db = new WorkDbContext())
                {
                    var membership = _db.ProjectMembers
                        .FirstOrDefault(pm => pm.ProjectId == selectedProjectId && pm.UserId == userId);

                    if (membership.Role == "Admin")
                    {
                        isAdmin = true;
                    }

                    TabAdminTasks.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

                    WorkspaceFrame.Content = new ProjectDetailView.ProjectDashboardView(selectedProjectId);
                }
            }
        }
    }
}
