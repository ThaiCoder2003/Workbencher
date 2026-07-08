using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workbencher.Database.Models
{
    [Table("ProjectMembers")]
    public class ProjectMember
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ProjectId { get; set; }
        public required Project Project { get; set; }

        [Required]
        public int UserId { get; set; }
        public required User User { get; set; }
        [Required]
        public string Role { get; set; } = "Member";
        [Required]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
