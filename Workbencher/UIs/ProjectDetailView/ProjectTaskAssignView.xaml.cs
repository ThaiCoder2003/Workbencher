using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Windows;

namespace Workbencher.UIs.ProjectDetailView
{
    /// <summary>
    /// Interaction logic for ProjectTaskAssignView.xaml
    /// </summary>
    public partial class ProjectTaskAssignView : UserControl
    {
        private int _projectId;
        public ObservableCollection<TaskItem> ProjectTasks { get; set; } = new ObservableCollection<TaskItem>();
        public ProjectTaskAssignView(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;
            DgTasks.ItemsSource = ProjectTasks;
            LoadMembers();
            LoadTasks();
        }

        private void LoadMembers()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var members = _db.ProjectMembers
                        .Where(pm => pm.ProjectId == _projectId)
                        .Include(pm => pm.User)
                        .ToList();

                    CbAssignee.ItemsSource = members;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTasks()
        {
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var tasks = _db.Tasks
                        .Where(task => task.ProjectId == _projectId)
                        .Include(task => task.AssignedToUser)
                        .ToList();

                    ProjectTasks.Clear();

                    foreach (var task in tasks) 
                    {
                        ProjectTasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AssignTask_Click(object sender, EventArgs e)
        {
            string title = TxtTitle.Text.Trim();
            string description = TxtDescription.Text.Trim();

            if (String.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("You must enter a title!", "No Title", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var _db = new WorkDbContext())
                {
                    int targetAssignee = (int)CbAssignee.SelectedValue;
                    int assigner = AppSession.Instance.CurrentUserId;
                    var assignerInfo = await _db.Users
                        .FirstOrDefaultAsync(u => assigner == u.Id);
                    if (assignerInfo == null)
                    {
                        MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var projectInfo = await _db.Projects
                        .FirstOrDefaultAsync(p => p.Id == _projectId);
                    if (projectInfo == null) 
                    {
                        MessageBox.Show("Project not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    DateTime deadline;
                    if (DPDeadline.SelectedDate.HasValue)
                    {
                        deadline = DateTime.SpecifyKind(DPDeadline.SelectedDate.Value, DateTimeKind.Utc);
                    }
                    else
                    {
                        deadline = DateTime.UtcNow.AddDays(7); // Default to 7 days from now
                    }
                    int activeTaskCount = _db.Tasks.Count(t => t.ProjectId == _projectId && t.Status == "To Do");
                    var newTask = new TaskItem
                    {
                        Title = title,
                        Description = String.IsNullOrEmpty(description) ? null : description,
                        Status = "To Do",
                        Position = activeTaskCount,
                        ProjectId = _projectId,
                        Project = projectInfo,
                        AssignedToUserId = targetAssignee,
                        CreatedByUserId = assigner,
                        CreatedByUser = assignerInfo,
                        Deadline = deadline
                    };

                    _db.Tasks.Add(newTask);
                    _db.SaveChanges();

                    ProjectTasks.Add(newTask); // Update UI table array

                    // Clear inputs
                    TxtTitle.Clear();
                    TxtDescription.Clear();
                    DPDeadline.SelectedDate = null;
                    CbAssignee.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is TaskItem selectedTask)
            {
                // Open our standard EditWindow. Since the admin is editing, isReadOnly is false.
                var editWin = new EditWindow(selectedTask, isReadOnly: false);
                editWin.Owner = Window.GetWindow(this);

                if (editWin.ShowDialog() == true)
                {
                    LoadTasks(); // Reload changes upon window context updates
                }
            }
        }
        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is TaskItem selectedTask)
            {
                var result = MessageBox.Show($"Are you sure you want to permanently delete task '{selectedTask.Title}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new WorkDbContext())
                        {
                            var dbTask = db.Tasks.Find(selectedTask.Id);
                            if (dbTask != null)
                            {
                                db.Tasks.Remove(dbTask);
                                db.SaveChanges();

                                ProjectTasks.Remove(selectedTask); // Remove from tracking collection directly
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed dropping target item record: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
