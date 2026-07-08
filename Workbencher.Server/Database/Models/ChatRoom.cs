using System.ComponentModel.DataAnnotations;
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
    }
}
