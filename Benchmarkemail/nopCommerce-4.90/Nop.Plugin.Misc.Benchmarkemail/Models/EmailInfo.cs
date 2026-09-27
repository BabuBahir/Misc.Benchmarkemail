using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents an email created in BenchmarkEmail
/// </summary>
public class EmailInfo
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the email as it was returned by the copy request
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject of the email as it was returned by the copy request.
    /// It is only informative, the plugin builds the subject of the email itself
    /// </summary>
    public string Subject { get; set; } = string.Empty;
}

/// <summary>
/// Represents an email returned by the BenchmarkEmail API
/// </summary>
public class EmailResponse
{
    [JsonProperty("ID")]
    public string Id { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the content of the copy, the placeholders of which are replaced afterwards
    /// </summary>
    [JsonProperty("PreviewContent")]
    public string PreviewContent { get; set; }

    [JsonProperty("TemplateCode")]
    public string TemplateCode { get; set; }

    [JsonProperty("TemplateContent")]
    public string TemplateContent { get; set; }

    [JsonProperty("Subject")]
    public string Subject { get; set; }

    /// <summary>
    /// Gets the content of the email, the copy endpoint returns it under the preview property,
    /// the documented email data model uses the template properties.
    /// The subject is not a source of the content, the plugin builds the subject of the email itself
    /// </summary>
    public string GetContent()
    {
        if (!string.IsNullOrEmpty(PreviewContent))
            return PreviewContent;

        if (!string.IsNullOrEmpty(TemplateCode))
            return TemplateCode;

        return TemplateContent ?? string.Empty;
    }
}
