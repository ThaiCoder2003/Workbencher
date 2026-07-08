using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workbencher.Database.Models
{
    [Table("Tasks")]
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } = null;
        [Required]
        public string Status { get; set; } = "To Do"; // Default status 
        [Required]
        public DateTime Deadline { get; set; } = DateTime.UtcNow.AddDays(7); // Default deadline is 7 days from now
        public int? AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
        [Required]
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public int Position { get; set; } = 0; // Default position is 0
    }
}
