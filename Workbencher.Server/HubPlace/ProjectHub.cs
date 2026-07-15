using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Concurrent;
using Workbencher.Database;
using Workbencher.Database.Models;

namespace Workbencher.HubPlace
{

    public class ProjectHub : Hub
    {
        private static readonly ConcurrentDictionary<string, int> _connectionUsers = new();
        public Task RegisterConnection(int userId)
        {
            ProjectConnectionManager.Instance.AddConnection(userId, Context.ConnectionId);
            _connectionUsers[Context.ConnectionId] = userId;
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionUsers.TryRemove(Context.ConnectionId, out int userId))
            {
                ProjectConnectionManager.Instance.RemoveConnection(userId, Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task InviteToProject(string email, string? message, int projectId)
        {
            try
            {
                var inviterId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
                        ?? throw new HubException("Invitee not found!");
                    int inviteeId = user.Id;
                    if (inviteeId == inviterId)
                        throw new HubException("You cannot invite yourself to a project.");
                    var project = await _db.Projects
                        .FirstOrDefaultAsync(pr => pr.Id == projectId) ?? throw new HubException("Project not found!");
                    var projectMember = await _db.ProjectMembers
                        .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.UserId == inviteeId);
                    if (projectMember != null)
                        throw new HubException("This user is already a member!");

                    var sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == inviterId)
                        ?? throw new HubException("Sender not found!");


                    var invitation = new Invitation
                    {
                        ProjectId = projectId,
                        SenderId = inviterId,
                        Sender = sender,
                        ReceiverId = user.Id,
                        Receiver = user,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        InviteMessage = string.IsNullOrWhiteSpace(message) ? null : message
                    };

                    _db.Invitations.Add(invitation);
                    await _db.SaveChangesAsync();

                    foreach (var connection in ProjectConnectionManager.Instance.GetConnections(inviteeId))
                    {
                        await Clients.Client(connection)
                            .SendAsync("InvitationReceived", projectId);
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

        public async Task AcceptInvitation(int invitationId)
        {
            try
            {
                var userId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                        ?? throw new HubException("Invitee not found!");

                    var invitation = await _db.Invitations.Include(inv => inv.Project)   // 👈 Include the first property
    .Include(inv => inv.Receiver).FirstOrDefaultAsync(u => u.Id == invitationId && u.ReceiverId == userId)
                        ?? throw new HubException("Invitation not found");

                    // Update status here
                    invitation.Status = "Accepted";

                    // Add project's new member
                    var newMember = new ProjectMember
                    {
                        UserId = userId,
                        User = invitation.Receiver,
                        ProjectId = invitation.ProjectId,
                        Project = invitation.Project,
                        Role = "Member",
                        JoinedAt = DateTime.UtcNow,
                    };

                    await _db.ProjectMembers.AddAsync(newMember);
                    await _db.SaveChangesAsync();
                    // Add member to project chat room

                    var chatRoom = _db.ChatRooms.FirstOrDefault(cr => cr.ProjectId == invitation.ProjectId);
                    if (chatRoom != null)
                    {
                        var newChatRoomMember = new ChatRoomMember
                        {
                            UserId = userId,
                            User = invitation.Receiver,
                            ChatRoomId = chatRoom.Id,
                            ChatRoom = chatRoom,
                            JoinedAt = DateTime.UtcNow,
                        };
                        await _db.ChatRoomMembers.AddAsync(newChatRoomMember);
                        await _db.SaveChangesAsync();

                        foreach (var connection in ProjectConnectionManager.Instance.GetConnections(userId))
                        {
                            await Clients.Client(connection)
                                .SendAsync("ChatJoined", chatRoom.Id, userId);
                        }
                    }

                    var members = await _db.ProjectMembers
                        .Where(prm => prm.ProjectId == invitation.ProjectId)
                        .ToListAsync();

                    // 2. Build a unique list of User IDs to notify (using a HashSet to avoid duplicate sends)
                    var userIdsToNotify = new HashSet<int>(members.Select(m => m.UserId));

                    // 3. FORCE include the original invitation sender (the one who's looking at the SendInvitationView!)
                    userIdsToNotify.Add(invitation.SenderId);

                    // 4. Send the real-time update to everyone associated
                    foreach (var memberId in userIdsToNotify)
                    {
                        foreach (var connection in ProjectConnectionManager.Instance.GetConnections(memberId))
                        {
                            await Clients.Client(connection)
                                .SendAsync("ProjectJoinApproved", invitation.ProjectId, userId);
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
                throw new HubException($"Error approving invitation: {ex.Message}");
            }
        }

        // Pseudocode / Plan:
        // 1. Retrieve the current user's id from the connection map (the decliner).
        // 2. Load the invitation by id, ensuring the receiver matches the current user.
        //    - Include Project, Sender and Receiver navigation properties so we have IDs and optional extra info.
        // 3. Set the invitation status to "Declined" and persist the change.
        // 4. Build a unique set of user IDs to notify: the SenderId and the ReceiverId (the decliner).
        //    - Use a HashSet<int> to avoid duplicate notifications if they are the same user.
        // 5. For each user id in the set, enumerate their active connections via ProjectConnectionManager.
        // 6. Send a single targeted SignalR message "ProjectJoinDeclined" to each connection.
        //    - Include the ProjectId and the userId of the decliner as message payload.
        // 7. Keep existing HubException handling and general exception wrapping to surface user-friendly errors.

        public async Task DeclineInvitation(int invitationId)
        {
            try
            {
                var userId = _connectionUsers[Context.ConnectionId];

                using (var _db = new WorkDbContext())
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                        ?? throw new HubException("Invitee not found!");

                    // Load invitation and related entities (sender/receiver/project)
                    var invitation = await _db.Invitations
                        .Include(inv => inv.Project)
                        .Include(inv => inv.Sender)
                        .Include(inv => inv.Receiver)
                        .FirstOrDefaultAsync(u => u.Id == invitationId && u.ReceiverId == userId)
                        ?? throw new HubException("Invitation not found");

                    invitation.Status = "Declined";

                    await _db.SaveChangesAsync();

                    // Notify only the sender and the receiver (the decliner)
                    var userIdsToNotify = new HashSet<int>
                    {
                        invitation.SenderId,
                        invitation.ReceiverId
                    };

                    foreach (var targetUserId in userIdsToNotify)
                    {
                        foreach (var connection in ProjectConnectionManager.Instance.GetConnections(targetUserId))
                        {
                            // Include decliner id so clients know who declined
                            await Clients.Client(connection)
                                .SendAsync("ProjectJoinDeclined", invitation.ProjectId, userId);
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
                throw new HubException($"Error declining invitation: {ex.Message}");
            }
        }
        public async Task LeaveProject(int projectId)
        {
            try
            {
                var userId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                        ?? throw new HubException("Invitee not found!");

                    var member = await _db.ProjectMembers.Include(inv => inv.Project).FirstOrDefaultAsync(u => u.UserId == userId && u.ProjectId == projectId)
                        ?? throw new HubException("You are not a project member");

                    _db.ChatRoomMembers.RemoveRange(_db.ChatRoomMembers.Where(crm => crm.ChatRoom.ProjectId == projectId && crm.UserId == member.UserId));
                    _db.ProjectMembers.Remove(member);
                    await _db.SaveChangesAsync();

                    var projectMembers = await _db.ProjectMembers
                        .Where(prm => prm.ProjectId == projectId)
                        .ToListAsync();
                    foreach (var projectMember in projectMembers)
                    {
                        foreach (var connection in  ProjectConnectionManager.Instance.GetConnections(projectMember.UserId))
                        {
                            await Clients.Client(connection)
                                .SendAsync("ProjectLeft", projectId, userId);
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
    }
}
