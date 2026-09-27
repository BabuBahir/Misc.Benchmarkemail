using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.Benchmarkemail;

public class BenchmarkEmailSettings : ISettings
{
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the BenchmarkEmail API token sent in the AuthToken header
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the contact list the newsletter subscribers are added to
    /// and the emails of the new blog posts are intended for
    /// </summary>
    public string ListId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact lists of the account, serialized as JSON.
    /// The cache is filled by the "Load lists" button of the configuration page,
    /// so that the list dropdowns are available without an API request on every page load.
    /// </summary>
    public string ContactListsJson { get; set; } = string.Empty;

    public bool EmailOnBlogPostEnabled { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the BenchmarkEmail email that is copied
    /// every time a blog post is created
    /// </summary>
    public string EmailTemplateId { get; set; } = BenchmarkEmailDefaults.DefaultEmailTemplateId;

    public bool LogRequests { get; set; }

    /// <summary>
    /// Gets or sets the delay in minutes before a newly created blog post email is scheduled to send
    /// </summary>
    public int EmailScheduleDelayMinutes { get; set; } = 5;
}