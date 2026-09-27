using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Nop.Core.Http;
using Nop.Plugin.Misc.Benchmarkemail.Models;
using Nop.Plugin.Misc.Benchmarkemail.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.Benchmarkemail.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class BenchmarkEmailController : BasePluginController
{
    #region Fields

    protected readonly BenchmarkEmailSettings _benchmarkEmailSettings;
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly ILogger _logger;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public BenchmarkEmailController(BenchmarkEmailSettings benchmarkEmailSettings,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService)
    {
        _benchmarkEmailSettings = benchmarkEmailSettings;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure()
    {
        return View("~/Plugins/Misc.Benchmarkemail/Views/Configure.cshtml", await PrepareModelAsync());
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    [ActionName("Configure")]
    [FormValueRequired("save")]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var apiToken = model.ApiToken?.Trim() ?? string.Empty;

        ApplyConfiguration(model, apiToken);

        _benchmarkEmailSettings.ListId = model.ListId?.Trim() ?? string.Empty;
        _benchmarkEmailSettings.EmailScheduleDelayMinutes = model.EmailScheduleDelayMinutes;

        await _settingService.SaveSettingAsync(_benchmarkEmailSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    /// <summary>
    /// Loads the contact lists of the account with the API token passed in the form and caches them
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    [ActionName("Configure")]
    [FormValueRequired("loadLists")]
    public async Task<IActionResult> LoadLists(ConfigurationModel model)
    {
        model ??= new ConfigurationModel();

        if (string.IsNullOrWhiteSpace(model.ApiToken))
        {
            _notificationService.ErrorNotification(string.Format(
                await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsFailed"),
                await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ApiTokenRequired")));

            return await ConfigureAsync(model);
        }

        try
        {
            //the lists endpoint validates the token and returns the contact lists of the account at once
            var lists = await CreateClient(model).GetListsAsync();

            var apiToken = model.ApiToken.Trim();

            //saves the settings as well, the admin has typed the token and does not have to save it separately
            ApplyConfiguration(model, apiToken);

            //the configured list is deliberately left untouched, the dropdown of the form was empty until now
            _benchmarkEmailSettings.ContactListsJson = JsonConvert.SerializeObject(lists);

            await _settingService.SaveSettingAsync(_benchmarkEmailSettings);

            var message = new StringBuilder();
            message.Append(await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsSuccess"));
            message.Append(". ");
            message.Append(string.Format(
                await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsCount"), lists.Count));

            _notificationService.SuccessNotification(message.ToString());
        }
        catch (Exception exception)
        {
            var errorMessage = exception.Message;
            if (exception.InnerException != null && !string.IsNullOrEmpty(exception.InnerException.Message))
                errorMessage = exception.InnerException.Message;

            await _logger.ErrorAsync($"BenchmarkEmail LoadLists failed: {errorMessage}", exception);

            _notificationService.ErrorNotification(string.Format(
                await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.LoadListsFailed"), errorMessage));
        }

        return await ConfigureAsync(model);
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Renders the configuration page, keeping the values passed by the load lists request
    /// </summary>
    protected virtual async Task<IActionResult> ConfigureAsync(ConfigurationModel model)
    {
        var resultModel = await PrepareModelAsync();

        //the load lists request does not always save the settings, keep what has been typed in
        if (!string.IsNullOrWhiteSpace(model.ApiToken))
            resultModel.ApiToken = model.ApiToken.Trim();
        if (!string.IsNullOrWhiteSpace(model.ListId))
            resultModel.ListId = model.ListId.Trim();

        resultModel.Enabled = model.Enabled;
        resultModel.EmailOnBlogPostEnabled = model.EmailOnBlogPostEnabled;
        resultModel.EmailTemplateId = string.IsNullOrWhiteSpace(model.EmailTemplateId)
            ? resultModel.EmailTemplateId
            : model.EmailTemplateId.Trim();

        resultModel.EmailScheduleDelayMinutes = model.EmailScheduleDelayMinutes;

        resultModel.LogRequests = model.LogRequests;

        //a list typed into the select2 is not part of the loaded lists, it has to stay selectable
        await PopulateAvailableListsAsync(resultModel, resultModel.ListId);

        return View("~/Plugins/Misc.Benchmarkemail/Views/Configure.cshtml", resultModel);
    }

    /// <summary>
    /// Prepares the configuration model based on the current settings
    /// </summary>
    protected virtual async Task<ConfigurationModel> PrepareModelAsync()
    {
        var model = new ConfigurationModel
        {
            Enabled = _benchmarkEmailSettings.Enabled,
            ApiToken = _benchmarkEmailSettings.ApiToken,
            ListId = _benchmarkEmailSettings.ListId,
            EmailOnBlogPostEnabled = _benchmarkEmailSettings.EmailOnBlogPostEnabled,
            EmailTemplateId = _benchmarkEmailSettings.EmailTemplateId,
            EmailScheduleDelayMinutes = _benchmarkEmailSettings.EmailScheduleDelayMinutes,
            LogRequests = _benchmarkEmailSettings.LogRequests
        };

        await PopulateAvailableListsAsync(model, model.ListId);

        return model;
    }

    /// <summary>
    /// Copies the passed configuration model over the settings
    /// </summary>
    /// <param name="model">Configuration model</param>
    /// <param name="apiToken">The already trimmed API token of the model</param>
    protected virtual void ApplyConfiguration(ConfigurationModel model, string apiToken)
    {
        _benchmarkEmailSettings.Enabled = model.Enabled;
        _benchmarkEmailSettings.EmailOnBlogPostEnabled = model.EmailOnBlogPostEnabled;
        _benchmarkEmailSettings.EmailTemplateId = model.EmailTemplateId?.Trim() ?? string.Empty;
        _benchmarkEmailSettings.EmailScheduleDelayMinutes = model.EmailScheduleDelayMinutes;
        _benchmarkEmailSettings.LogRequests = model.LogRequests;

        //the cached lists belong to a single account, another token invalidates them
        if (!string.Equals(_benchmarkEmailSettings.ApiToken?.Trim(), apiToken, StringComparison.Ordinal))
            _benchmarkEmailSettings.ContactListsJson = string.Empty;

        _benchmarkEmailSettings.ApiToken = apiToken;
    }

    /// <summary>
    /// Fills the dropdown of the contact lists with the cached lists of the account
    /// </summary>
    /// <param name="model">Configuration model</param>
    /// <param name="selectedIds">The configured list ids, they stay selectable even when the API does not return them</param>
    protected virtual async Task PopulateAvailableListsAsync(ConfigurationModel model, params string[] selectedIds)
    {
        var lists = DeserializeContactLists(_benchmarkEmailSettings.ContactListsJson);

        var items = new List<SelectListItem>
        {
            new(await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.SelectList"), string.Empty)
        };

        //a list without a name would render as a blank entry, fall back to its id
        items.AddRange(lists.Where(list => !string.IsNullOrEmpty(list.Id))
            .Select(list => new SelectListItem(string.IsNullOrWhiteSpace(list.Name) ? list.Id : list.Name, list.Id)));

        foreach (var selectedId in selectedIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
        {
            //a configured list that is no longer returned is kept as an option, so that saving the page does not reset it
            if (items.Any(item => item.Value == selectedId))
                continue;

            items.Add(new SelectListItem(string.Format(
                await _localizationService.GetResourceAsync("Nop.Plugin.Misc.Benchmarkemail.ListNotFound"), selectedId), selectedId));
        }

        model.AvailableLists = items;
    }

    /// <summary>
    /// Deserializes the cached contact lists
    /// </summary>
    protected static IList<ListInfo> DeserializeContactLists(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<ListInfo>();

        try
        {
            return JsonConvert.DeserializeObject<List<ListInfo>>(json) ?? new List<ListInfo>();
        }
        catch (JsonException)
        {
            //the cache is only a convenience, a corrupted value is ignored
            return new List<ListInfo>();
        }
    }

    /// <summary>
    /// Creates a BenchmarkEmail client based on the passed (not yet saved) settings
    /// </summary>
    protected virtual BenchmarkEmailHttpClient CreateClient(ConfigurationModel model)
    {
        var settings = new BenchmarkEmailSettings
        {
            Enabled = model.Enabled,
            ApiToken = model.ApiToken?.Trim(),
            ListId = model.ListId?.Trim(),
            LogRequests = model.LogRequests
        };

        //use the default client of the application, it is configured with the store proxy settings
        return new BenchmarkEmailHttpClient(_httpClientFactory.CreateClient(NopHttpDefaults.DefaultHttpClient), settings, _logger);
    }

    #endregion
}