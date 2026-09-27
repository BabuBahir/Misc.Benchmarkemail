using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.Benchmarkemail.Services;

/// <summary>
/// Represents a schedule task that creates the BenchmarkEmail emails of the new blog posts
/// </summary>
public class BlogPostEmailTask : IScheduleTask
{
    #region Fields

    protected readonly BenchmarkEmailManager _benchmarkEmailManager;

    #endregion

    #region Ctor

    public BlogPostEmailTask(BenchmarkEmailManager benchmarkEmailManager)
    {
        _benchmarkEmailManager = benchmarkEmailManager;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Execute task
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task ExecuteAsync()
    {
        await _benchmarkEmailManager.ProcessPendingBlogPostEmailsAsync();
    }

    #endregion
}