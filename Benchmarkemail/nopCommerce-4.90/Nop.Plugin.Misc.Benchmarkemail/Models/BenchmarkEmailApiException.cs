namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents an error returned by the BenchmarkEmail API
/// </summary>
public class BenchmarkEmailApiException : Exception
{
    public BenchmarkEmailApiException(string message, int status = 0, string errorMessage = "") : base(message)
    {
        Status = status;
        ErrorMessage = errorMessage;
    }

    public BenchmarkEmailApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the status returned by the API, -1 means an error
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// Gets the messages of the errors returned by the API
    /// </summary>
    public string ErrorMessage { get; }
}