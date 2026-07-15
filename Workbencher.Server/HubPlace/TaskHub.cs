using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Concurrent;
using Workbencher.Database;
using Workbencher.Database.Models;

namespace Workbencher.HubPlace
{

    public class TaskHub : Hub
    {
        private static readonly ConcurrentDictionary<string, int> _connectionUsers = new();
        public Task RegisterConnection(int userId)
        {
            TaskConnectionManager.Instance.AddConnection(userId, Context.ConnectionId);
            _connectionUsers[Context.ConnectionId] = userId;
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionUsers.TryRemove(Context.ConnectionId, out int userId))
            {
                TaskConnectionManager.Instance.RemoveConnection(userId, Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinProject(int projectId)
        {
            var userId = _connectionUsers[Context.ConnectionId];
            using (var _db = new WorkDbContext())
            {
                var project = await _db.Projects
                    .FirstOrDefaultAsync(r => r.Id == projectId);
                if (project == null)
                    throw new HubException("Project not found.");

                // Must belong to the room
                var projectMember = await _db.ProjectMembers
                    .FirstOrDefaultAsync(_ => _.ProjectId == projectId && _.UserId == userId);
                if (projectMember == null)
                    throw new HubException("User is not a part of the project.");
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"Project_{projectId}");
            }
        }

        public async Task AssignTask(int assignee, string title, string? description, int projectId, DateTime deadline)
        {
            try
            {
                var assignerId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var assigneeInfo = await _db.Users
                        .FirstOrDefaultAsync(u => u.Id == assignee) ?? throw new HubException("Invitee not found!");
                    int assigner = _connectionUsers[Context.ConnectionId];
                    var assignerInfo = await _db.Users
                        .FirstOrDefaultAsync(u => assigner == u.Id);
                    if (assignerInfo == null)
                    {
                        throw new HubException("User not found.");
            
                    }
                    var projectInfo = await _db.Projects
                        .FirstOrDefaultAsync(p => p.Id == projectId);
                    if (projectInfo == null)
                    {
                        throw new HubException("Task not found.");
                    }

                    int activeTaskCount = _db.Tasks.Count(t => t.ProjectId == projectId && t.Status == "To Do" && t.AssignedToUserId == assignee);


                    var newTask = new TaskItem
                    {
                        Title = title,
                        Description = String.IsNullOrEmpty(description) ? null : description,
                        Status = "To Do",
                        Position = activeTaskCount,
                        ProjectId = projectId,
                        Project = projectInfo,
                        AssignedToUserId = assignee,
                        AssignedToUser = assigneeInfo,
                        CreatedByUserId = assigner,
                        CreatedByUser = assignerInfo,
                        Deadline = deadline
                    };

                    _db.Tasks.Add(newTask);
                    await _db.SaveChangesAsync();
                    foreach (var connection in TaskConnectionManager.Instance.GetConnections(assignee))
                    {
                        await Clients.Client(connection)
                            .SendAsync("TaskMentioned", newTask.Id);
                    }

                    await Clients.Group($"Project_{projectId}")
                        .SendAsync("TaskAssigned", newTask.Id);
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error assigning task: {ex.Message}");
            }
        }

        public async Task EditTask(int taskId, string title, string? description, DateTime? deadline)
        {
            try
            {
                var assignerId = _connectionUsers[Context.ConnectionId];

                using (var _db = new WorkDbContext())
                {

                    var task = await _db.Tasks
                        .FirstOrDefaultAsync(t => t.Id == taskId);
                    if (task == null)
                    {
                        throw new HubException("Task not found.");
                    }
                    var member = await _db.ProjectMembers
                    .FirstOrDefaultAsync(pm =>
                        pm.ProjectId == task.ProjectId &&
                        pm.UserId == assignerId);

                    if (member == null)
                        throw new HubException("Not a member.");

                    if (member.Role != "Admin")
                        throw new HubException("Only project admins can perform this action.");

                    int assignee = task.AssignedToUserId ?? -1;

                    task.Title = title;
                    task.Description = description;
                    if (deadline.HasValue)
                        task.Deadline = deadline.Value;

                    await _db.SaveChangesAsync();

                    foreach (var connection in TaskConnectionManager.Instance.GetConnections(assignee))
                    {
                        await Clients.Client(connection)
                            .SendAsync("TaskMentioned", task.Id);
                    }

                    await Clients.Group($"Project_{task.ProjectId}")
                        .SendAsync("TaskEdited", task.Id);
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error assigning task: {ex.Message}");
            }
        }

        public async Task DeleteTask(int taskId)
        {
            try
            {
                int assignerId = _connectionUsers[Context.ConnectionId];
                using (var _db = new WorkDbContext())
                {
                    var task = await _db.Tasks
                        .FirstOrDefaultAsync(t => t.Id == taskId);
                    if (task == null)
                    {
                        throw new HubException("Task not found.");
                    }

                    var member = await _db.ProjectMembers
                    .FirstOrDefaultAsync(pm =>
                        pm.ProjectId == task.ProjectId &&
                        pm.UserId == assignerId);

                    if (member == null)
                        throw new HubException("Not a member.");

                    if (member.Role != "Admin")
                        throw new HubException("Only project admins can perform this action.");

                    int assignee = task.AssignedToUserId ?? -1;

                    _db.Tasks.Remove(task);
                    await _db.SaveChangesAsync();

                    foreach (var connection in TaskConnectionManager.Instance.GetConnections(assignee))
                    {
                        await Clients.Client(connection)
                            .SendAsync("TaskMentioned", task.Id);
                    }

                    await Clients.Group($"Project_{task.ProjectId}")
                        .SendAsync("TaskDeleted", task.Id);
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error assigning task: {ex.Message}");
            }
        }

        public async Task UpdateTaskStatus(int taskId, string status)
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var task = await _db.Tasks
                        .FirstOrDefaultAsync(t => t.Id == taskId);
                    if (task == null)
                    {
                        throw new HubException("Task not found.");
                    }

                    int assignee = task.AssignedToUserId ?? -1;

                    task.Status = status;
                    await _db.SaveChangesAsync();
                    foreach (var connection in TaskConnectionManager.Instance.GetConnections(assignee))
                    {
                        await Clients.Client(connection)
                            .SendAsync("TaskMentioned", task.Id);
                    }
                    await Clients.Group($"Project_{task.ProjectId}")
                        .SendAsync("TaskStatusUpdated", task.Id);
                }
            }
            catch (HubException)
            {
                throw;      // Already a user-facing error
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, send an error message to the client, etc.)
                throw new HubException($"Error assigning task: {ex.Message}");
            }
        }

        public async Task LeaveProject(int projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        }
    }
}

