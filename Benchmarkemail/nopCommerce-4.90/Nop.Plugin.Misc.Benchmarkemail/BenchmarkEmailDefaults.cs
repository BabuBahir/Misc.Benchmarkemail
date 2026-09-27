namespace Nop.Plugin.Misc.Benchmarkemail;

public static class BenchmarkEmailDefaults
{
    public static string SystemName => "Misc.Benchmarkemail";
    public static string ConfigurationPageUrl => "Admin/BenchmarkEmail/Configure";

    /// <summary>
    /// Gets the base URL of the BenchmarkEmail API
    /// </summary>
    public static string BaseApiUrl => "https://clientapi.benchmarkemail.com";

    /// <summary>
    /// Gets the name of the header used to authenticate the BenchmarkEmail API requests
    /// </summary>
    public static string AuthTokenHeader => "AuthToken";

    /// <summary>
    /// Gets the URL of the request that returns the contact lists
    /// </summary>
    public static string ListsUrl => "/Contact/";

    /// <summary>
    /// Gets the format of the URL of the request that adds a contact to a list
    /// </summary>
    public static string ContactDetailsUrlFormat => "/Contact/{0}/ContactDetails";

    /// <summary>
    /// Gets the URL of the request that removes a contact from a list (unsubscribe)
    /// </summary>
    public static string ContactDetailsAllUrl => "/Contact/ContactDetails/ALL";

    /// <summary>
    /// Gets the format of the URL of a request that addresses a single email.
    /// A POST copies the email, a PATCH updates it
    /// </summary>
    public static string EmailUrlFormat => "/Emails/{0}";

    /// <summary>
    /// Gets the format of the URL of a request that schedules an email.
    /// </summary>
    public static string EmailScheduleUrlFormat => "/Emails/{0}/Schedule";

    /// <summary>
    /// Gets the identifier of the email that is copied for every new blog post
    /// </summary>
    public static string DefaultEmailTemplateId => "29182994";

    /// <summary>
    /// Gets the placeholder of the copied template that is replaced with the URL of the blog post
    /// </summary>
    public static string BlogPostPlaceholder => "XXXblogpostXXX";

    /// <summary>
    /// Gets the pattern of the subject of the email that is created for a new blog post.
    /// The title of the blog post is filled in, e.g. 'Check out our latest post: My new post'
    /// </summary>
    public static string BlogPostEmailSubjectPattern => "Check out our latest post: {0}";

    public static int RequestTimeout => 30;

    /// <summary>
    /// Gets the number of attempts of a request that is safe to repeat
    /// </summary>
    public static int MaxRequestAttempts => 3;

    /// <summary>
    /// Gets the delay between the attempts of a request that is safe to repeat, in milliseconds
    /// </summary>
    public static int RetryDelay => 2000;

    /// <summary>
    /// Gets the name of the schedule task that creates the emails of the new blog posts
    /// </summary>
    public static string BlogPostEmailTaskName => "Blog post emails (BenchmarkEmail plugin)";

    /// <summary>
    /// Gets the type of the schedule task that creates the emails of the new blog posts
    /// </summary>
    public static string BlogPostEmailTask => "Nop.Plugin.Misc.Benchmarkemail.Services.BlogPostEmailTask";

    /// <summary>
    /// Gets the period of the schedule task that creates the emails of the new blog posts, in minutes.
    /// The emails are created by a run of the task that the save of the search engine name of a new blog post
    /// triggers, the period is the safety net for the runs that were missed, e.g. while the application was not running
    /// </summary>
    public static int DefaultBlogPostEmailPeriod => 1;

    /// <summary>
    /// Gets the name of the attribute that holds the BenchmarkEmail identifier of a newsletter subscription
    /// </summary>
    public static string ContactIdAttributeName => "BenchmarkEmailContactId";

    /// <summary>
    /// Gets the name of the attribute that flags a blog post waiting for its BenchmarkEmail email.
    /// The mere presence of the attribute means the email is still pending, it is removed once the email is created.
    /// </summary>
    public static string PendingBlogPostEmailAttributeName => "BenchmarkEmailBlogPostEmailPending";
}