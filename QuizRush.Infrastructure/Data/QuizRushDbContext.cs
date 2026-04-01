using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;

namespace QuizRush.Infrastructure
{
    public class QuizRushDbContext : DbContext
    {
        public QuizRushDbContext(DbContextOptions<QuizRushDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Unique Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // One-to-Many for Quiz Creator
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Creator)
                .WithMany(u => u.Quizzes)
                .HasForeignKey(q => q.CreatorId);
        }
    }
}