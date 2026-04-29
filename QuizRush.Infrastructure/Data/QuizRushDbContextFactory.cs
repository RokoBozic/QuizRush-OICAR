using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuizRush.Infrastructure;

public class QuizRushDbContextFactory : IDesignTimeDbContextFactory<QuizRushDbContext>
{
    public QuizRushDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuizRushDbContext>();

        // Use your SQL Server connection string here
        optionsBuilder.UseSqlServer("Server=(local);Database=QuizRushDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new QuizRushDbContext(optionsBuilder.Options);
    }
}