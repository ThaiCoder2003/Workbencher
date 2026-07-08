using System.ComponentModel.DataAnnotations;
namespace Workbencher.Database.Models
{
    public class ChatRoomMember
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ChatRoomId { get; set; }
        public required ChatRoom ChatRoom { get; set; }
        [Required]
        public int UserId { get; set; }
        public required User User { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
