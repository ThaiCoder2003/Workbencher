using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;

namespace Workbencher.Windows
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        public ObservableCollection<Project> ProjectList { get; set; } = new ObservableCollection<Project>();
        private HubConnection TaskHub;
        private TaskItem _task;
        public EditWindow(TaskItem task, bool isReadOnly, HubConnection taskHub)
        {
            InitializeComponent();
            _task = task;
            TaskHub = taskHub;
           
            LoadData();
            TxtTitle.Text = _task.Title;
            TxtDescription.Text = _task.Description;
            DPDeadline.SelectedDate = _task.Deadline;

            if (isReadOnly)
            {
                // 🔒 Lock down text inputs
                TxtTitle.IsReadOnly = true;
                TxtDescription.IsReadOnly = true;

                // 🔒 Lock down dropdown and date controls
                DPDeadline.IsEnabled = false;

                // Optional: Change background or add a message label so the user knows why it's locked
                TxtTitle.Background = Brushes.GhostWhite;
                TxtDescription.Background = Brushes.GhostWhite;

                SaveBtn.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    ProjectList.Clear();

                    foreach (var project in _db.Projects.ToList())
                    {
                        ProjectList.Add(project);
                    }

                    this.DataContext = this;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(TxtTitle.Text))
            {
                MessageBox.Show("Title cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to save changes?", "Confirm Save", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            if (_task.ProjectId != null)
            {
                try
                {
                    string title = TxtTitle.Text.Trim();
                    string? description = string.IsNullOrWhiteSpace(TxtDescription.Text)
                        ? null
                        : TxtDescription.Text.Trim();

                    DateTime? deadline = DPDeadline.SelectedDate.HasValue
                        ? DateTime.SpecifyKind(DPDeadline.SelectedDate.Value, DateTimeKind.Local).ToUniversalTime()
                        : null;
                    await TaskHub.InvokeAsync("EditTask", _task.Id, title, description, deadline);
                    this.DialogResult = true;
                    this.Close();
                }
                catch (HubException ex)
                {
                    MessageBox.Show($"Error editing task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            else
            {
                try
                {
                    using (var _db = new WorkDbContext())
                    {
                        var taskToUpdate = _db.Tasks.Find(_task.Id);
                        if (taskToUpdate != null)
                        {
                            taskToUpdate.Title = TxtTitle.Text;
                            taskToUpdate.Description = TxtDescription.Text;
                            taskToUpdate.Deadline = DPDeadline.SelectedDate ?? DateTime.UtcNow.AddDays(7);

                            _db.Entry(taskToUpdate).State = EntityState.Modified;

                            _task.Title = taskToUpdate.Title;
                            _task.Description = taskToUpdate.Description;
                            _task.Deadline = taskToUpdate.Deadline;
                            _db.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error assigning task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
