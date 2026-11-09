namespace Alexandria.Api.Options;

/// <summary>Azure Vision (Image Analysis 4.0) for reading text in book photos. Optional: scanning is off when unset.</summary>
public class AzureVisionOptions
{
    public const string SectionName = "AzureVision";

    public string? Endpoint { get; set; }

    /// <summary>Comes from Key Vault in Azure and user-secrets locally; never committed.</summary>
    public string? ApiKey { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>Azure OpenAI for turning scanned text into book titles and authors. Optional: scanning is off when unset.</summary>
public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string? Endpoint { get; set; }

    /// <summary>Model deployment name, e.g. gpt-5-mini.</summary>
    public string? Deployment { get; set; }

    /// <summary>Comes from Key Vault in Azure and user-secrets locally; never committed.</summary>
    public string? ApiKey { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Deployment) && !string.IsNullOrWhiteSpace(ApiKey);
}
