using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Security;

namespace QuizRush.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly QuizRushDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(QuizRushDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>Registers a new user account.</summary>
    /// <response code="200">Registration successful.</response>
    /// <response code="400">Email or username already in use.</response>
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [HttpPost("register")]
    public async Task<ActionResult<string>> Register(RegisterViewModel model)
    {
        var duplicateMessage = await GetDuplicateUserMessageAsync(model.Email, model.Username);
        if (duplicateMessage is not null)
        {
            return BadRequest(duplicateMessage);
        }

        string salt = PasswordHashProvider.GenerateSalt();
        string hash = PasswordHashProvider.HashPassword(model.Password, salt);

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            duplicateMessage = await GetDuplicateUserMessageAsync(model.Email, model.Username);
            if (duplicateMessage is not null)
            {
                return BadRequest(duplicateMessage);
            }

            throw;
        }

        return Ok("Registration successful.");
    }

    private async Task<string?> GetDuplicateUserMessageAsync(string email, string username)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return "An account with this email already exists. Please log in.";
        }

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return "This username is already taken. Please log in or choose another username.";
        }

        return null;
    }

    /// <summary>Logs in with email and password, returns a JWT token.</summary>
    /// <response code="200">Login successful, JWT token returned.</response>
    /// <response code="401">Invalid credentials.</response>
    [ProducesResponseType(typeof(AuthResponseViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseViewModel>> Login(LoginViewModel model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
            return Unauthorized("Invalid credentials.");

        if (!PasswordHashProvider.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized("Invalid credentials.");

        string token = _jwtTokenService.GenerateToken(user);

        return Ok(new AuthResponseViewModel
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        });
    }


}
