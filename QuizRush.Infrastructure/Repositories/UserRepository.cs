using QuizRush.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace QuizRush.Infrastructure.Repositories;

public class UserRepository
{
    private readonly QuizRushDbContext _context;
    public UserRepository(QuizRushDbContext context) => _context = context;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}