using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

public record ConfigurationModel : BaseNopModel
{
    /// <summary>
    /// Gets or sets the contact lists the list dropdowns are built from
    /// </summary>
    public IList<SelectListItem> AvailableLists { get; set; } = new List<SelectListItem>();

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.Enabled")]
    public bool Enabled { get; set; }

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.ApiToken")]
    public string ApiToken { get; set; } = string.Empty;

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.ListId")]
    public string ListId { get; set; } = string.Empty;

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.EmailOnBlogPostEnabled")]
    public bool EmailOnBlogPostEnabled { get; set; }

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.EmailTemplateId")]
    public string EmailTemplateId { get; set; } = BenchmarkEmailDefaults.DefaultEmailTemplateId;

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.LogRequests")]
    public bool LogRequests { get; set; }

    [NopResourceDisplayName("Nop.Plugin.Misc.Benchmarkemail.EmailScheduleDelayMinutes")]
    public int EmailScheduleDelayMinutes { get; set; } = 5;
}
