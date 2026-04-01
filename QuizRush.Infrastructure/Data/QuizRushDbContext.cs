using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;

namespace QuizRush.Infrastructure;

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

        // Unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Unique username
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // User creates quizzes
        modelBuilder.Entity<Quiz>()
            .HasOne(q => q.Creator)
            .WithMany(u => u.Quizzes)
            .HasForeignKey(q => q.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quiz has questions
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Question has answers
        modelBuilder.Entity<Answer>()
            .HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // GameSession belongs to quiz
        modelBuilder.Entity<GameSession>()
            .HasOne(gs => gs.Quiz)
            .WithMany()
            .HasForeignKey(gs => gs.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        // GameSession host (user)
        modelBuilder.Entity<GameSession>()
            .HasOne(gs => gs.HostUser)
            .WithMany()
            .HasForeignKey(gs => gs.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Player belongs to session
        modelBuilder.Entity<Player>()
            .HasOne(p => p.GameSession)
            .WithMany(gs => gs.Players)
            .HasForeignKey(p => p.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlayerAnswer belongs to session
        modelBuilder.Entity<PlayerAnswer>()
            .HasOne(pa => pa.GameSession)
            .WithMany(gs => gs.PlayerAnswers)
            .HasForeignKey(pa => pa.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlayerAnswer belongs to player
        modelBuilder.Entity<PlayerAnswer>()
            .HasOne(pa => pa.Player)
            .WithMany()
            .HasForeignKey(pa => pa.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        // PlayerAnswer belongs to question
        modelBuilder.Entity<PlayerAnswer>()
            .HasOne(pa => pa.Question)
            .WithMany()
            .HasForeignKey(pa => pa.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // GamblingAction belongs to player
        modelBuilder.Entity<GamblingAction>()
            .HasOne(ga => ga.Player)
            .WithMany(p => p.GamblingActions)
            .HasForeignKey(ga => ga.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        // GamblingAction belongs to session
        modelBuilder.Entity<GamblingAction>()
            .HasOne(ga => ga.GameSession)
            .WithMany(gs => gs.GamblingActions)
            .HasForeignKey(ga => ga.GameSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // GamblingAction belongs to question
        modelBuilder.Entity<GamblingAction>()
            .HasOne(ga => ga.Question)
            .WithMany()
            .HasForeignKey(ga => ga.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}