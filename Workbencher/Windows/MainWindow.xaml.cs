using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Workbencher.Config;
using Workbencher.UIs;

namespace Workbencher.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView = new();
        private readonly KanbanView _kanbanView = new();
        private readonly CalendarView _calendarView = new();

        private ChatView? _chatView;
        private ProjectView? _projectView;
        private HubConnection _hubConnection;
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
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5001/chathub")
                .WithAutomaticReconnect()
                .Build();

            await _hubConnection.StartAsync();

            await _hubConnection.InvokeAsync(
                "RegisterConnection",
                AppSession.Instance.CurrentUserId);
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
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
                    MainContentPresenter.Content = _kanbanView;
                    break;
                case "Calendar":
                    MainContentPresenter.Content = _calendarView;
                    break;
                case "Chat":
                    _chatView ??= new ChatView(_hubConnection);

                    MainContentPresenter.Content = _chatView;
                    break;

                case "Projects":
                    _projectView ??= new ProjectView();

                    MainContentPresenter.Content = _projectView;
                    break;
            }
        }

        public void OpenProjectChat(int projectId)
        {
            RbChat.IsChecked = true;
            _chatView ??= new ChatView(_hubConnection);

            MainContentPresenter.Content = _chatView;

            _chatView.OpenProjectChat(projectId);
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            App.ClearSession();
            // Disconnect from the SignalR hub if connected
            if (_hubConnection != null)
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                    await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            Close();
        }
    }
}