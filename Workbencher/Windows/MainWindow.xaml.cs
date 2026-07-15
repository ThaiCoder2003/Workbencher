using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.UIs;

namespace Workbencher.Windows
{
    public class HubManager
    {
        public HubConnection ChatHub { get; private set; }
        public HubConnection ProjectHub { get; private set; }
        public HubConnection TaskHub { get; private set; }

        public async Task ConnectAsync(int userId)
        {
            ChatHub = new HubConnectionBuilder()
                .WithUrl("http://localhost:5001/chathub")
                .WithAutomaticReconnect()
                .Build();

            await ChatHub.StartAsync();

            await ChatHub.InvokeAsync(
                "RegisterConnection",
                userId);

            ProjectHub = new HubConnectionBuilder()
            .WithUrl("http://localhost:5001/projecthub")
            .WithAutomaticReconnect()
            .Build();

            await ProjectHub.StartAsync();

            await ProjectHub.InvokeAsync(
                "RegisterConnection",
                userId);

            TaskHub = new HubConnectionBuilder()
            .WithUrl("http://localhost:5001/taskhub")
            .WithAutomaticReconnect()
            .Build();

            await TaskHub.StartAsync();

            await TaskHub.InvokeAsync(
                "RegisterConnection",
                userId);
        }

        public async Task DisconnectAsync()
        {
            if (ChatHub != null)
            {
                await ChatHub.StopAsync();
                await ChatHub.DisposeAsync();
            }

            if (ProjectHub != null)
            {
                await ProjectHub.StopAsync();
                await ProjectHub.DisposeAsync();
            }

            //if (TaskHub != null)
            //{
            //    await TaskHub.StopAsync();
            //    await TaskHub.DisposeAsync();
            //}
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView = new();
        private readonly CalendarView _calendarView = new();

        private KanbanView? _kanbanView;
        private ChatView? _chatView;
        private ProjectView? _projectView;
        private HubManager _hubManager;
        public MainWindow()
        {
            InitializeComponent();
            LoadUserIdentity();
            TxtSectionTitle.Text = "Dashboard";



            SwitchWorkspaceView("Dashboard");
            Loaded += MainWindow_Loaded;
            Closing += Window_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hubManager = new HubManager();
            await _hubManager.ConnectAsync(AppSession.Instance.CurrentUserId);
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            await _hubManager.DisconnectAsync();
        }
        private void LoadUserIdentity()
        {
            // Pull the authenticated identity notes using our static helper!
            if (App.CheckSession(out string email))
            {
                TxtUserEmail.Text = email;
            }
        }

        private void Navigation_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && TxtSectionTitle != null)
            {
                string? destination = rb.Tag?.ToString();
                TxtSectionTitle.Text = $"{destination}";

                if (!string.IsNullOrEmpty(destination))
                {
                    SwitchWorkspaceView(destination);
                }
            }
        }

        private void SwitchWorkspaceView(string viewName)
        {
            // Placeholder: This control container changes its contents 
            // depending on which sidebar option the user clicks.
            switch (viewName)
            {
                case "Dashboard":
                    MainContentPresenter.Content = _dashboardView;
                    break;
                case "Kanban":
                    _kanbanView ??= new KanbanView(_hubManager.TaskHub);
                    MainContentPresenter.Content = _kanbanView;
                    break;
                case "Calendar":
                    MainContentPresenter.Content = _calendarView;
                    break;
                case "Chat":
                    _chatView ??= new ChatView(_hubManager.ChatHub, _hubManager.ProjectHub);

                    MainContentPresenter.Content = _chatView;
                    break;

                case "Projects":
                    _projectView ??= new ProjectView(_hubManager.ProjectHub, _hubManager.TaskHub);

                    MainContentPresenter.Content = _projectView;
                    break;
            }
        }

        public void OpenProjectChat(int projectId)
        {
            RbChat.IsChecked = true;
            _chatView ??= new ChatView(_hubManager.ChatHub, _hubManager.ProjectHub);

            MainContentPresenter.Content = _chatView;

            _chatView.OpenProjectChat(projectId);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            App.ClearSession();
            // Disconnect from the SignalR hub if connected
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            Close();
        }
    }
}