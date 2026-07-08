using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Workbencher.Database;
using Workbencher.Database.Models;

namespace Workbencher.HubPlace
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
    public class ChatHub : Hub
    {

        private static readonly ConcurrentDictionary<string, int> _connectionUsers = new();
        public Task RegisterConnection(int userId)
        {
            UserConnectionManager.AddConnection(userId, Context.ConnectionId);
            _connectionUsers[Context.ConnectionId] = userId;
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionUsers.TryRemove(Context.ConnectionId, out int userId))
            {
                UserConnectionManager.RemoveConnection(userId, Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }
        public async Task CreateInvitation(int recipientId)
        {
            try
            {
                var inviterId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    if (inviterId == recipientId)
                        throw new HubException("You cannot invite yourself to a direct message room.");

                    if (!_db.Users.Any(u => u.Id == inviterId) || !_db.Users.Any(u => u.Id == recipientId))
                        throw new HubException("One or both users do not exist.");

                    var existingRoom = await _db.ChatRooms
                        .Include(r => r.Members)
                        .FirstOrDefaultAsync(r =>
                            r.RoomType == "Direct Message" &&
                            r.Members.Any(m => m.UserId == inviterId) &&
                            r.Members.Any(m => m.UserId == recipientId));

                    if (existingRoom != null)
                        throw new Exception("A direct message room already exists between these users.");

                    var room = new ChatRoom
                    {
                        RoomType = "Direct Message",
                        ApprovalStatus = "Pending",
                        InvitedByUserId = inviterId,
                    };


                    room.Members.Add
                    (
                        new ChatRoomMember
                        {
                            ChatRoomId = room.Id,
                            ChatRoom = room,
                            UserId = inviterId,
                            User = await _db.Users.FindAsync(inviterId) ?? throw new HubException("Inviter user not found."),
                            JoinedAt = DateTime.UtcNow
                        }
                    );

                    room.Members.Add
                    (
                        new ChatRoomMember
                        {
                            ChatRoomId = room.Id,
                            ChatRoom = room,
                            UserId = recipientId,
                            User = await _db.Users.FindAsync(recipientId) ?? throw new HubException("Recipient user not found."),
                            JoinedAt = DateTime.UtcNow
                        }
                    );
                    _db.ChatRooms.Add(room);

                    await _db.SaveChangesAsync();

                    foreach (var connection in UserConnectionManager.GetConnections(recipientId))
                    {
                        await Clients.Client(connection)
                            .SendAsync("InvitationReceived", room.Id);
                    }
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error creating invitation: {ex.Message}");
            }
        }

        public async Task ApproveInvitation(int chatRoomId)
        {
            try
            {
                var userId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var room = await _db.ChatRooms
                        .Include(r => r.Members)
                        .FirstOrDefaultAsync(r => r.Id == chatRoomId);
                    if (room == null)
                        throw new HubException("Chat room not found.");
                    if (!room.Members.Any(m => m.UserId == userId))
                        throw new HubException("User is not a member of this chat room.");
                    if (room.InvitedByUserId == userId)
                        throw new HubException("You cannot approve your own invitation.");
                    if (room.ApprovalStatus == "Approved")
                        throw new HubException("This room has already been approved.");
                    room.ApprovalStatus = "Approved";
                    await _db.SaveChangesAsync();
                    foreach (var member in room.Members)
                    {
                        foreach (var connection in UserConnectionManager.GetConnections(member.UserId))
                        {
                            await Clients.Client(connection)
                                .SendAsync("ChatApproved", room.Id);
                        }
                    }
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error creating invitation: {ex.Message}");
            }
        }
        public async Task DeclineInvitation(int chatRoomId)
        {
            try
            {
                var userId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var room = await _db.ChatRooms
                        .Include(r => r.Members)
                        .FirstOrDefaultAsync(r => r.Id == chatRoomId);
                    if (room == null)
                        throw new HubException("Chat room not found.");
                    var memberIds = room.Members
                    .Select(m => m.UserId)
                    .ToList();
                    if (room == null)
                        throw new HubException("Chat room not found.");
                    if (!room.Members.Any(m => m.UserId == userId))
                        throw new HubException("User is not a member of this chat room.");
                    if (room.InvitedByUserId == userId)
                        throw new HubException("You cannot decline your own invitation.");
                    if (room.ApprovalStatus == "Approved")
                        throw new HubException("This room has already been approved.");
                    // Remove all ChatRoomMembers of the Room ID
                    _db.ChatRoomMembers.RemoveRange(room.Members);
                    _db.ChatRooms.Remove(room);
                    await _db.SaveChangesAsync();
                    foreach (var memberId in memberIds)
                    {
                        foreach (var connection in UserConnectionManager.GetConnections(memberId))
                        {
                            await Clients.Client(connection)
                                .SendAsync("ChatDeclined", room.Id);
                        }
                    }
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error declining invitation: {ex.Message}");
            }
        }
        public async Task JoinRoom(int chatRoomId)
        {
            try
            {
                var userId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var room = await _db.ChatRooms
                        .Include(r => r.Members)
                        .FirstOrDefaultAsync(r => r.Id == chatRoomId);
                    if (room == null)
                        throw new HubException("Chat room not found.");

                    // Must belong to the room
                    if (!room.Members.Any(m => m.UserId == userId))
                        throw new HubException("User is not a member of this chat room.");
                    // DM not approved yet
                    if (room.RoomType == "Direct Message" &&
                        room.ApprovalStatus != "Approved")
                        throw new HubException("Direct message room has not been approved yet.");

                    // Join the SignalR group for this chat room
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{chatRoomId}");
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error creating invitation: {ex.Message}");
            }
        }

        // 2. Leave the room when switching conversations
        public async Task LeaveRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{chatRoomId}");
        }
        public async Task BroadcastMessage(int chatRoomId, string text)
        {
            try 
            { 
                var userId = _connectionUsers[Context.ConnectionId];
                using var db = new WorkDbContext();

                var room = db.ChatRooms
                    .Include(r => r.Members)
                    .FirstOrDefault(r => r.Id == chatRoomId);

                if (room == null)
                    throw new HubException("Chat room not found.");

                if (!room.Members.Any(m => m.UserId == userId))
                    throw new HubException("User is not a member of this chat room.");

                // No chatting until approved
                if (room.RoomType == "Direct Message"
                    && room.ApprovalStatus != "Approved")
                    throw new HubException("Direct message room has not been approved yet.");
                var sentAt = DateTime.UtcNow;
                var message = new Message
                {
                    ChatRoomId = chatRoomId,
                    SenderId = userId,
                    MessageText = text,
                    SentAt = sentAt,
                };

                db.ChatMessages.Add(message);
                await db.SaveChangesAsync();

                var sender = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new HubException("Sender user not found.");
                var dto = new MessageDto
                {
                    ChatRoomId = chatRoomId,
                    SenderId = userId,
                    SenderName = sender.Email,
                    Content = text,
                    SentAt = sentAt,
                };
                await Clients.Group($"Room_{chatRoomId}")
                    .SendAsync("ReceiveMessage", dto);
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error creating invitation: {ex.Message}");
            }
        }
    }
}
