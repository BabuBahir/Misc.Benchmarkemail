using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Benchmarkemail.Models;

/// <summary>
/// Represents the request that schedules an existing email.
/// </summary>
public class ScheduleEmailRequest
{
    [JsonProperty("ScheduleDate")]
    public string ScheduleDate { get; set; }

    [JsonProperty("TimeZone")]
    public string TimeZone { get; set; } = "UTC";
}