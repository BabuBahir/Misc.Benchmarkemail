using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the request that removes a contact from a contact list (unsubscribe)
/// </summary>
public class UnsubscribeContactRequest
{
    [JsonProperty("ListID")]
    public string ListID { get; set; }

    [JsonProperty("ContactID")]
    public string ContactID { get; set; }
}