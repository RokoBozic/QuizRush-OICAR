using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using System.Security.Claims;

namespace QuizRush.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Returns the profile of the currently authenticated user.</summary>
        /// <response code="200">Profile returned.</response>
        /// <response code="401">Not authenticated.</response>
        [ProducesResponseType(typeof(UserProfileViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileViewModel>> GetProfile()
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        /// <summary>Player statistics and completed game history.</summary>
        [ProducesResponseType(typeof(PlayerStatsViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("stats")]
        public async Task<ActionResult<PlayerStatsViewModel>> GetPlayerStats()
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            var stats = await _userService.GetPlayerStatsAsync(userId);
            return Ok(stats);
        }

        /// <summary>Updates the username and email of the currently authenticated user.</summary>
        /// <response code="204">Profile updated successfully.</response>
        /// <response code="400">Username or email already taken.</response>
        /// <response code="401">Not authenticated.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            try
            {
                await _userService.UpdateProfileAsync(userId, model);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Changes the password of the currently authenticated user.</summary>
        /// <response code="204">Password changed successfully.</response>
        /// <response code="400">Current password is incorrect.</response>
        /// <response code="401">Not authenticated.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!TryGetUserId(out long userId))
                return Unauthorized();

            try
            {
                await _userService.ChangePasswordAsync(userId, model);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool TryGetUserId(out long userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out userId);
        }
    }
}
