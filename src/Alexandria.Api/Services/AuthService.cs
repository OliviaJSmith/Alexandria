using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public class AuthService(
    IGoogleTokenValidator googleTokenValidator,
    IUserRepository users,
    IUsernameGenerator usernameGenerator,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    public const string TermsNotAcceptedMessage = "You must accept the beta terms and conditions to sign in.";
    public const string InvalidGoogleTokenMessage = "The Google sign-in could not be verified.";

    public async Task<ServiceResult<AuthResponse>> SignInWithGoogleAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.AcceptedTerms)
        {
            return ServiceResult<AuthResponse>.Failure(ServiceErrorType.Validation, TermsNotAcceptedMessage);
        }

        var googleUser = await googleTokenValidator.ValidateAccessTokenAsync(request.AccessToken, cancellationToken);
        if (googleUser is null)
        {
            return ServiceResult<AuthResponse>.Failure(ServiceErrorType.Unauthorized, InvalidGoogleTokenMessage);
        }

        var now = timeProvider.GetUtcNow();
        var user = await users.GetByGoogleSubjectIdAsync(googleUser.SubjectId, cancellationToken);
        if (user is null)
        {
            user = await CreateUserAsync(googleUser, now, cancellationToken);
            logger.LogInformation("Created user {UserId} on first Google sign-in", user.Id);
        }
        else
        {
            ApplyProfileChanges(user, googleUser, now);
        }

        user.TermsAcceptedAt = now;
        await users.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.CreateToken(user);
        return ServiceResult<AuthResponse>.Success(new AuthResponse(token.Token, token.ExpiresAt, UserDto.FromUser(user)));
    }

    private async Task<User> CreateUserAsync(GoogleUser googleUser, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var userName = await usernameGenerator.GenerateUniqueAsync(cancellationToken);
        var user = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = googleUser.SubjectId,
            Email = googleUser.Email,
            DisplayName = googleUser.Name,
            UserName = userName,
            NormalizedUserName = UsernameGenerator.Normalize(userName),
            ProfilePictureUrl = googleUser.PictureUrl,
            CreatedAt = now,
            UpdatedAt = now,
        };
        users.Add(user);
        return user;
    }

    private static void ApplyProfileChanges(User user, GoogleUser googleUser, DateTimeOffset now)
    {
        // Keep the profile in sync with Google; the username is Alexandria's own and never changes here.
        user.Email = googleUser.Email;
        user.DisplayName = googleUser.Name;
        user.ProfilePictureUrl = googleUser.PictureUrl;
        user.UpdatedAt = now;
    }
}
