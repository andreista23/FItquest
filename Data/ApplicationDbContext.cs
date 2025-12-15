using Microsoft.EntityFrameworkCore;
using FitQuest.Models;

namespace FitQuest.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Evidence> Evidence { get; set; }
        public DbSet<XPEvent> XPEvents { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<UserQuest> UserQuests { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<TrainerProfile> TrainerProfiles { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserBadge PK compus
            modelBuilder.Entity<UserBadge>()
                .HasKey(ub => new { ub.UserId, ub.BadgeId });

            // UserQuest PK compus
            modelBuilder.Entity<UserQuest>()
                .HasKey(uq => new { uq.UserId, uq.QuestId });
        }
    }
}
