using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents a BenchmarkEmail contact list
/// </summary>
public class ListInfo
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a contact list returned by the BenchmarkEmail API
/// </summary>
public class ListResponse
{
    [JsonProperty("ID")]
    public string Id { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }
}