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
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            long userId = GetUserId();

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        /// <summary>Updates the username and email of the currently authenticated user.</summary>
        /// <response code="204">Profile updated successfully.</response>
        /// <response code="400">Username or email already taken.</response>
        /// <response code="401">Not authenticated.</response>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            long userId = GetUserId();

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
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            long userId = GetUserId();

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

        private long GetUserId()
        {
            return long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }
}
