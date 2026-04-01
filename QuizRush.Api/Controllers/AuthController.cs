using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizRush.Core.Entities;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuizRush.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly QuizRushDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(QuizRushDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        string token = GenerateJwtToken(user);

        return Ok(new AuthResponseViewModel
        {
            Token = token,
            Username = user.Username,
            Email = user.Email
        });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:DurationInMinutes"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
