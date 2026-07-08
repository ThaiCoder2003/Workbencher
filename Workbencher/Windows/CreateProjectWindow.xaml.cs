using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;


namespace Workbencher.Windows
{
    /// <summary>
    /// Interaction logic for CreateProjectWindow.xaml
    /// </summary>
    public partial class CreateProjectWindow : Window
    {
        public Project? NewCreatedProject { get; private set; }
        public CreateProjectWindow()
        {
            InitializeComponent();
        }

        public async void Create_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtProjectName.Text.Trim();
            string description = TxtProjectDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a valid project name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int currentUserId = AppSession.Instance.CurrentUserId;
            if (currentUserId == 0)
            {
                MessageBox.Show("Session expired. Please log in again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var _db = new WorkDbContext())
                {
                    var User = await _db.Users.FindAsync(currentUserId);
                    if (User == null)
                    {
                        MessageBox.Show("Current user not found in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var project = new Project
                    {
                        Name = name,
                        Description = string.IsNullOrEmpty(description) ? null : description
                    };

                    _db.Projects.Add(project);
                    _db.SaveChanges();

                    var adminMembership = new ProjectMember
                    {
                        ProjectId = project.Id,
                        Project = project,
                        UserId = currentUserId,
                        User = User,
                        Role = "Admin"
                    };

                    _db.ProjectMembers.Add(adminMembership);
                    await _db.SaveChangesAsync();

                    // Assign to public property and close dialog successfully
                    NewCreatedProject = project;

                    // Create a ChatRoom for the project
                    var chatRoom = new ChatRoom
                    {
                        ProjectId = project.Id,
                        RoomType = "Channel",
                        ApprovalStatus = "Approved",
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.ChatRooms.Add(chatRoom);

                    await _db.SaveChangesAsync();

                    var chatRoomMember = new ChatRoomMember
                    {
                        ChatRoomId = chatRoom.Id,
                        ChatRoom = chatRoom,
                        UserId = currentUserId,
                        User = User,
                        JoinedAt = DateTime.UtcNow
                    };
                    _db.ChatRoomMembers.Add(chatRoomMember);
                    await _db.SaveChangesAsync();

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to build workspace: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
