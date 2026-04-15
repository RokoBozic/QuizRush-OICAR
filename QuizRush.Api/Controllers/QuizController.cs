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
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        /// <summary>Returns all quizzes including their questions and answers.</summary>
        /// <response code="200">List of quizzes returned.</response>
        [ProducesResponseType(typeof(IEnumerable<QuizResponseViewModel>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizResponseViewModel>>> GetAll()
        {
            var quizzes = await _quizService.GetAllAsync();
            return Ok(quizzes.Select(MapQuiz));
        }

        /// <summary>Returns a single quiz by ID.</summary>
        /// <response code="200">Quiz found and returned.</response>
        /// <response code="404">Quiz not found.</response>
        [ProducesResponseType(typeof(QuizResponseViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizResponseViewModel>> GetById(long id)
        {
            var quiz = await _quizService.GetByIdAsync(id);
            if (quiz == null)
                return NotFound();

            return Ok(MapQuiz(quiz));
        }

        /// <summary>Creates a new quiz. Requires authentication.</summary>
        /// <response code="201">Quiz created successfully.</response>
        /// <response code="400">Validation error (missing questions, no correct answer, etc).</response>
        /// <response code="401">Not authenticated.</response>
        [ProducesResponseType(typeof(QuizResponseViewModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<QuizResponseViewModel>> Create(QuizViewModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            long creatorId = long.Parse(userIdClaim);

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

        /// <summary>Updates an existing quiz. Requires authentication.</summary>
        /// <response code="204">Quiz updated successfully.</response>
        /// <response code="400">Validation error.</response>
        /// <response code="404">Quiz not found.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, QuizViewModel model)
        {
            try
            {
                await _quizService.UpdateAsync(id, model);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Deletes a quiz by ID. Requires authentication.</summary>
        /// <response code="204">Quiz deleted successfully.</response>
        /// <response code="404">Quiz not found.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _quizService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
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