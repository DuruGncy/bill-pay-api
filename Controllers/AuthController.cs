using Asp.Versioning;
using global::MobileProviderBillPaymentSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MobileProviderBillPaymentSystem.Controllers;

[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _config;

    public AuthController(IUserService userService, IConfiguration config)
    {
        _userService = userService;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginDto dto)
    {
        var success = await _userService.RegisterUser(dto.Username, dto.Password);

        if (!success)
            return BadRequest("User already exists.");

        return Ok("User registered.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userService.Authenticate(dto.Username, dto.Password);

        if (user == null)
            return Unauthorized("Invalid credentials.");

        var token = GenerateJwt(user.Username);

        return Ok(new { token });
    }

    private string GenerateJwt(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _config["JwtIssuer"],
            audience: _config["JwtAudience"],
            claims: new[] { new Claim(ClaimTypes.Name, username) },
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public class LoginDto
{
    public string Username { get; set; }
    public string Password { get; set; }
}

