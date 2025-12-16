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

            modelBuilder.Entity<UserBadge>()
            .HasKey(ub => new { ub.UserId, ub.BadgeId });

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.User)
                .WithMany() // <- fără u.UserBadges
                .HasForeignKey(ub => ub.UserId);

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges) // aici poți păstra dacă vrei navigație în Badge
                .HasForeignKey(ub => ub.BadgeId);


            // UserQuest PK compus
            modelBuilder.Entity<UserQuest>()
                .HasKey(uq => new { uq.UserId, uq.QuestId });

            modelBuilder.Entity<Badge>().HasData(
            new Badge { Id = 1, Title = "Level 5", Description = "Ai ajuns la nivelul 5!", Criteria = "Reach level 5", ImagePath = "/images/badges/level5.jpeg" },
            new Badge { Id = 2, Title = "Level 10", Description = "Ai ajuns la nivelul 10!", Criteria = "Reach level 10", ImagePath = "/images/badges/level10.jpeg" },
            new Badge { Id = 3, Title = "Level 15", Description = "Ai ajuns la nivelul 15!", Criteria = "Reach level 15", ImagePath = "/images/badges/level15.jpeg" },
            new Badge { Id = 4, Title = "Level 20", Description = "Ai ajuns la nivelul 20!", Criteria = "Reach level 20", ImagePath = "/images/badges/level20.jpeg" },
            new Badge { Id = 5, Title = "Level 25", Description = "Ai ajuns la nivelul 25!", Criteria = "Reach level 25", ImagePath = "/images/badges/level25.jpeg" },
            new Badge { Id = 6, Title = "Level 30", Description = "Ai ajuns la nivelul 30!", Criteria = "Reach level 30", ImagePath = "/images/badges/level30.jpeg" },
            new Badge { Id = 7, Title = "Level 35", Description = "Ai ajuns la nivelul 35!", Criteria = "Reach level 35", ImagePath = "/images/badges/level35.jpeg" },
            new Badge { Id = 8, Title = "Level 40", Description = "Ai ajuns la nivelul 40!", Criteria = "Reach level 40", ImagePath = "/images/badges/level40.jpeg" },
            new Badge { Id = 9, Title = "Level 45", Description = "Ai ajuns la nivelul 45!", Criteria = "Reach level 45", ImagePath = "/images/badges/level45.jpeg" }
        );

        }
    }
}
