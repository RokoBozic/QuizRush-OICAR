using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;

namespace QuizRush.Infrastructure.Services
{
    public class QuizService : IQuizService
    {
        private readonly QuizRushDbContext _context;

        public QuizService(QuizRushDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Quiz>> GetAllForCreatorAsync(long creatorId)
        {
            return await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.CreatorId == creatorId)
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<Quiz?> GetByIdForCreatorAsync(long id, long creatorId)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == id && q.CreatorId == creatorId);
        }

        public async Task<Quiz> CreateAsync(QuizViewModel model, long creatorId)
        {
            if (model.Questions == null || model.Questions.Count == 0)
                throw new ArgumentException("At least one question is required.");

            foreach (var question in model.Questions)
            {
                if (question.Answers == null || question.Answers.Count == 0)
                    throw new ArgumentException($"Question '{question.Text}' must have at least one answer.");

                if (!question.Answers.Any(a => a.IsCorrect))
                    throw new ArgumentException($"Question '{question.Text}' must have at least one correct answer.");
            }

            string title = model.Title.Trim();
            if (string.IsNullOrEmpty(title))
                throw new ArgumentException("Title is required.");

            if (await _context.Quizzes.AnyAsync(q => q.CreatorId == creatorId && q.Title == title))
                throw new ArgumentException("You already have a quiz with this title. Choose a different title.");

            var quiz = new Quiz
            {
                Title = title,
                Description = model.Description?.Trim() ?? string.Empty,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow,
                Questions = model.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    PointsValue = q.PointsValue,
                    TimeLimitSeconds = q.TimeLimitSeconds,
                    Answers = q.Answers.Select(a => new Answer
                    {
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return quiz;
        }

        public async Task UpdateAsync(long id, QuizViewModel model, long actingUserId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with id {id} not found.");

            if (quiz.CreatorId != actingUserId)
                throw new UnauthorizedAccessException("You can only edit quizzes you created.");

            if (model.Questions == null || model.Questions.Count == 0)
                throw new ArgumentException("At least one question is required.");

            foreach (var question in model.Questions)
            {
                if (question.Answers == null || question.Answers.Count == 0)
                    throw new ArgumentException($"Question '{question.Text}' must have at least one answer.");

                if (!question.Answers.Any(a => a.IsCorrect))
                    throw new ArgumentException($"Question '{question.Text}' must have at least one correct answer.");
            }

            string title = model.Title.Trim();
            if (string.IsNullOrEmpty(title))
                throw new ArgumentException("Title is required.");

            if (await _context.Quizzes.AnyAsync(q => q.CreatorId == actingUserId && q.Id != id && q.Title == title))
                throw new ArgumentException("You already have a quiz with this title. Choose a different title.");

            quiz.Title = title;
            quiz.Description = model.Description?.Trim() ?? string.Empty;

            foreach (var question in quiz.Questions.ToList())
            {
                _context.Answers.RemoveRange(question.Answers);
                _context.Questions.Remove(question);
            }

            quiz.Questions = model.Questions.Select(q => new Question
            {
                Text = q.Text,
                PointsValue = q.PointsValue,
                TimeLimitSeconds = q.TimeLimitSeconds,
                Answers = q.Answers.Select(a => new Answer
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList();

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id, long actingUserId)
        {
            var quiz = await _context.Quizzes.FindAsync(id);

            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with id {id} not found.");

            if (quiz.CreatorId != actingUserId)
                throw new UnauthorizedAccessException("You can only delete quizzes you created.");

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
        }
    }
}
