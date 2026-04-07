using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.Models;

namespace ProjectManagementAPI.DataBaseContext
{
    public class ContextDb : DbContext
    {
        public ContextDb(DbContextOptions<ContextDb> options)
            : base(options)
        {
            Database.SetCommandTimeout(120);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<BacklogItem> BacklogItems { get; set; }
        public DbSet<SubTask> SubTasks { get; set; }
        public DbSet<Blocker> Blockers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<DailyUserTask> DailyUserTasks { get; set; }
        public DbSet<SprintVelocity> SprintVelocities { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RetrospectiveItem> RetrospectiveItems { get; set; }
        public DbSet<RetrospectiveVote> RetrospectiveVotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ProjectMember>()
                .HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            modelBuilder.Entity<DailyUserTask>()
                .HasIndex(d => new { d.UserId, d.Date })
                .IsUnique();

            modelBuilder.Entity<SprintVelocity>()
                .HasIndex(sv => sv.SprintId)
                .IsUnique();

            modelBuilder.Entity<Sprint>()
                .ToTable(t => t.HasCheckConstraint("CK_Sprint_Dates", "[StartDate] <= [EndDate]"));

            modelBuilder.Entity<Blocker>()
                .ToTable(t => t.HasCheckConstraint("CK_Blocker_Entity",
                    "[BacklogItemId] IS NOT NULL OR [SubTaskId] IS NOT NULL"));

            modelBuilder.Entity<Attachment>()
                .ToTable(t => t.HasCheckConstraint("CK_Attachment_Entity",
                    "[BacklogItemId] IS NOT NULL OR [SubTaskId] IS NOT NULL"));

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BacklogItem>()
                .HasOne(b => b.CreatedBy)
                .WithMany(u => u.CreatedBacklogItems)
                .HasForeignKey(b => b.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Blocker>()
                .HasOne(b => b.ReportedBy)
                .WithMany(u => u.ReportedBlockers)
                .HasForeignKey(b => b.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RetrospectiveVote>()
            .HasIndex(rv => new { rv.RetrospectiveItemId, rv.UserId })
            .IsUnique();

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            modelBuilder.Entity<RetrospectiveItem>()
                .HasIndex(ri => new { ri.SprintId, ri.Category });

            modelBuilder.Entity<BacklogItem>()
                .HasIndex(bi => new { bi.AssigneeId, bi.Status, bi.SprintId });

            modelBuilder.Entity<SubTask>()
                .HasIndex(st => new { st.AssigneeId, st.Status, st.BacklogItemId });

            modelBuilder.Entity<RetrospectiveVote>()
                .HasOne(rv => rv.User)
                .WithMany() 
                .HasForeignKey(rv => rv.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RetrospectiveVote>()
                .HasOne(rv => rv.RetrospectiveItem)
                .WithMany(ri => ri.Votes)
                .HasForeignKey(rv => rv.RetrospectiveItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blocker>()
                .HasOne(b => b.ReportedBy)
                .WithMany(u => u.ReportedBlockers)
                .HasForeignKey(b => b.ReportedById)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}