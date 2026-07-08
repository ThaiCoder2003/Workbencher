using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Workbencher.Database;
using Workbencher.Database.Models;

namespace Workbencher.Windows
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void GoToRegister_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
        }

        private void GoToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = LoginEmailTextBox.Text.Trim();
            string password = LoginPasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both email and password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string passwordHash = ComputeSHA256Hash(password);

                // Fix for CS1026 and CS0029:
                // Use Any() to check if a user exists with the given email and password hash. 
                using (var _db = new WorkDbContext())
                {
                    if (_db.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == passwordHash) is User found)
                    {
                        MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Save the session and open the main window
                        App.SaveSession(found);
                        // After logging in, open the main window and close the auth window 
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid email or password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    // This will print the actual missing DLL or type to your debug console
                    System.Diagnostics.Debug.WriteLine(loaderEx?.Message);
                }
                throw;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string email = RegisterEmailTextBox.Text.Trim();
            string password = RegisterPasswordBox.Password;
            string confirmPassword = RegisterRetypePasswordBox.Password;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                string passwordHash = ComputeSHA256Hash(password);
                using (var _db = new WorkDbContext())
                {
                    // Make sure the email is not already registered
                    if (_db.Users.Any(u => u.Email == email))
                    {
                        MessageBox.Show("Email is already registered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    User newUser = new User
                    {
                        Email = email,
                        PasswordHash = passwordHash
                    };
                    _db.Users.Add(newUser);

                    _db.SaveChanges();
                    MessageBox.Show("Registration successful! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    App.SaveSession(newUser);
                    // After logging in, open the main window and close the auth window 
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    Close();
                }
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

        private string ComputeSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                string hash = builder.ToString();
                return hash;
            }

        }
    }
}

