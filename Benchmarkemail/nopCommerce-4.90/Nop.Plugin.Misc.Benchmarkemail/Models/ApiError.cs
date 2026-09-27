using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents a single BenchmarkEmail API error
/// </summary>
public class ApiError
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
}