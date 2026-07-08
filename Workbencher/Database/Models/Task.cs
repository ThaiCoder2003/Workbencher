using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Workbencher.Database.Models
{
    [Table("Tasks")]
    public class TaskItem :  INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }

        private string _title = string.Empty;
        private string? _description = string.Empty;
        private string _status = "To Do"; // Default status
        private DateTime _deadline;
        public int? AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
        public int CreatedByUserId { get; set; }
        public required User CreatedByUser { get; set; }
        private int? _projectId;
        private Project? _project;
        private int _position; // For ordering tasks within a column

        [Required]
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); } // 🔥 Triggers UI update
        }

        public string? Description
        {
            get => _description ?? null;
            set { _description = value; OnPropertyChanged(); }
        }

        [Required]
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        [Required]
        public int Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); }
        }

        public DateTime Deadline
        {
            get => _deadline;
            set { _deadline = value; OnPropertyChanged(); }
        }

        public int? ProjectId
        {
            get => _projectId;
            set { _projectId = value; OnPropertyChanged(); }
        }

        public Project? Project
        {
            get => _project;
            set { _project = value; OnPropertyChanged(); }
        }
        // 4. THE INTERFACE IMPLEMENTATION (The notification mechanism)
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
