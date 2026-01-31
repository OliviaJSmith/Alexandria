using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    AlexandriaDbContext context,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Exchanges a Google access token for an API JWT token.
    /// Creates a new user if one doesn't exist.
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] GoogleLoginDto request)
    {
        try
        {
            // Validate the Google token by fetching user info
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.AccessToken);
            
            var response = await httpClient.GetAsync("https://www.googleapis.com/userinfo/v2/me");
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Invalid Google token provided");
                return Unauthorized(new { error = "Invalid Google token" });
            }

            var content = await response.Content.ReadAsStringAsync();
            var googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (googleUser is null || string.IsNullOrEmpty(googleUser.Id))
            {
                return Unauthorized(new { error = "Failed to get user info from Google" });
            }

            // Find or create user
            var user = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleUser.Id);
            
            if (user is null)
            {
                // Create new user
                user = new User
                {
                    GoogleId = googleUser.Id,
                    Email = googleUser.Email ?? "",
                    Name = googleUser.Name ?? "",
                    ProfilePictureUrl = googleUser.Picture,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                context.Users.Add(user);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Created new user with ID {UserId} from Google login", user.Id);
            }
            else
            {
                // Update user info from Google
                user.Email = googleUser.Email ?? user.Email;
                user.Name = googleUser.Name ?? user.Name;
                user.ProfilePictureUrl = googleUser.Picture ?? user.ProfilePictureUrl;
                user.UpdatedAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync();
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    UserName = user.UserName,
                    ProfilePictureUrl = user.ProfilePictureUrl
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Google login");
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? "YourSecretKeyForAuthenticationOfAlexandria2026";
        var issuer = configuration["Jwt:Issuer"] ?? "Alexandria";
        var audience = configuration["Jwt:Audience"] ?? "AlexandriaUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("google_id", user.GoogleId)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class GoogleLoginDto
{
    public string AccessToken { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class GoogleUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
}
