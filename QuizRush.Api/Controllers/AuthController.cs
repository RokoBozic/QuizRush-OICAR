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

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            return BadRequest("Email already in use.");

        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            return BadRequest("Username already taken.");

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
        await _context.SaveChangesAsync();

        return Ok("Registration successful.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
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
