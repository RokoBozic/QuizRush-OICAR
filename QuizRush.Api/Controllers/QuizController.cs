using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizRush.Core.Entities;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using System.Security.Claims;

namespace QuizRush.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        /// <summary>Returns quizzes created by the current user.</summary>
        [ProducesResponseType(typeof(IEnumerable<QuizResponseViewModel>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizResponseViewModel>>> GetMine()
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            var quizzes = await _quizService.GetAllForCreatorAsync(userId);
            return Ok(quizzes.Select(MapQuiz));
        }

        /// <summary>Returns a single quiz by ID if it belongs to the current user.</summary>
        [ProducesResponseType(typeof(QuizResponseViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizResponseViewModel>> GetById(long id)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            var quiz = await _quizService.GetByIdForCreatorAsync(id, userId);
            if (quiz == null)
                return NotFound();

            return Ok(MapQuiz(quiz));
        }

        [ProducesResponseType(typeof(QuizResponseViewModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost]
        public async Task<ActionResult<QuizResponseViewModel>> Create(QuizViewModel model)
        {
            if (!TryGetUserId(out long creatorId))
                return Unauthorized();

            try
            {
                var quiz = await _quizService.CreateAsync(model, creatorId);
                return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, MapQuiz(quiz));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, QuizViewModel model)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            try
            {
                await _quizService.UpdateAsync(id, model, userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            try
            {
                await _quizService.DeleteAsync(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
        }

        private bool TryGetUserId(out long userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out userId);
        }

        private static QuizResponseViewModel MapQuiz(Quiz quiz)
        {
            return new QuizResponseViewModel
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                CreatorId = quiz.CreatorId,
                CreatedAt = quiz.CreatedAt,
                Questions = quiz.Questions.Select(MapQuestion).ToList()
            };
        }

        private static QuestionResponseViewModel MapQuestion(Question question)
        {
            return new QuestionResponseViewModel
            {
                Id = question.Id,
                Text = question.Text,
                PointsValue = question.PointsValue,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Answers = question.Answers.Select(MapAnswer).ToList()
            };
        }

        private static AnswerResponseViewModel MapAnswer(Answer answer)
        {
            return new AnswerResponseViewModel
            {
                Id = answer.Id,
                Text = answer.Text,
                IsCorrect = answer.IsCorrect
            };
        }
    }
}
