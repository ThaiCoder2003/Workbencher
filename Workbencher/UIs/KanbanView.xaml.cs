using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Config;
using Workbencher.Windows;
using GongSolutions.Wpf.DragDrop;
using GongDragDrop = GongSolutions.Wpf.DragDrop.DragDrop;
using System.Collections.Generic;
namespace Workbencher.UIs
{
    /// <summary>
    /// Interaction logic for KanbanView.xaml
    /// </summary>
    public partial class KanbanView : UserControl, IDropTarget
    {
        public ObservableCollection<Project> ProjectList { get; set; } = new ObservableCollection<Project>();
        public ObservableCollection<TaskItem> ToDoTaskList { get; set; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> InProgressTaskList { get; set; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> CompletedTaskList { get; set; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> OverdueTaskList { get; set; } = new ObservableCollection<TaskItem>();
        private int userId = AppSession.Instance.CurrentUserId;
        public KanbanView()
        {
            InitializeComponent();
            userId = AppSession.Instance.CurrentUserId;
            if (userId == 0)
            {
                return;
            }
            InitializeProjectFilter();
        }

        private void LoadTask()
        {
            ToDoTaskList.Clear();
            InProgressTaskList.Clear();
            CompletedTaskList.Clear();
            OverdueTaskList.Clear();
            try
            {
                using (var _db = new WorkDbContext())
                {
                    var authorizedProjects = _db.ProjectMembers
                        .Where(pm => pm.UserId == userId)
                        .Select(pm => pm.ProjectId)
                        .ToList();

                    var taskItems = new List<TaskItem>();

                    int selectedIndex = CboWorkspaceFilter.SelectedIndex;
                    // All tasks
                    if (selectedIndex == 0)
                    {
                        taskItems = _db.Tasks
                            .Where(t => (authorizedProjects.Contains(t.ProjectId.Value) && t.AssignedToUserId == userId) || (userId == t.CreatedByUserId && t.ProjectId == null))
                            .OrderBy(t => t.Position)
                            .ToList();

                    }

                    else if (selectedIndex == 1)
                    {
                        taskItems = _db.Tasks
                            .Where(t => userId == t.CreatedByUserId && t.ProjectId == null)
                            .OrderBy(t => t.Position)
                            .ToList();
                    }

                    else if (CboWorkspaceFilter.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int selectedProjectId)
                    {
                        taskItems = _db.Tasks
                            .Where(t => t.AssignedToUserId == userId && t.ProjectId == selectedProjectId)
                            .OrderBy(t => t.Position)
                            .ToList();
                    }

                    bool databaseChanged = false;

                    foreach (TaskItem task in taskItems) 
                    {
                        if (task.Status == "To Do" && task.Deadline >= DateTime.UtcNow) ToDoTaskList.Add(task);
                        else if (task.Status == "In Progress" && task.Deadline >= DateTime.UtcNow) InProgressTaskList.Add(task);
                        else if (task.Status == "Completed") CompletedTaskList.Add(task);
                        else if (task.Status == "Overdue") OverdueTaskList.Add(task);
                        else if (task.Status != "Completed" && task.Status != "Overdue" && task.Deadline < DateTime.UtcNow)
                        {
                            // Update the task status to "Overdue" in the database
                            var taskInDb = _db.Tasks.Find(task.Id);
                            if (taskInDb != null)
                            {
                                taskInDb.Status = "Overdue";
                                _db.Entry(taskInDb).State = EntityState.Modified;
                                databaseChanged = true;
                            }
                            OverdueTaskList.Add(task);
                        }
                    }
                    if (databaseChanged)
                    {
                        _db.SaveChanges();
                    }
                }

                DataContext = this;
            }
            catch (Exception ex)
            {
                // Updated message to reveal if it's a database connectivity issue
                MessageBox.Show($"Database or System Error: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}",
                                "Connection Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskItem && dropInfo.TargetCollection is ObservableCollection<TaskItem>)
            {
                dropInfo.Effects = DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            }
        }

        public void DragEnter(IDropInfo dropInfo)
        {
            // Optional: Add visual feedback when dragging enters a target area
        }

        public void DragLeave(IDropInfo dropInfo)
        {
            // Optional: Remove visual feedback when dragging leaves a target area
        }

        public void DropHint(IDropHintInfo dropHintInfo)
        {
            // Optional: Handle any cleanup after a drop is completed
        }

        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is TaskItem taskToEdit)
            {
                button.IsEnabled = false;
                bool isReadOnly = (taskToEdit.ProjectId != null);

                var editWindow = new EditWindow(taskToEdit, isReadOnly);
                if (editWindow.ShowDialog() == true)
                {
                    LoadTask(); // Refresh layout configurations upon change confirmations
                }
                button.IsEnabled = true;
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is TaskItem taskToDelete)
            {
                bool isReadOnly = (taskToDelete.ProjectId != null);
                if (isReadOnly)
                {
                    MessageBox.Show("This task is restricted!");
                    return;
                }
                try
                {
                    var result = MessageBox.Show($"Are you sure you want to delete the task '{taskToDelete.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        using (var _db = new WorkDbContext())
                        {
                            ObservableCollection<TaskItem> targetCollection = null;
                            if (ToDoTaskList.Contains(taskToDelete))
                                targetCollection = ToDoTaskList;
                            else if (InProgressTaskList.Contains(taskToDelete))
                                targetCollection = InProgressTaskList;
                            else if (CompletedTaskList.Contains(taskToDelete))
                                targetCollection = CompletedTaskList;
                            var taskInDb = _db.Tasks.Find(taskToDelete.Id);
                            if (taskInDb != null)
                            {
                                _db.Tasks.Remove(taskInDb);
                            }

                            if (targetCollection != null)
                            {
                                targetCollection.Remove(taskToDelete);

                                for (int i = 0; i < targetCollection.Count; i++)
                                {
                                    var currentItem = targetCollection[i];
                                    var dbTask = _db.Tasks.Find(currentItem.Id);
                                    if (dbTask != null)
                                    {
                                        dbTask.Position = i;
                                        _db.Entry(dbTask).State = EntityState.Modified;
                                        currentItem.Position = i;
                                    }
                                }
                            }

                            _db.SaveChanges();
                        }
                        // Remove the task from the appropriate ObservableCollection
                        if (taskToDelete.Status == "To Do" || string.IsNullOrEmpty(taskToDelete.Status))
                            ToDoTaskList.Remove(taskToDelete);
                        else if (taskToDelete.Status == "In Progress")
                            InProgressTaskList.Remove(taskToDelete);
                        else if (taskToDelete.Status == "Completed")
                            CompletedTaskList.Remove(taskToDelete);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }   
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskItem task && dropInfo.TargetCollection is ObservableCollection<TaskItem> targetCollection)
            {
                var targetListBox = dropInfo.VisualTarget as ListBox;
                if (targetListBox == null) return;
                string originalStatus = task.Status;
                string targetStatus = "To Do";
                if (targetListBox.Name == "InProgressTasks") targetStatus = "In Progress";
                else if (targetListBox.Name == "CompletedTasks") targetStatus = "Completed";    

                GongDragDrop.DefaultDropHandler.Drop(dropInfo);

                task.Status = targetStatus;

                try
                {
                    using (var _db = new WorkDbContext())
                    {
                        // Update the database with the new status and position of each task in the target collection
                        var draggedTask = _db.Tasks.Find(task.Id);
                        if (draggedTask != null) {
                            draggedTask.Status = targetStatus;
                            _db.Entry(draggedTask).State = EntityState.Modified;
                        }

                        for (int i = 0; i < targetCollection.Count; i++)
                        {
                            var currentItem = targetCollection[i];

                            // Pull the fresh row context from the database
                            var dbTask = _db.Tasks.Find(currentItem.Id);
                            if (dbTask != null)
                            {
                                if (CboWorkspaceFilter.SelectedIndex != 0)
                                {
                                    dbTask.Status = targetStatus;
                                    dbTask.Position = i; // Save its exact layout row sequence index (0, 1, 2...)
                                    _db.Entry(dbTask).State = EntityState.Modified;
                                    // Sync the in-memory object position property too
                                    currentItem.Position = i;
                                }
                            }
                        }


                        // If the task originally came from a different collection, update its position there as well
                        var sourceCollection = dropInfo.DragInfo.SourceCollection as ObservableCollection<TaskItem>;
                        if (sourceCollection != null && sourceCollection != targetCollection)
                        {
                            for (int i = 0; i < sourceCollection.Count; i++)
                            {
                                var currentItem = sourceCollection[i];
                                var dbTask = _db.Tasks.Find(currentItem.Id);
                                if (dbTask != null)
                                {
                                    dbTask.Position = i;
                                    _db.Entry(dbTask).State = EntityState.Modified;
                                    currentItem.Position = i;
                                }
                            }
                        }

                        _db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating task status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CreateTask_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtNewTitle.Text))
            {
                MessageBox.Show("Please enter a task title.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var _db = new WorkDbContext())
                {
                    var foundUser = _db.Users.Find(userId);
                    if (foundUser == null)
                    {
                        MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    int currentToDoCount = _db.Tasks.Count(t => t.Status == "To Do" || string.IsNullOrEmpty(t.Status));
                    var newTask = new TaskItem
                    {
                        Title = TxtNewTitle.Text,
                        Status = "To Do", 
                        Position = currentToDoCount,
                        CreatedByUserId = userId,
                        CreatedByUser = foundUser
                    };

                    if (!string.IsNullOrEmpty(TxtNewDescription.Text))
                        newTask.Description = TxtNewDescription.Text;

                    if (DPDeadline.SelectedDate.HasValue)
                        newTask.Deadline = DateTime.SpecifyKind(DPDeadline.SelectedDate.Value, DateTimeKind.Utc);
                    else 
                        newTask.Deadline = DateTime.UtcNow.AddDays(7);

                    _db.Tasks.Add(newTask);
                    await _db.SaveChangesAsync();
                    ToDoTaskList.Add(newTask);
                }

                TxtNewTitle.Clear();
                TxtNewDescription.Clear();
                DPDeadline.SelectedDate = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeProjectFilter()
        {
            CboWorkspaceFilter.SelectionChanged -= CboWorkspaceFilter_SelectionChanged;
            CboWorkspaceFilter.Items.Clear();

            CboWorkspaceFilter.Items.Add(new ComboBoxItem { Content = "🌍 All Workspaces", Tag = "all" });
            CboWorkspaceFilter.Items.Add(new ComboBoxItem { Content = "👤 Personal Tasks Only", Tag = "personal" });

            try
            {
                using (var db = new WorkDbContext())
                {
                    var userProjectIds = db.ProjectMembers
                        .Where(pm => pm.UserId == userId)
                        .Select(pm => pm.ProjectId)
                        .ToList();

                    var userProjects = db.Projects.Where(p => userProjectIds.Contains(p.Id)).ToList();

                    // 3. Append formal project names directly to items collection
                    foreach (var proj in userProjects)
                    {
                        CboWorkspaceFilter.Items.Add(new ComboBoxItem
                        {
                            Content = $"📁 {proj.Name}",
                            Tag = proj.Id // We use this value property to extract the int Project ID later
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed initializing filters: {ex.Message}");
            }
            CboWorkspaceFilter.SelectedIndex = 0;
            CboWorkspaceFilter.SelectionChanged += CboWorkspaceFilter_SelectionChanged;

            // Load initial data state
            LoadTask();
        }

        private void CboWorkspaceFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTask();
        }
    }
}
