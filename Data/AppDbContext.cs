using Microsoft.EntityFrameworkCore;
using Study.Models;

namespace Study.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Question> Questions => Set<Question>();
        public DbSet<PracticeQuestion> PracticeQuestions => Set<PracticeQuestion>();
        public DbSet<KnowledgeStat> KnowledgeStats => Set<KnowledgeStat>();
        public DbSet<Paper> Papers => Set<Paper>();
        public DbSet<User> Users => Set<User>();
        public DbSet<InvitationCode> InvitationCodes => Set<InvitationCode>();
        public DbSet<AdviceHistory> AdviceHistories => Set<AdviceHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<InvitationCode>().HasIndex(i => i.Code).IsUnique();

            modelBuilder.Entity<KnowledgeStat>()
                .HasIndex(k => new { k.UserId, k.KnowledgePoint })
                .IsUnique(false);

            modelBuilder.Entity<KnowledgeStat>()
                .HasKey(k => new { k.UserId, k.Grade, k.Subject, k.KnowledgePoint });

            modelBuilder.Entity<AdviceHistory>()
                .HasIndex(h => new { h.UserId, h.Grade, h.Subject, h.Textbook, h.RequestHash });
        }
    }
}
