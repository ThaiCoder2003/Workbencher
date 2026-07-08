using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workbencher.Database.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;
        [Required]
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;
        [Required]
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
