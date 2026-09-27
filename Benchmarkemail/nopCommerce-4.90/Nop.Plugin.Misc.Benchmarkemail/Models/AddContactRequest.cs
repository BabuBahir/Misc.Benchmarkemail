using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the request that adds a contact to a contact list
/// </summary>
public class AddContactRequest
{
    [JsonProperty("Data")]
    public AddContactData Data { get; set; } = new();
}

/// <summary>
/// Represents the data of a contact added to a contact list
/// </summary>
public class AddContactData
{
    [JsonProperty("Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the contact has given the permission to receive the emails.
    /// The BenchmarkEmail API rejects the contact when it is not set to 1.
    /// </summary>
    [JsonProperty("EmailPerm")]
    public int EmailPermission { get; set; } = 1;

    [JsonProperty("FirstName", NullValueHandling = NullValueHandling.Ignore)]
    public string FirstName { get; set; }

    [JsonProperty("LastName", NullValueHandling = NullValueHandling.Ignore)]
    public string LastName { get; set; }
}