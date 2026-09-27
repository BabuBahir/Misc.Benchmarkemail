using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the request that updates an existing email.
/// A field that is not passed is left as it is on the BenchmarkEmail side
/// </summary>
public class UpdateEmailRequest
{
    [JsonProperty("Detail")]
    public EmailContentDetail Detail { get; set; } = new();
}

/// <summary>
/// Represents the content fields of an email that is updated.
/// The null fields are left out of the request, so that they keep their current value
/// </summary>
public class EmailContentDetail
{
    [JsonProperty("TemplateCode", NullValueHandling = NullValueHandling.Ignore)]
    public string TemplateCode { get; set; }

    [JsonProperty("TemplateContent", NullValueHandling = NullValueHandling.Ignore)]
    public string TemplateContent { get; set; }

    [JsonProperty("Subject", NullValueHandling = NullValueHandling.Ignore)]
    public string Subject { get; set; }

    [JsonProperty("ContactLists", NullValueHandling = NullValueHandling.Ignore)]
    public List<ContactListItem> ContactLists { get; set; }

    [JsonProperty("ExcludeContactLists", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ExcludeContactLists { get; set; } = new();
}

/// <summary>
/// Represents a contact list item in the email update request
/// </summary>
public class ContactListItem
{
    [JsonProperty("ID")]
    public string ID { get; set; }

    [JsonProperty("selected")]
    public bool Selected { get; set; } = true;
}
