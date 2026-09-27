using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Plugin.Misc.Benchmarkemail.Models;
using Nop.Services.Blogs;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Seo;

namespace Nop.Plugin.Misc.Benchmarkemail.Services;

/// <summary>
/// Represents the BenchmarkEmail synchronization manager
/// </summary>
public class BenchmarkEmailManager
{
    #region Fields

    protected readonly BenchmarkEmailHttpClient _httpClient;
    protected readonly BenchmarkEmailSettings _settings;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILanguageService _languageService;
    protected readonly ILogger _logger;
    protected readonly IRepository<GenericAttribute> _genericAttributeRepository;
    protected readonly IBlogService _blogService;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IWebHelper _webHelper;
    protected readonly LocalizationSettings _localizationSettings;
    protected readonly ICustomerService _customerService;

    #endregion

    #region Ctor

    public BenchmarkEmailManager(BenchmarkEmailHttpClient httpClient,
        BenchmarkEmailSettings settings,
        IGenericAttributeService genericAttributeService,
        ILanguageService languageService,
        ILogger logger,
        IRepository<GenericAttribute> genericAttributeRepository,
        IBlogService blogService,
        IUrlRecordService urlRecordService,
        IWebHelper webHelper,
        LocalizationSettings localizationSettings,
        ICustomerService customerService)
    {
        _httpClient = httpClient;
        _settings = settings;
        _genericAttributeService = genericAttributeService;
        _languageService = languageService;
        _logger = logger;
        _genericAttributeRepository = genericAttributeRepository;
        _blogService = blogService;
        _urlRecordService = urlRecordService;
        _webHelper = webHelper;
        _localizationSettings = localizationSettings;
        _customerService = customerService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets a value indicating whether the plugin is configured and enabled
    /// </summary>
    public static bool IsConfigured(BenchmarkEmailSettings settings)
    {
        return settings is { Enabled: true }
               && !string.IsNullOrWhiteSpace(settings.ApiToken)
               && !string.IsNullOrWhiteSpace(settings.ListId);
    }

    /// <summary>
    /// Gets a value indicating whether the creation of the emails for the new blog posts is enabled
    /// </summary>
    protected bool IsBlogPostEmailEnabled => _settings.Enabled && _settings.EmailOnBlogPostEnabled;

    /// <summary>
    /// Repeats the passed operation while it fails. Only for the requests that are safe to repeat,
    /// because the API creates a new email on every call and cannot be retried without duplicates.
    /// </summary>
    protected virtual async Task ExecuteWithRetryAsync(Func<Task> operation)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await operation();

                return;
            }
            catch when (attempt < BenchmarkEmailDefaults.MaxRequestAttempts)
            {
                await Task.Delay(BenchmarkEmailDefaults.RetryDelay);
            }
        }
    }

    /// <summary>
    /// Removes the pending flag of the passed blog post
    /// </summary>
    protected virtual async Task ClearPendingBlogPostEmailAsync(int blogPostId)
    {
        //an empty value deletes the attribute, see GenericAttributeService
        await _genericAttributeService.SaveAttributeAsync(new BlogPost { Id = blogPostId },
            BenchmarkEmailDefaults.PendingBlogPostEmailAttributeName, string.Empty);
    }

    /// <summary>
    /// Gets the storefront URL of the passed blog post.
    /// The URL is composed from the store location, because the schedule task runs without an HTTP
    /// request and the generic URL helper returns nothing in that case
    /// </summary>
    protected virtual async Task<string> GetBlogPostUrlAsync(BlogPost blogPost)
    {
        var seName = await _urlRecordService.GetSeNameAsync(blogPost, blogPost.LanguageId, true, false);
        if (string.IsNullOrEmpty(seName))
            return string.Empty;

        var urlPrefix = string.Empty;

        //the language code is a part of the generic blog post route, see GenericUrlRouteProvider
        if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
        {
            var language = await _languageService.GetLanguageByIdAsync(blogPost.LanguageId);
            if (!string.IsNullOrEmpty(language?.UniqueSeoCode))
                urlPrefix = $"{language.UniqueSeoCode.ToLowerInvariant()}/";
        }

        return $"{_webHelper.GetStoreLocation()}{urlPrefix}{seName}";
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds the passed newsletter subscriber to the configured BenchmarkEmail list
    /// </summary>
    public async Task SynchronizeSubscriberAsync(NewsLetterSubscription subscription)
    {
        if (subscription == null || string.IsNullOrWhiteSpace(subscription.Email))
            return;

        //skip inactive subscriptions
        if (!subscription.Active)
            return;

        if (!IsConfigured(_settings))
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the plugin is not configured, the subscriber '{subscription.Email}' was skipped.");

            return;
        }

        try
        {
            //try to get customer to fetch first/last name
            var customer = await _customerService.GetCustomerByEmailAsync(subscription.Email);
            var firstName = customer?.FirstName;
            var lastName = customer?.LastName;

            ContactInfo contact = null;

            //adding an existing contact is an idempotent operation on the BenchmarkEmail side, so it is safe to repeat
            await ExecuteWithRetryAsync(async () => { contact = await _httpClient.AddContactAsync(_settings.ListId, subscription.Email, firstName, lastName); });

            if (!string.IsNullOrEmpty(contact?.Id))
                await _genericAttributeService.SaveAttributeAsync(subscription,
                    BenchmarkEmailDefaults.ContactIdAttributeName, contact.Id);

            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the subscriber '{subscription.Email}' was added to the list {_settings.ListId}.");
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync(
                $"{BenchmarkEmailDefaults.SystemName}: unable to add the subscriber '{subscription.Email}'. {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Removes the passed newsletter subscriber from the configured BenchmarkEmail list
    /// </summary>
    public async Task UnsubscribeSubscriberAsync(NewsLetterSubscription subscription)
    {
        if (subscription == null || string.IsNullOrWhiteSpace(subscription.Email))
            return;

        //note: the Active value is deliberately not checked, the unsubscription event is published with the original
        //subscription, so it is still true when a subscriber only unsubscribed or changed their email address

        if (!IsConfigured(_settings))
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the plugin is not configured, the subscriber '{subscription.Email}' was skipped.");

            return;
        }

        try
        {
            //get the contact id stored during subscribe
            var contactId = await _genericAttributeService.GetAttributeAsync<string>(subscription,
                BenchmarkEmailDefaults.ContactIdAttributeName);

            if (string.IsNullOrEmpty(contactId))
            {
                await _logger.WarningAsync(
                    $"{BenchmarkEmailDefaults.SystemName}: unable to unsubscribe '{subscription.Email}', contact ID not found. The subscriber may not have been synced yet.");

                return;
            }

            //unsubscribe: DELETE /Contact/ContactDetails/ALL with ListID and ContactID
            await _httpClient.UnsubscribeContactAsync(_settings.ListId, contactId);

            //clean up stored contact ID since it's no longer valid
            await _genericAttributeService.SaveAttributeAsync(subscription,
                BenchmarkEmailDefaults.ContactIdAttributeName, string.Empty);

            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the subscriber '{subscription.Email}' was unsubscribed from the list {_settings.ListId} (contact ID: {contactId}).");
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync(
                $"{BenchmarkEmailDefaults.SystemName}: unable to unsubscribe the subscriber '{subscription.Email}'. {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Creates a BenchmarkEmail email for the passed blog post immediately.
    /// The email is a copy of the configured template, the placeholder of the copy is then
    /// replaced with the URL of the blog post (generated from the blog post title).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is true when the email was created</returns>
    public virtual async Task ScheduleEmailForBlogPostAsync(BlogPost blogPost)
    {
        if (blogPost == null)
            return;

        if (blogPost.Id == 0)
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the blog post '{blogPost.Title}' has no identifier yet, " +
                "no email was scheduled for it.");

            return;
        }

        if (!IsBlogPostEmailEnabled)
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the creation of the emails of the new blog posts is disabled " +
                $"(enabled: {_settings.Enabled}, create an email on blog post creation: {_settings.EmailOnBlogPostEnabled}), " +
                $"no email was scheduled for the blog post '{blogPost.Title}' (id: {blogPost.Id}).");

            return;
        }

        try
        {
            // Generate the SEO slug from the blog post title (same logic as admin controller)
            var seName = await _urlRecordService.ValidateSeNameAsync(blogPost, null, blogPost.Title, true);

            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: creating email for blog post '{blogPost.Title}' (id: {blogPost.Id}, slug: {seName}).");

            await CreateEmailForBlogPostAsync(blogPost, seName);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync(
                $"{BenchmarkEmailDefaults.SystemName}: unable to schedule email for blog post '{blogPost.Title}'. {exception.Message}", exception);

            // Fallback: set pending flag so the schedule task can retry
            await _genericAttributeService.SaveAttributeAsync(blogPost,
                BenchmarkEmailDefaults.PendingBlogPostEmailAttributeName, true);
        }
    }

    /// <summary>
    /// Creates the BenchmarkEmail emails of the blog posts that are still waiting for them
    /// </summary>
    public virtual async Task ProcessPendingBlogPostEmailsAsync()
    {
        if (!IsBlogPostEmailEnabled)
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the creation of the emails of the new blog posts is disabled " +
                $"(enabled: {_settings.Enabled}, create an email on blog post creation: {_settings.EmailOnBlogPostEnabled}), " +
                "the task has nothing to do.");

            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the API token is not configured, the task has nothing to do.");

            return;
        }

        var pendingIds = await _genericAttributeRepository.Table
            .Where(attribute => attribute.KeyGroup == nameof(BlogPost) &&
                                attribute.Key == BenchmarkEmailDefaults.PendingBlogPostEmailAttributeName &&
                                attribute.StoreId == 0)
            .Select(attribute => attribute.EntityId)
            .ToListAsync();

        await _logger.InformationAsync(
            $"{BenchmarkEmailDefaults.SystemName}: the task has run, {pendingIds.Count} blog post(s) are waiting for an email.");

        if (pendingIds.Count == 0)
            return;

        foreach (var blogPostId in pendingIds)
        {
            var blogPost = await _blogService.GetBlogPostByIdAsync(blogPostId);

            if (blogPost == null)
            {
                //the blog post has been deleted, there is nothing to create the email for
                await _logger.WarningAsync(
                    $"{BenchmarkEmailDefaults.SystemName}: the blog post {blogPostId} was waiting for an email, but it " +
                    "cannot be found. It has probably been deleted, its pending flag is removed.");

                await ClearPendingBlogPostEmailAsync(blogPostId);

                continue;
            }

            //the flag is kept when the creation fails, so the email is created on one of the next runs
            if (await CreateEmailForBlogPostAsync(blogPost))
                await ClearPendingBlogPostEmailAsync(blogPostId);
        }
    }

    /// <summary>
    /// Creates a BenchmarkEmail email for the passed blog post.
    /// The email is a copy of the configured template, the placeholder of the copy is then
    /// replaced with the URL of the blog post
    /// </summary>
    /// <param name="blogPost">Blog post</param>
    /// <param name="seName">Optional pre-generated search engine name (slug). If not provided, it will be fetched.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true when the email was created</returns>
    public virtual async Task<bool> CreateEmailForBlogPostAsync(BlogPost blogPost, string seName = null)
    {
        if (blogPost == null || string.IsNullOrWhiteSpace(blogPost.Title))
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: a blog post without a title cannot be announced, " +
                "its email was not created.");

            return false;
        }

        if (!_settings.Enabled)
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the plugin is disabled, the blog post '{blogPost.Title}' was skipped.");

            return false;
        }

        if (!_settings.EmailOnBlogPostEnabled)
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the creation of the emails is disabled, the blog post '{blogPost.Title}' was skipped.");

            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the API token is not configured, the blog post '{blogPost.Title}' was skipped.");

            return false;
        }

        var templateId = _settings.EmailTemplateId?.Trim();

        if (string.IsNullOrWhiteSpace(templateId))
        {
            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the email template is not configured, the blog post '{blogPost.Title}' was skipped.");

            return false;
        }

        try
        {
            //the copy carries the whole template, including the placeholder of the blog post
            var email = await _httpClient.CopyEmailAsync(templateId, blogPost.Title);

            if (string.IsNullOrEmpty(email.Id))
            {
                await _logger.WarningAsync(
                    $"{BenchmarkEmailDefaults.SystemName}: the copy of the email template {templateId} returned no identifier, the blog post '{blogPost.Title}' was skipped.");

                return false;
            }

            //the subject is built by the plugin, the subject of the template is not used
            var subject = string.Format(BenchmarkEmailDefaults.BlogPostEmailSubjectPattern, blogPost.Title);

            await UpdateCopiedEmailAsync(email, blogPost, seName);

            //the copy endpoint does not take a contact list, the configured one is only recorded here
            //and the email is sent to it from BenchmarkEmail
            var list = string.IsNullOrWhiteSpace(_settings.ListId)
                ? "no contact list configured"
                : $"contact list {_settings.ListId}";

            await _logger.InformationAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the email '{email.Name}' was created for the blog post '{blogPost.Title}' " +
                $"(id: {email.Id}, copy of the template {templateId}, subject: {subject}, {list}).");

            if (string.IsNullOrWhiteSpace(_settings.ListId))
                await _logger.WarningAsync(
                    $"{BenchmarkEmailDefaults.SystemName}: no contact list is configured, the email of the blog post '{blogPost.Title}' was created without an intended list.");

            return true;
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync(
                $"{BenchmarkEmailDefaults.SystemName}: unable to create the email for the blog post '{blogPost.Title}'. {exception.Message}", exception);

            return false;
        }
    }

/// <summary>
    /// Replaces the blog post placeholder of the passed email copy with the URL of the blog post
    /// and sets the subject of the copy, which the plugin builds itself
    /// </summary>
    protected virtual async Task UpdateCopiedEmailAsync(EmailInfo email, BlogPost blogPost, string seName = null)
    {
        //the content is only replaced when the copy carries the placeholder, otherwise the copy keeps
        //the content of the template and the request leaves the content fields out
        var content = await GetCopiedEmailContentAsync(email, blogPost, seName);

        var subject = string.Format(BenchmarkEmailDefaults.BlogPostEmailSubjectPattern, blogPost.Title);

        await _httpClient.UpdateEmailAsync(email.Id, content, subject, _settings.ListId);

        // Schedule the email to be sent after the configured delay
        var scheduleTime = DateTime.UtcNow.AddMinutes(_settings.EmailScheduleDelayMinutes);
        await _httpClient.ScheduleEmailAsync(email.Id, scheduleTime, "UTC");
    }

    /// <summary>
    /// Gets the content of the passed email copy, with the blog post placeholder replaced by the URL
    /// of the blog post
    /// </summary>
    /// <param name="email">Email info</param>
    /// <param name="blogPost">Blog post</param>
    /// <param name="seName">Optional pre-generated search engine name (slug). If not provided, it will be fetched.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the new content of
    /// the copy, or null when the content of the copy has to stay as it is</returns>
    protected virtual async Task<string> GetCopiedEmailContentAsync(EmailInfo email, BlogPost blogPost, string seName = null)
    {
        if (string.IsNullOrEmpty(email.Content))
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the copy of the email template returned no content, the placeholder " +
                $"{BenchmarkEmailDefaults.BlogPostPlaceholder} of the email '{email.Name}' (id: {email.Id}) was not replaced.");

            return null;
        }

        if (!email.Content.Contains(BenchmarkEmailDefaults.BlogPostPlaceholder, StringComparison.Ordinal))
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the content of the email template does not contain the placeholder " +
                $"{BenchmarkEmailDefaults.BlogPostPlaceholder}, the content of the email '{email.Name}' (id: {email.Id}) was left unchanged.");

            return null;
        }

        string blogPostUrl;

        if (!string.IsNullOrEmpty(seName))
        {
            // Build URL directly from pre-generated slug
            var urlPrefix = string.Empty;

            if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                var language = await _languageService.GetLanguageByIdAsync(blogPost.LanguageId);
                if (!string.IsNullOrEmpty(language?.UniqueSeoCode))
                    urlPrefix = $"{language.UniqueSeoCode.ToLowerInvariant()}/";
            }

            blogPostUrl = $"{_webHelper.GetStoreLocation()}{urlPrefix}{seName}";
        }
        else
        {
            // Fallback: fetch slug from database
            blogPostUrl = await GetBlogPostUrlAsync(blogPost);
        }

        if (string.IsNullOrEmpty(blogPostUrl))
        {
            await _logger.WarningAsync(
                $"{BenchmarkEmailDefaults.SystemName}: the URL of the blog post '{blogPost.Title}' could not be built, " +
                $"the placeholder {BenchmarkEmailDefaults.BlogPostPlaceholder} of the email '{email.Name}' (id: {email.Id}) was not replaced.");

            return null;
        }

        return email.Content.Replace(BenchmarkEmailDefaults.BlogPostPlaceholder, blogPostUrl, StringComparison.Ordinal);
    }

    #endregion
}
