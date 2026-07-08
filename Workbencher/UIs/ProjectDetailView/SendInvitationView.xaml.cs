using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database.Models;
using Workbencher.Database;
using Workbencher.Windows;
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
        public SendInvitationView(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            _senderId = AppSession.Instance.CurrentUserId;
            DgSentInvites.ItemsSource = SentInvitations;
            CheckAdmin();
            LoadInvitations();
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
                using (var _db = new WorkDbContext())
                {
                    var yourself = await _db.Users
                        .FirstOrDefaultAsync(u => u.Id == _senderId);
                    if (yourself == null)
                    {
                        MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var user = await _db.Users
                        .Where(u => u.Email == email)
                        .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        MessageBox.Show("This email does not exist! Try again!", "Email does not exist!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var invitation = new Invitation
                    {
                        ProjectId = _projectId,
                        SenderId = _senderId,
                        Sender = yourself,
                        ReceiverId = user!.Id,
                        Receiver = user,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        InviteMessage = string.IsNullOrWhiteSpace(message) ? null : message
                    };

                    _db.Invitations.Add(invitation);
                    await _db.SaveChangesAsync();

                    MessageBox.Show("Invitation sent successfully!", "Sent!", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtEmail.Clear();
                    TxtMessage.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error making Invitation: {ex.Message}", "Invitation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
