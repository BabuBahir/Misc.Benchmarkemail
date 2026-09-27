using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.Benchmarkemail;

public class BenchmarkEmailPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IScheduleTaskService _scheduleTaskService;
    protected readonly ISettingService _settingService;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public BenchmarkEmailPlugin(IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _scheduleTaskService = scheduleTaskService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}{BenchmarkEmailDefaults.ConfigurationPageUrl}";
    }

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new BenchmarkEmailSettings());
        await AddOrUpdateLocaleResourcesAsync();
        await InstallBlogPostEmailTaskAsync();
        await base.InstallAsync();
    }

    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        await AddOrUpdateLocaleResourcesAsync();
        //an installation that predates the task would never create the blog post emails,
        //and one that predates a shorter period announces them too late
        await InstallBlogPostEmailTaskAsync();
        await base.UpdateAsync(currentVersion, targetVersion);
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<BenchmarkEmailSettings>();

        //schedule task
        var task = await _scheduleTaskService.GetTaskByTypeAsync(BenchmarkEmailDefaults.BlogPostEmailTask);
        if (task != null)
            await _scheduleTaskService.DeleteTaskAsync(task);

        //pending blog post flags
        await _genericAttributeService.DeleteAttributesAsync<BlogPost>(BenchmarkEmailDefaults.PendingBlogPostEmailAttributeName);

        //stored BenchmarkEmail contact identifiers
        await _genericAttributeService.DeleteAttributesAsync<NewsLetterSubscription>(BenchmarkEmailDefaults.ContactIdAttributeName);

        await DeleteLocaleResourcesAsync();
        await base.UninstallAsync();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Installs the schedule task that creates the emails of the new blog posts.
    /// The save of the search engine name of a new blog post runs the task right away, the period is the
    /// safety net for the missed runs.
    /// The period of an already installed task is refreshed as well, otherwise an installation that
    /// predates a shorter period would keep announcing the blog posts with the old delay
    /// </summary>
    protected virtual async Task InstallBlogPostEmailTaskAsync()
    {
        var seconds = BenchmarkEmailDefaults.DefaultBlogPostEmailPeriod * 60;

        var task = await _scheduleTaskService.GetTaskByTypeAsync(BenchmarkEmailDefaults.BlogPostEmailTask);

        if (task != null)
        {
            if (task.Seconds == seconds)
                return;

            task.Seconds = seconds;

            await _scheduleTaskService.UpdateTaskAsync(task);

            return;
        }

        await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
        {
            Enabled = true,
            LastEnabledUtc = DateTime.UtcNow,
            Seconds = seconds,
            Name = BenchmarkEmailDefaults.BlogPostEmailTaskName,
            Type = BenchmarkEmailDefaults.BlogPostEmailTask
        });
    }

    /// <summary>
    /// Adds or updates the locale resources of the plugin
    /// </summary>
    protected virtual async Task AddOrUpdateLocaleResourcesAsync()
    {
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.Title", "BenchmarkEmail");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.Enabled", "Enabled");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.Enabled.Hint",
            "Enables the synchronization with BenchmarkEmail");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ApiToken", "API token");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ApiToken.Hint",
            "The API token sent in the AuthToken header, found in your BenchmarkEmail account (Integrate)");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ApiTokenRequired",
            "the API token is required");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ListId", "Contact list");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ListId.Hint",
            "The contact list the newsletter subscribers are added to and the emails of the new blog posts are intended for. " +
            "Use the Load lists button to fill the dropdown with the lists of your account");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.SelectList", "Please select a list");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ListNotFound", "{0} (not found)");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailOnBlogPostEnabled", "Create an email on blog post creation");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailOnBlogPostEnabled.Hint",
            "Copies an email of BenchmarkEmail every time a blog post is created. The name of the copy is the title of the blog post");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailTemplateId", "Email template");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailTemplateId.Hint",
            $"The identifier of the BenchmarkEmail email that is copied for every new blog post. " +
            $"The placeholder {BenchmarkEmailDefaults.BlogPostPlaceholder} of the copy is then replaced with the URL of the blog post. " +
            "The subject of the copy is built by the plugin, the subject of the template is not used");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailScheduleDelayMinutes", "Email schedule delay (minutes)");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailScheduleDelayMinutes.Hint",
            "The delay in minutes before a newly created blog post email is scheduled to send. Default is 5 minutes.");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LogRequests", "Log requests");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LogRequests.Hint",
            "Logs the API requests and their responses in Admin > System > Log");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadLists", "Load lists");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadLists.Hint",
            "Loads the contact lists of the account with the API token above, so that they can be selected in the dropdowns. " +
            "The API token is saved at the same time");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsSuccess",
            "The contact lists were loaded from BenchmarkEmail");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsCount",
            "{0} list(s) found");
        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsFailed",
            "The contact lists could not be loaded from BenchmarkEmail: {0}");

        //retired by the load lists button and by the single contact list dropdown
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.TestConnection");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.TestSuccess");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.TestLists");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.TestFailed");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.BlogPostListId");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.BlogPostListId.Hint");

        //retired AutoSync placeholders
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.AutoSyncEnabled");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.AutoSyncEnabled.Hint");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.AutoSyncIntervalHours");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.AutoSyncIntervalHours.Hint");

        //retired by the email template copy, which brings the settings of its source email along
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailSourceVersion");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailSourceVersion.Hint");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailHasWebpageVersion");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailHasWebpageVersion.Hint");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailHasPermissionReminderMessage");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.EmailHasPermissionReminderMessage.Hint");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.PermissionReminderMessage");
        await _localizationService.DeleteLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.PermissionReminderMessage.Hint");

        await _localizationService.AddOrUpdateLocaleResourceAsync("Nop.Plugin.Misc.Benchmarkemail.BlogPostEmailTask",
            "Create a BenchmarkEmail email for every new blog post");
    }

    /// <summary>
    /// Deletes the locale resources of the plugin
    /// </summary>
    protected virtual async Task DeleteLocaleResourcesAsync()
    {
        await _localizationService.DeleteLocaleResourcesAsync("Nop.Plugin.Misc.Benchmarkemail");
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.Benchmarkemail");
    }

    #endregion
}