using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Workbencher.Config;
namespace Workbencher.Database.Models
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string RoomType { get; set; } = "Direct Message"; // Direct Message or Channel
        public int? ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        [Required]
        public string ApprovalStatus { get; set; } = "Approved"; // Channels default to Approved, DMs default to Pending

        /// <summary>
        /// Tracks who sent the DM invitation.
        /// </summary>
        public int? InvitedByUserId { get; set; }
        public User InvitedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatRoomMember> Members { get; set; } = new List<ChatRoomMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        [NotMapped]
        public string DisplayName
        {
            get
            {
                // Condition 1: It's a Project/Channel
                if (RoomType.Equals("Channel", StringComparison.OrdinalIgnoreCase))
                {
                    // Pull the project name safely from your related Project navigation object
                    return Project != null ? Project.Name : $"Channel (ID: {Id})";
                }

                // Condition 2: It's a Direct Message
                // Find the participant in the room who ISN'T you.
                int currentUserId = AppSession.Instance.CurrentUserId; // Replace this with your global session/logged-in user ID state!

                var otherUser = Members.FirstOrDefault(m => m.UserId != currentUserId)?.User;

                if (otherUser != null)
                {
                    return otherUser.Email;
                }

                return "Saved Messages (You)";
            }
        }
        [NotMapped]
        public bool AmITheInviter
        {
            get
            {
                int currentUserId = AppSession.Instance.CurrentUserId;
                return InvitedByUserId == currentUserId;
            }
        }
    }
}
