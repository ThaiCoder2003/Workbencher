using Workbencher.Database.Models;

namespace Workbencher.Config
{
    public class AppSession
    {
        // Thread-safe Singleton Instance
        private static AppSession? _instance;
        public static AppSession Instance => _instance ??= new AppSession();

        private AppSession() 
        {
            CurrentUser = null!;
        }

        // The globally accessible properties
        public User CurrentUser { get; set; }
        public int CurrentUserId => CurrentUser?.Id ?? 0;
    }
}
