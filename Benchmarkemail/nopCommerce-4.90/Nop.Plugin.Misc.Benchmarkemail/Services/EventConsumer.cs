using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Messages;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.Benchmarkemail.Services;

public class EventConsumer :
    IConsumer<EmailSubscribedEvent>,
    IConsumer<EmailUnsubscribedEvent>,
    IConsumer<EntityInsertedEvent<BlogPost>>
{
    #region Fields

    protected readonly BenchmarkEmailManager _benchmarkEmailManager;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILogger _logger;

    #endregion

    #region Ctor

    public EventConsumer(BenchmarkEmailManager benchmarkEmailManager,
        IGenericAttributeService genericAttributeService,
        ILogger logger)
    {
        _benchmarkEmailManager = benchmarkEmailManager;
        _genericAttributeService = genericAttributeService;
        _logger = logger;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds a new newsletter subscriber to the configured BenchmarkEmail list
    /// </summary>
    public async Task HandleEventAsync(EmailSubscribedEvent eventMessage)
    {
        if (eventMessage?.Subscription == null)
            return;

        await _benchmarkEmailManager.SynchronizeSubscriberAsync(eventMessage.Subscription);
    }

    /// <summary>
    /// Removes a newsletter subscriber from the configured BenchmarkEmail list
    /// </summary>
    public async Task HandleEventAsync(EmailUnsubscribedEvent eventMessage)
    {
        if (eventMessage?.Subscription == null)
            return;

        await _benchmarkEmailManager.UnsubscribeSubscriberAsync(eventMessage.Subscription);
    }

    /// <summary>
    /// Creates the BenchmarkEmail email for a blog post immediately when the blog post is created.
    /// The email is created right away using the generated slug from the blog post title.
    /// </summary>
    public async Task HandleEventAsync(EntityInsertedEvent<BlogPost> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        await _benchmarkEmailManager.ScheduleEmailForBlogPostAsync(eventMessage.Entity);
    }

    #endregion
}