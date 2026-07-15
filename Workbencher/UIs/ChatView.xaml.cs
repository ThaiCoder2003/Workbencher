using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Windows;

namespace Workbencher.UIs
{
    public class MessageDto
    {
        public int Id { get; set; }

        public int ChatRoomId { get; set; }

        public int SenderId { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
    /// <summary>
    /// Interaction logic for ChatView.xaml
    /// </summary>
    public partial class ChatView : UserControl, INotifyPropertyChanged
    {
        private int _userId;

        private int? _chatRoomId;
        public HubConnection ChatHub { get; private set; }
        public HubConnection ProjectHub { get; private set; }
        public ObservableCollection<Message> Messages { get; set; } = new();
        public ObservableCollection<ChatRoom> GroupChatRooms { get; set; } = new();
        public ObservableCollection<ChatRoom> DirectChatRooms { get; set; } = new();
        private ChatRoom? _activeChatRoom;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool CanChat =>
            ActiveChatRoom != null &&
            ActiveChatRoom.ApprovalStatus == "Approved";
        public ChatRoom? ActiveChatRoom
        {
            get => _activeChatRoom;
            set
            {
                _activeChatRoom = value;
                OnPropertyChanged(nameof(ActiveChatRoom));
                OnPropertyChanged(nameof(CanChat));
            }
        }
        private void RegisterHubHandlers()
        {
            ChatHub.On<int>("InvitationReceived", OnInvitationReceived);
            ChatHub.On<int>("ChatApproved", OnChatApproved);
            ChatHub.On<MessageDto>("ReceiveMessage", OnReceiveMessage);
            ChatHub.On<int>("ChatDeclined", OnChatDeclined);
            ProjectHub.On<int>("ChatJoined", OnChatJoined);
        }
        private void OnInvitationReceived(int roomId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadChats();
            });
        }

        private void OnChatApproved(int roomId)
        {
            _ = Dispatcher.Invoke(async () =>
            {
                LoadChats();
                // Reload ActiveChatRoom
                ActiveChatRoom =
                    GroupChatRooms.FirstOrDefault(r => r.Id == roomId)
                    ?? DirectChatRooms.FirstOrDefault(r => r.Id == roomId);


                if (ActiveChatRoom != null && ActiveChatRoom.Id == roomId)
                {
                    await JoinChat(ActiveChatRoom);
                    OnPropertyChanged(nameof(CanChat));
                }
            });
        }

        private void OnReceiveMessage(MessageDto message)
        {
            Dispatcher.Invoke(() =>
            {
                if (ActiveChatRoom != null && message.ChatRoomId == ActiveChatRoom.Id)
                {
                    LoadMessages();
                }
            });
        }

        private void OnChatDeclined(int roomId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadChats();
                if (ActiveChatRoom != null && ActiveChatRoom.Id == roomId)
                {
                    ActiveChatRoom = null;
                    Messages.Clear();
                }
            });
        }

        private void OnChatJoined(int roomId)
        {
            Dispatcher.Invoke(() =>
            {
                LoadChats();
            });
        }

        public void OpenProjectChat(int projectId)
        {
            using (var db = new WorkDbContext())
            {
                var room = GroupChatRooms
                    .FirstOrDefault(r => r.ProjectId == projectId);

                if (room != null)
                {
                    //  Select the chat room in the UI
                    LstProjectChats.SelectedItem = room;
                    _activeChatRoom = room;
                    LoadMessages();
                }
            }
        }
        public ChatView(HubConnection chatHub, HubConnection projectHub)
        {
            InitializeComponent();
            _userId = AppSession.Instance.CurrentUserId;
            ChatHub = chatHub;
            ProjectHub = projectHub;
            RegisterHubHandlers();
            DataContext = this;
            LoadChats();
        }

        private void LoadChats()
        {
            try
            {
                GroupChatRooms.Clear();
                DirectChatRooms.Clear();
                using (var _db = new WorkDbContext())
                {
                    var groupChats = _db.ChatRooms
                        .Include(cr => cr.Project)
                        .Where(cr => cr.RoomType == "Channel" && cr.Members.Any(m => m.UserId == _userId))
                        .ToList();
                    foreach (var chat in groupChats)
                    {
                        GroupChatRooms.Add(chat);
                    }
                    var directChats = _db.ChatRooms
                        .Include(cr => cr.Members)
                            .ThenInclude(m => m.User)
                        .Where(cr => cr.RoomType == "Direct Message" && cr.Members.Any(m => m.UserId == _userId))
                        .ToList();
                    foreach (var chat in directChats)
                    {
                        DirectChatRooms.Add(chat);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading chatroom data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadMessages()
        {
            try
            {
                Messages.Clear();
                if (_chatRoomId != null)
                {
                    using (var _db = new WorkDbContext())
                    {
                        var messages = _db.ChatMessages
                            .Include(m => m.Sender)
                            .Where(m => m.ChatRoomId == _chatRoomId)
                            .OrderBy(m => m.SentAt)
                            .ToList();
                        foreach (var message in messages)
                        {
                            Messages.Add(message);
                        }
                    }
                }

                if (Messages.Any())
                {
                    ChatMessagesList.ScrollIntoView(Messages.Last());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading message data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void Chat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Triggers when selecting a project channel or DM user
            if (sender == LstProjectChats)
                LstDMChats.SelectedItem = null;
            else if (sender == LstDMChats)
                LstProjectChats.SelectedItem = null;

            if (sender is ListBox listBox && 
                listBox.SelectedItem is ChatRoom selectedChatRoom)
            {
                // Join Chat here
                await JoinChat(selectedChatRoom);
            }
        }

        private async Task JoinChat(ChatRoom chatRoom)
        {
            try
            {
                if (ActiveChatRoom != null && ActiveChatRoom.Id != chatRoom.Id)
                {
                    await ChatHub.InvokeAsync("LeaveRoom", ActiveChatRoom.Id);
                }

                ActiveChatRoom = chatRoom;
                _chatRoomId = chatRoom.Id;

                if (CanChat)
                    await ChatHub.InvokeAsync("JoinRoom", ActiveChatRoom.Id);

                LoadMessages();
            }
            catch (HubException ex)
            {
                MessageBox.Show($"Error joining chat: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void InviteDM_Click(object sender, RoutedEventArgs e)
        {
            ChatInvite invite = new ChatInvite();
            if (invite.ShowDialog() != true)
                return;

            string email = invite.email;
            if (!string.IsNullOrEmpty(email))
            {
                using (var _db = new WorkDbContext())
                {
                    var user = _db.Users.FirstOrDefault(u => u.Email == email);
                    if (user == null)
                    {
                        MessageBox.Show("User with this email does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    try
                    {
                        await ChatHub.InvokeAsync("CreateInvitation", user.Id);
                    }
                    catch (HubException ex)
                    {
                        MessageBox.Show($"Error inviting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    LoadChats();
                }
            }
            else
            {
                MessageBox.Show("Email cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveChatRoom == null ||
                ActiveChatRoom.RoomType != "Direct Message" ||
                ActiveChatRoom.ApprovalStatus != "Pending")
                return;
            int roomId = ActiveChatRoom.Id;
            try 
            {
                await ChatHub.InvokeAsync("ApproveInvitation", ActiveChatRoom.Id);
                ActiveChatRoom =
                    GroupChatRooms.FirstOrDefault(r => r.Id == roomId)
                    ?? DirectChatRooms.FirstOrDefault(r => r.Id == roomId);
                if (ActiveChatRoom != null && ActiveChatRoom.Id == roomId)
                {
                    await JoinChat(ActiveChatRoom);
                    OnPropertyChanged(nameof(CanChat));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error approving invitation: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnDecline_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveChatRoom != null && ActiveChatRoom.RoomType == "Direct Message" && ActiveChatRoom.ApprovalStatus == "Pending")
            {
                try
                {
                    using (var _db = new WorkDbContext())
                    {
                        var chatRoom = _db.ChatRooms
                            .Include(r => r.Members)
                            .FirstOrDefault(r => r.Id == ActiveChatRoom.Id);
                        if (chatRoom != null)
                        {
                            _db.ChatRoomMembers.RemoveRange(chatRoom.Members);
                            _db.ChatRooms.Remove(chatRoom);
                            _db.SaveChanges();
                            DirectChatRooms.Remove(ActiveChatRoom);
                            ActiveChatRoom = null;
                            Messages.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error declining DM: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == textBox.Tag?.ToString())
            {
                textBox.Text = string.Empty;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag?.ToString() ?? "Type a message...";
            }
        }

        private async void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ActiveChatRoom != null && CanChat)
                {
                    string messageText = TxtMessageInput.Text.Trim();
                    if (!string.IsNullOrEmpty(messageText))
                    {
                        await ChatHub.InvokeAsync("BroadcastMessage", ActiveChatRoom.Id, messageText);
                        TxtMessageInput.Text = string.Empty;
                    }
                }
            }
            catch (HubException ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
