using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the BenchmarkEmail API response
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Gets or sets the status of the response, 1 means success
    /// </summary>
    [JsonProperty("Status")]
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the result of the request, its shape depends on the endpoint
    /// </summary>
    [JsonProperty("Data")]
    public JToken Data { get; set; }

    /// <summary>
    /// Gets or sets the errors of the request
    /// </summary>
    [JsonProperty("Error")]
    public IList<ApiError> Error { get; set; } = new List<ApiError>();

    /// <summary>
    /// Gets a value indicating whether the request was successful
    /// </summary>
    public bool IsSuccess => Status == 1;

    /// <summary>
    /// Gets the messages of the returned errors
    /// </summary>
    public string GetErrorMessage()
    {
        if (Error == null || Error.Count == 0)
            return string.Empty;

        return string.Join("; ", Error.Select(error => error.Message).Where(message => !string.IsNullOrEmpty(message)));
    }
}