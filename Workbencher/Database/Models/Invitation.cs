using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workbencher.Database.Models
{
    [Table("Invitations")]
    public class Invitation
    { 
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        [Required]
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;
        [Required]
        public int ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
        public string? InviteMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
