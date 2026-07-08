using System.Windows;
using Workbencher.Database;

namespace Workbencher.Windows
{
    /// <summary>
    /// Interaction logic for ChatInvite.xaml
    /// </summary>
    public partial class ChatInvite : Window
    {
        public string email { get; private set; } = String.Empty;
        public ChatInvite()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Invite_Click(object sender, RoutedEventArgs e)
        {
            string input_email = TxtEmail.Text.Trim();
            using var db = new WorkDbContext();
            {
                var user = db.Users.FirstOrDefault(u => u.Email == input_email);
                if (user == null)
                {
                    MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            email = input_email;
            DialogResult = true;
            Close();
        }
    }
}
