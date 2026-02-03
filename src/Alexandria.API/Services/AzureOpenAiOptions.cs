using System.ComponentModel.DataAnnotations;

namespace Alexandria.API.Services;

/// <summary>
/// Configuration options for Azure Open AI service.
/// </summary>
public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// The Azure Open AI endpoint URL.
    /// </summary>
    [Required(ErrorMessage = "Azure Open AI Endpoint is required")]
    [Url(ErrorMessage = "Azure Open AI Endpoint must be a valid URL")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The Azure Open AI API key.
    /// </summary>
    [Required(ErrorMessage = "Azure Computer Vision ApiKey is required")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The deployment name for Azure OpenAI model (optional).
    /// If provided, OCR results will be enhanced with AI.
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the Azure Computer Vision service is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(ApiKey);

    /// <summary>
    /// Indicates whether AI enhancement is enabled (requires DeploymentName).
    /// </summary>
    public bool IsAiEnhancementEnabled => IsConfigured && !string.IsNullOrEmpty(DeploymentName);
}
