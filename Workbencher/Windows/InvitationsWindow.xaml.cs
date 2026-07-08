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
        public ObservableCollection<Invitation> Invitations { get; set; } = new ObservableCollection<Invitation>();
        public InvitationsWindow()
        {
            InitializeComponent();
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

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Invitation invitation)
            {
                try
                {
                    var result = MessageBox.Show($"Are you sure you want to accept the invitation to '{invitation.Project.Name}'?", "Confirm Accept", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes) 
                    {
                        using (var _db = new WorkDbContext())
                        {
                            var foundInv = _db.Invitations
                                .FirstOrDefault(inv => inv.Id == invitation.Id);

                            
                            if (foundInv != null)
                            {
                                foundInv.Status = "Accepted";

                                _db.Entry(foundInv).State = EntityState.Modified;

                                var newMember = new ProjectMember
                                {
                                    UserId = _userId,
                                    User = invitation.Receiver,
                                    ProjectId = invitation.ProjectId,
                                    Project = invitation.Project,
                                    Role = "Member",
                                    JoinedAt = DateTime.UtcNow,
                                };

                                _db.ProjectMembers.Add(newMember);
                                _db.SaveChanges();

                                // Add the user to the project's chat room
                                var chatRoom = _db.ChatRooms.FirstOrDefault(cr => cr.ProjectId == invitation.ProjectId);
                                if (chatRoom != null)
                                {
                                    var newChatRoomMember = new ChatRoomMember
                                    {
                                        UserId = _userId,
                                        User = invitation.Receiver,
                                        ChatRoomId = chatRoom.Id,
                                        ChatRoom = chatRoom,
                                        JoinedAt = DateTime.UtcNow,
                                    };
                                    _db.ChatRoomMembers.Add(newChatRoomMember);
                                    _db.SaveChanges();
                                }
                            }
                        }

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

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Invitation invitation)
            {
                try
                {
                    var result = MessageBox.Show($"Are you sure you want to decline the invitation to '{invitation.Project.Name}'?", "Confirm Accept", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        using (var _db = new WorkDbContext())
                        {
                            var foundInv = _db.Invitations
                                .FirstOrDefault(inv => inv.Id == invitation.Id);

                            if (foundInv != null)
                            {
                                foundInv.Status = "Declined";

                                _db.Entry(foundInv).State = EntityState.Modified;

                                _db.SaveChanges();
                            }
                        }

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
