using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the request that creates an email
/// </summary>
public class CreateEmailRequest
{
    [JsonProperty("Detail")]
    public EmailDetail Detail { get; set; } = new();
}

/// <summary>
/// Represents the details of a created email
/// </summary>
public class EmailDetail
{
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("HasWebpageVersion")]
    public string HasWebpageVersion { get; set; } = "1";

    [JsonProperty("HasPermissionReminderMessage")]
    public string HasPermissionReminderMessage { get; set; } = "1";

    [JsonProperty("PermissionReminderMessage")]
    public string PermissionReminderMessage { get; set; } = string.Empty;

    [JsonProperty("Version")]
    public string Version { get; set; } = string.Empty;
}
