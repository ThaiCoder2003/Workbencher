using Microsoft.EntityFrameworkCore;
using Workbencher.Database.Models;
namespace Workbencher.Database
{
    internal class WorkDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatRoomMember> ChatRoomMembers { get; set; }
        public DbSet<Message> ChatMessages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Replace with your local PostgreSQL credentials!
                string connectionString = "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.zyjtijubocwdmcsxdfdx;Password=WeLoveMettaton";

                optionsBuilder.UseNpgsql(connectionString);
            }
        }
    }
}
