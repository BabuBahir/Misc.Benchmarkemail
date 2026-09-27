using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the request that copies an existing email.
/// The name is sent as is, the copy endpoint does not wrap it into a detail object
/// </summary>
public class CopyEmailRequest
{
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;
}
