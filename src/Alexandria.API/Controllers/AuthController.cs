using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    AlexandriaDbContext context,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthController> logger
) : ControllerBase
{
    /// <summary>
    /// Exchanges a Google access token for an Alexandria JWT token.
    /// Creates a new user if one doesn't exist.
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.AccessToken))
        {
            return BadRequest("Access token is required");
        }

        // Verify the Google token and get user info
        var googleUser = await VerifyGoogleTokenAsync(request.AccessToken);
        if (googleUser is null)
        {
            return Unauthorized("Invalid Google access token");
        }

        // Find or create user
        var user = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleUser.Id);
        
        if (user is null)
        {
            // Create new user
            user = new User
            {
                GoogleId = googleUser.Id,
                Email = googleUser.Email,
                Name = googleUser.Name,
                ProfilePictureUrl = googleUser.Picture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Created new user {UserId} for Google account {Email}", user.Id, user.Email);
        }
        else
        {
            // Update user info if changed
            var updated = false;
            if (user.Name != googleUser.Name)
            {
                user.Name = googleUser.Name;
                updated = true;
            }
            if (user.ProfilePictureUrl != googleUser.Picture)
            {
                user.ProfilePictureUrl = googleUser.Picture;
                updated = true;
            }
            if (updated)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        });
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    private async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string accessToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}"
            );

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google token verification failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return userInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying Google token");
            return null;
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
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

    private class GoogleUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }
}

public class GoogleLoginRequest
{
    public string AccessToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
