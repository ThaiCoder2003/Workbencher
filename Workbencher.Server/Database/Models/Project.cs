using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Workbencher.Database.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TaskItem>? Tasks { get; set; }
    }
}
