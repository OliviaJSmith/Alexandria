using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alexandria.Api.Options;
using Microsoft.Extensions.Options;

namespace Alexandria.Api.Services;

/// <summary>
/// Validates Google access tokens with Google's tokeninfo endpoint (audience, expiry, verified email),
/// then reads the profile from the OpenID Connect userinfo endpoint.
/// </summary>
public class GoogleTokenValidator(
    HttpClient httpClient,
    IOptions<GoogleOptions> options,
    ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    public const string TokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo";
    public const string UserInfoUrl = "https://openidconnect.googleapis.com/v1/userinfo";

    public async Task<GoogleUser?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenInfo = await GetTokenInfoAsync(accessToken, cancellationToken);
            if (tokenInfo is null || !IsAcceptable(tokenInfo))
            {
                return null;
            }

            var userInfo = await GetUserInfoAsync(accessToken, cancellationToken);
            if (userInfo is null || userInfo.Sub != tokenInfo.Sub)
            {
                logger.LogWarning("Google userinfo did not match the validated token");
                return null;
            }

            var name = string.IsNullOrWhiteSpace(userInfo.Name) ? tokenInfo.Email! : userInfo.Name;
            return new GoogleUser(tokenInfo.Sub!, tokenInfo.Email!, name, userInfo.Picture);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Google token validation failed");
            return null;
        }
    }

    private async Task<TokenInfo?> GetTokenInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        // POST keeps the token out of URLs (and therefore out of request logs).
        using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("access_token", accessToken)]);
        using var response = await httpClient.PostAsync(TokenInfoUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("Google rejected an access token with status {StatusCode}", (int)response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TokenInfo>(cancellationToken);
    }

    private async Task<UserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Google userinfo request failed with status {StatusCode}", (int)response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
    }

    private bool IsAcceptable(TokenInfo tokenInfo)
    {
        if (string.IsNullOrEmpty(tokenInfo.Sub) || string.IsNullOrEmpty(tokenInfo.Email))
        {
            logger.LogWarning("Google token info is missing the subject or email");
            return false;
        }

        if (!options.Value.ClientIds.Contains(tokenInfo.Aud, StringComparer.Ordinal))
        {
            logger.LogWarning("Google access token was issued to an unexpected client {Audience}", tokenInfo.Aud);
            return false;
        }

        if (!IsTrue(tokenInfo.EmailVerified))
        {
            logger.LogWarning("Google account email is not verified");
            return false;
        }

        return true;
    }

    // Google documents email_verified as a string ("true") on tokeninfo, but accept a JSON boolean too.
    private static bool IsTrue(JsonElement? value) => value?.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.String => string.Equals(value.Value.GetString(), "true", StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private sealed class TokenInfo
    {
        [JsonPropertyName("aud")]
        public string? Aud { get; set; }

        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public JsonElement? EmailVerified { get; set; }
    }

    private sealed class UserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}
