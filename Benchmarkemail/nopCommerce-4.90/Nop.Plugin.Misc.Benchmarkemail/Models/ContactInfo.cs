using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents a contact of a BenchmarkEmail contact list
/// </summary>
public class ContactInfo
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Represents a contact returned by the BenchmarkEmail API
/// </summary>
public class AddContactResponse
{
    [JsonProperty("ID")]
    public string Id { get; set; }
}