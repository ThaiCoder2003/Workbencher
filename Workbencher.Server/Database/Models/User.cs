using System.ComponentModel.DataAnnotations;

namespace Workbencher.Database.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = String.Empty;

        [Required]
        public string PasswordHash { get; set; } = String.Empty;
    }
}
