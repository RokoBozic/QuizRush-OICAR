using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using System.Security.Claims;

namespace QuizRush.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameSessionController : ControllerBase
    {
        private readonly IGameSessionService _gameSessionService;

        public GameSessionController(IGameSessionService gameSessionService)
        {
            _gameSessionService = gameSessionService;
        }

        /// <summary>Creates a new game session for a quiz and generates a PIN code. Requires authentication.</summary>
        /// <response code="201">Session created, PIN code returned.</response>
        /// <response code="404">Quiz not found.</response>
        /// <response code="401">Not authenticated.</response>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateSession(CreateSessionViewModel model)
        {
            long hostUserId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var session = await _gameSessionService.CreateSessionAsync(model.QuizId, hostUserId);
                return CreatedAtAction(nameof(GetSessionByCode), new { code = session.Code }, session);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>Returns a game session by its PIN code.</summary>
        /// <response code="200">Session found and returned.</response>
        /// <response code="404">Session not found.</response>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetSessionByCode(string code)
        {
            var session = await _gameSessionService.GetSessionByCodeAsync(code);
            if (session == null)
                return NotFound();

            return Ok(session);
        }

        /// <summary>Returns the final leaderboard/results for a completed game session.</summary>
        /// <response code="200">Results returned, ranked by score.</response>
        /// <response code="404">Session not found.</response>
        [HttpGet("{id:long}/results")]
        public async Task<IActionResult> GetResults(long id)
        {
            try
            {
                var results = await _gameSessionService.GetResultsAsync(id);
                return Ok(results);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
