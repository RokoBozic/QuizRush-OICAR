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

        public async Task<IEnumerable<Quiz>> GetAllAsync()
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .ToListAsync(); 
        }

        public async Task<Quiz?> GetByIdAsync(long id)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);
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

            var quiz = new Quiz
            {
                Title = model.Title,
                Description = model.Description,
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

        public async Task UpdateAsync(long id, QuizViewModel model)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with id {id} not found.");

            if (model.Questions == null || model.Questions.Count == 0)
                throw new ArgumentException("At least one question is required.");

            foreach (var question in model.Questions)
            {
                if (question.Answers == null || question.Answers.Count == 0)
                    throw new ArgumentException($"Question '{question.Text}' must have at least one answer.");

                if (!question.Answers.Any(a => a.IsCorrect))
                    throw new ArgumentException($"Question '{question.Text}' must have at least one correct answer.");
            }

            quiz.Title = model.Title;
            quiz.Description = model.Description;

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

        public async Task DeleteAsync(long id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);

            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with id {id} not found.");

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
        }
    }
}