using System.Net;
using System.Text;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Logging;
using Nop.Plugin.Misc.Benchmarkemail.Models;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.Benchmarkemail.Services;

/// <summary>
/// Represents an HTTP client to request the BenchmarkEmail API
/// </summary>
public class BenchmarkEmailHttpClient
{
    #region Fields

    protected readonly HttpClient _httpClient;
    protected readonly BenchmarkEmailSettings _settings;
    protected readonly ILogger _logger;

    #endregion

    #region Ctor

    public BenchmarkEmailHttpClient(HttpClient httpClient,
        BenchmarkEmailSettings settings,
        ILogger logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        //configure client
        httpClient.BaseAddress = new Uri(BenchmarkEmailDefaults.BaseApiUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(BenchmarkEmailDefaults.RequestTimeout);
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, MimeTypes.ApplicationJson);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the contact lists of the account
    /// </summary>
    public async Task<IList<ListInfo>> GetListsAsync()
    {
        var response = await RequestAsync(HttpMethod.Get, BenchmarkEmailDefaults.ListsUrl);
        if (response?.Data == null)
            return new List<ListInfo>();

        var lists = response.Data.ToObject<List<ListResponse>>() ?? new List<ListResponse>();

        return lists.Where(list => !string.IsNullOrEmpty(list.Id)).Select(list => new ListInfo
        {
            Id = list.Id,
            Name = list.Name
        }).ToList();
    }

    /// <summary>
    /// Adds a contact to the passed contact list
    /// </summary>
    public async Task<ContactInfo> AddContactAsync(string listId, string email, string firstName = null, string lastName = null)
    {
        var url = string.Format(BenchmarkEmailDefaults.ContactDetailsUrlFormat, listId);
        var request = new AddContactRequest
        {
            Data = new AddContactData
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            }
        };

        var response = await RequestAsync(HttpMethod.Post, url, JsonConvert.SerializeObject(request));
        var contact = response?.Data?.ToObject<AddContactResponse>();

        return new ContactInfo
        {
            Id = contact?.Id ?? string.Empty,
            Email = email
        };
    }

    /// <summary>
    /// Removes a contact from a contact list (unsubscribe)
    /// </summary>
    public async Task UnsubscribeContactAsync(string listId, string contactId)
    {
        if (string.IsNullOrWhiteSpace(listId))
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: the contact list identifier is not specified.");

        if (string.IsNullOrWhiteSpace(contactId))
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: the contact identifier is not specified.");

        var url = BenchmarkEmailDefaults.ContactDetailsAllUrl;
        var request = new UnsubscribeContactRequest
        {
            ListID = listId,
            ContactID = contactId
        };

        await RequestAsync(HttpMethod.Delete, url, JsonConvert.SerializeObject(request));
    }

    /// <summary>
    /// Copies the passed email. The copy keeps the content of the source email,
    /// so that its placeholders can be replaced afterwards
    /// </summary>
    /// <param name="emailId">Identifier of the email to copy</param>
    /// <param name="name">Name of the new email</param>
    public async Task<EmailInfo> CopyEmailAsync(string emailId, string name)
    {
        if (string.IsNullOrWhiteSpace(emailId))
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: the identifier of the email to copy is not specified.");

        var request = new CopyEmailRequest { Name = name };

        var response = await RequestAsync(HttpMethod.Post, GetEmailUrl(emailId), JsonConvert.SerializeObject(request));
        var email = response?.Data?.ToObject<EmailResponse>();

        return new EmailInfo
        {
            Id = email?.Id ?? string.Empty,
            Name = email?.Name ?? name,
            Content = email?.GetContent() ?? string.Empty,
            Subject = email?.Subject ?? string.Empty
        };
    }

    /// <summary>
    /// Replaces the content and the subject of the passed email.
    /// A null value leaves the field as it is on the BenchmarkEmail side
    /// </summary>
    /// <param name="emailId">Identifier of the email to update</param>
    /// <param name="content">The new content of the email, or null to keep the current one</param>
    /// <param name="subject">The new subject of the email, or null to keep the current one</param>
    /// <param name="listId">Optional contact list ID to associate with the email</param>
    public async Task UpdateEmailAsync(string emailId, string content, string subject, string listId = null)
    {
        if (string.IsNullOrWhiteSpace(emailId))
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: the identifier of the email to update is not specified.");

        var detail = new EmailContentDetail
        {
            TemplateCode = content,
            TemplateContent = content,
            Subject = subject
        };

        if (!string.IsNullOrWhiteSpace(listId))
        {
            detail.ContactLists = new List<ContactListItem>
            {
                new ContactListItem { ID = listId, Selected = true }
            };
        }

        var request = new UpdateEmailRequest
        {
            Detail = detail
        };

        await RequestAsync(HttpMethod.Patch, GetEmailUrl(emailId), JsonConvert.SerializeObject(request));
    }

    /// <summary>
    /// Schedules the passed email to be sent at the specified date and time.
    /// </summary>
    /// <param name="emailId">Identifier of the email to schedule</param>
    /// <param name="scheduleDate">The date and time to schedule the email for (UTC)</param>
    /// <param name="timeZone">The timezone for the schedule date (default: UTC)</param>
    public async Task ScheduleEmailAsync(string emailId, DateTime scheduleDate, string timeZone = "UTC")
    {
        if (string.IsNullOrWhiteSpace(emailId))
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: the identifier of the email to schedule is not specified.");

        var url = string.Format(BenchmarkEmailDefaults.EmailScheduleUrlFormat, emailId);
        var request = new ScheduleEmailRequest
        {
            ScheduleDate = scheduleDate.ToString("dd MMM yyyy HH:mm"),
            TimeZone = timeZone
        };

        await RequestAsync(HttpMethod.Post, url, JsonConvert.SerializeObject(request));
    }

    /// <summary>
    /// Gets the URL of the passed email
    /// </summary>
    protected static string GetEmailUrl(string emailId)
    {
        return string.Format(BenchmarkEmailDefaults.EmailUrlFormat, emailId);
    }

    /// <summary>
    /// Requests the BenchmarkEmail API
    /// </summary>
    protected virtual async Task<ApiResponse> RequestAsync(HttpMethod httpMethod, string url, string data = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
            throw new BenchmarkEmailApiException($"{BenchmarkEmailDefaults.SystemName}: the API token is not configured.");

        var request = new HttpRequestMessage(httpMethod, url.TrimStart('/'));
        request.Headers.TryAddWithoutValidation(BenchmarkEmailDefaults.AuthTokenHeader, _settings.ApiToken.Trim());
        if (!string.IsNullOrEmpty(data))
            request.Content = new StringContent(data, Encoding.UTF8, MimeTypes.ApplicationJson);
        else
        {
            //the API documentation sends the content type on the bodyless requests as well.
            //HttpRequestHeaders silently drops a content header, so it has to be set on the content.
            //The content is empty, the GET requests do not send a body
            request.Content = new ByteArrayContent([]);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MimeTypes.ApplicationJson);
        }

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.SendAsync(request);
        }
        catch (Exception exception)
        {
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: unable to request '{url}'. {exception.Message}", exception);
        }

        var content = await httpResponse.Content.ReadAsStringAsync();

        if (_settings.LogRequests)
            await _logger.InsertLogAsync(LogLevel.Debug, $"{BenchmarkEmailDefaults.SystemName} {httpMethod} {url}", content);

        if (!httpResponse.IsSuccessStatusCode)
            throw CreateApiException(httpResponse.StatusCode, content);

        ApiResponse response;
        try
        {
            //every response is wrapped into the Response property
            response = JsonConvert.DeserializeObject<ApiResponseWrapper>(content)?.Response;
        }
        catch (Exception exception)
        {
            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName}: unable to parse the response of '{url}'. {exception.Message}", exception);
        }

        //the API returns the status -1 when the request cannot be completed
        if (response == null || !response.IsSuccess)
        {
            var errorMessage = response?.GetErrorMessage();
            var status = response?.Status ?? 0;

            throw new BenchmarkEmailApiException(
                $"{BenchmarkEmailDefaults.SystemName} error (status {status}): {(string.IsNullOrEmpty(errorMessage) ? content : errorMessage)}",
                status, errorMessage);
        }

        return response;
    }

    /// <summary>
    /// Creates an exception based on an unsuccessful HTTP response
    /// </summary>
    protected virtual BenchmarkEmailApiException CreateApiException(HttpStatusCode statusCode, string content)
    {
        var statusCodeValue = (int)statusCode;
        var errorMessage = string.IsNullOrWhiteSpace(content) ? $"HTTP {statusCodeValue}" : content;

        return new BenchmarkEmailApiException(
            $"{BenchmarkEmailDefaults.SystemName} error (HTTP {statusCodeValue}): {errorMessage}", statusCodeValue, errorMessage);
    }

    #endregion

    #region Nested classes

    /// <summary>
    /// Represents the wrapper of every BenchmarkEmail API response
    /// </summary>
    private class ApiResponseWrapper
    {
        [JsonProperty("Response")]
        public ApiResponse Response { get; set; }
    }

    #endregion
}