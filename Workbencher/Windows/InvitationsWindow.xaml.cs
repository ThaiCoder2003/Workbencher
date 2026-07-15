using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;

namespace Workbencher.Windows
{
    /// <summary>
    /// Interaction logic for InvitationWindow.xaml
    /// </summary>
    public partial class InvitationsWindow : Window
    {
        private int _userId;
        private HubConnection ProjectHub;
        public ObservableCollection<Invitation> Invitations { get; set; } = new ObservableCollection<Invitation>();
        public InvitationsWindow(HubConnection projectHub)
        {
            InitializeComponent();
            ProjectHub = projectHub;
            RegisterHubHandlers();
            _userId = AppSession.Instance.CurrentUserId;
            LstInvitations.ItemsSource = Invitations;
            LoadInvitations();
        }

        private void LoadInvitations() 
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var invitations = _db.Invitations
                        .Where(inv => inv.ReceiverId == _userId && inv.Status == "Pending")
                        .Include(inv => inv.Sender)
                        .Include(inv => inv.Project)
                        .OrderBy(inv => inv.Id)
                        .ToList();

                    Invitations.Clear();
                    foreach (var inv in invitations)
                    {
                        Invitations.Add(inv);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterHubHandlers()
        {
            ProjectHub.On<int>("InvitationReceived", OnInvitationReceived);
        }

        private void OnInvitationReceived(int projectId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadInvitations();
            });
        }

        private async void Accept_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Invitation invitation)
            {
                try
                {
                    var result = MessageBox.Show($"Are you sure you want to accept the invitation to '{invitation.Project.Name}'?", "Confirm Accept", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes) 
                    {
                        await ProjectHub.InvokeAsync("AcceptInvitation", invitation.Id);

                        MessageBox.Show($"Welcome to '{invitation.Project.Name}'!", "Welcome Alert", MessageBoxButton.OK, MessageBoxImage.Information);
                        Invitations.Remove(invitation);

                        this.DialogResult = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error accepting invitation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Decline_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Invitation invitation)
            {
                try
                {
                    var result = MessageBox.Show($"Are you sure you want to decline the invitation to '{invitation.Project.Name}'?", "Confirm Accept", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        await ProjectHub.InvokeAsync("DeclineInvitation", invitation.Id);

                        MessageBox.Show($"'{invitation.Project.Name}' invitation declined!", "Decline Alert", MessageBoxButton.OK, MessageBoxImage.Information);
                        Invitations.Remove(invitation);

                        this.DialogResult = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error declining invitation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
