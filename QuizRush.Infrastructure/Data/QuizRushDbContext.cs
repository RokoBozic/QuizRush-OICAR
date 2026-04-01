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
        public DbSet<GameSession> GameSessions { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerAnswer> PlayerAnswers { get; set; }
        public DbSet<GamblingAction> GamblingActions { get; set; }

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

            // GameSession
            modelBuilder.Entity<GameSession>()
                .HasOne(gs => gs.Quiz)
                .WithMany()
                .HasForeignKey(gs => gs.QuizId);

            modelBuilder.Entity<GameSession>()
                .HasOne(gs => gs.HostUser)
                .WithMany()
                .HasForeignKey(gs => gs.HostUserId);

            // Player
            modelBuilder.Entity<Player>()
                .HasOne(p => p.GameSession)
                .WithMany(gs => gs.Players)
                .HasForeignKey(p => p.GameSessionId);

            // PlayerAnswer
            modelBuilder.Entity<PlayerAnswer>()
                .HasOne(pa => pa.GameSession)
                .WithMany(gs => gs.PlayerAnswers)
                .HasForeignKey(pa => pa.GameSessionId);

            modelBuilder.Entity<PlayerAnswer>()
                .HasOne(pa => pa.Player)
                .WithMany()
                .HasForeignKey(pa => pa.PlayerId);

            // GamblingAction
            modelBuilder.Entity<GamblingAction>()
                .HasOne(ga => ga.Player)
                .WithMany()
                .HasForeignKey(ga => ga.PlayerId);

            modelBuilder.Entity<GamblingAction>()
                .HasOne(ga => ga.GameSession)
                .WithMany()
                .HasForeignKey(ga => ga.GameSessionId);

            modelBuilder.Entity<GamblingAction>()
                .HasOne(ga => ga.Question)
                .WithMany()
                .HasForeignKey(ga => ga.QuestionId);
        }
    }
}