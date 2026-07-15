using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Windows;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
namespace Workbencher.UIs.ProjectDetailView
{
    /// <summary>
    /// Interaction logic for SendInvitationView.xaml
    /// </summary>
    public partial class SendInvitationView : UserControl
    {
        private int _projectId;
        public ObservableCollection<Invitation> SentInvitations { get; set; } = new ObservableCollection<Invitation>();
        private int _senderId;
        public HubConnection ProjectHub { get; private set; }
        public SendInvitationView(int projectId, HubConnection projectHub)
        {
            InitializeComponent();
            _projectId = projectId;
            _senderId = AppSession.Instance.CurrentUserId;
            DgSentInvites.ItemsSource = SentInvitations;
            ProjectHub = projectHub;
            RegisterHubHandlers();
            CheckAdmin();
            LoadInvitations();
        }
        private void RegisterHubHandlers()
        {
            ProjectHub.On<int, int>("ProjectJoinApproved", OnInvitationApprovedAsync);
            ProjectHub.On<int, int>("ProjectJoinDeclined", OnInvitationDeclinedAsync);
        }

        private async Task OnInvitationApprovedAsync(int projectId, int userId)
        {
            await Task.Delay(250);
            await Task.Delay(250);
            if (projectId == _projectId && _senderId != userId)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadInvitations();
                });
            }
        }
        private async Task OnInvitationDeclinedAsync(int projectId, int userId)
        {
            await Task.Delay(250);
            if (projectId == _projectId && _senderId != userId)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadInvitations();
                });
            }
        }
        private void CheckAdmin()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var role = _db.ProjectMembers
                        .Where(pm => pm.UserId == _senderId && pm.ProjectId == _projectId)
                        .Select(pm => pm.Role)
                        .FirstOrDefault();

                    if (role == "Admin")
                    {
                        LogBorder.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LogBorder.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void LoadInvitations()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var invitations = await _db.Invitations
                        .AsNoTracking()
                        .Where(inv => inv.ProjectId == _projectId)
                        .Include(inv => inv.Receiver)
                        .OrderBy(inv => inv.Id)
                        .ToListAsync();

                    SentInvitations.Clear();
                    foreach (var inv in invitations) 
                    {
                        SentInvitations.Add(inv);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendInvitation_Click(object sender, RoutedEventArgs e) 
        {
            string email = TxtEmail.Text.Trim();
            var message = TxtMessage.Text.Trim();
            if (String.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("You must enter an email!", "No Email", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await ProjectHub.InvokeAsync("InviteToProject", email, message, _projectId);

                MessageBox.Show("Invitation sent successfully!", "Sent!", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtEmail.Clear();
                    TxtMessage.Clear();
                LoadInvitations();
                
            }
            catch (HubException ex)
            {
                MessageBox.Show($"Error sending invitation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
