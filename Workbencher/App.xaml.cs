using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using Workbencher.Database;
using Workbencher.Database.Models;
using Workbencher.Windows;
using Workbencher.Config;

namespace Workbencher
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public class UserSessionData
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
        }

        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkerPlace",
            "session.dat"
        );

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ⬇️ UNCOMMENT THIS ONE LINE TEMPORARILY TO WIPE THE OLD FILE ON THE NEXT LAUNCH:
            ClearSession();
            if (CheckSession(out string email))
            {
                // If a session exists, go straight to the main window
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                // Otherwise, show the authentication window
                AuthWindow authWindow = new AuthWindow();
                authWindow.Show();
            }
        }

        /// <summary>
        /// By checking the local identity file, it determines whether or not a session exists.
        /// Every time a user boots up the application already logged in, the app goes straight to main window.
        /// </summary>
        public static bool CheckSession(out string email)
        {
            email = string.Empty;

            try
            {
                if (File.Exists(SessionFilePath))
                {
                    byte[] encryptedBytes = File.ReadAllBytes(SessionFilePath);
                    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

                    string json = Encoding.UTF8.GetString(decryptedBytes);

                    var sessionData = JsonSerializer.Deserialize<UserSessionData>(json);
                    if (sessionData != null && sessionData.UserId > 0 && !string.IsNullOrEmpty(sessionData.Email))
                    {
                        using (var db = new WorkDbContext())
                        {
                            var user = db.Users.FirstOrDefault(u => u.Id == sessionData.UserId && u.Email == sessionData.Email);
                            if (user != null)
                            {
                                AppSession.Instance.CurrentUser = user;
                                email = user.Email;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking session: {ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        /// <summary>
        /// Creates the local identity file to persist the user's session across application restarts after a successful login.
        /// </summary>
        public static void SaveSession(User user)
        {
            try
            {
                string? directory = Path.GetDirectoryName(SessionFilePath);
                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException("Session file path does not have a valid directory.");
                }
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                // 1. Pack data into a small object
                var data = new UserSessionData { UserId = user.Id, Email = user.Email };
                string json = JsonSerializer.Serialize(data);

                // 2. Encrypt the string into safe bytes using current Windows User context
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(json);
                byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.CurrentUser);

                // 3. Write raw encrypted bytes safely to disk
                File.WriteAllBytes(SessionFilePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Deletes the local identity file when a user deliberately logs out.
        /// </summary>
        public static void ClearSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
