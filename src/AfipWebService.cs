using System.Text.Json;

namespace Afip.Net;

public class AfipWebService
{
    protected readonly Afip Afip;
    private readonly Dictionary<string, object?> _options;

    public AfipWebService(Afip afip, string service, IDictionary<string, object?>? options = null)
    {
        Afip = afip ?? throw new ArgumentNullException(nameof(afip));
        Service = !string.IsNullOrWhiteSpace(service) ? service : throw new ArgumentException("service is required", nameof(service));
        _options = options != null ? new Dictionary<string, object?>(options) : new Dictionary<string, object?>();

        if (!_options.ContainsKey("soapV1_2"))
        {
            _options["soapV1_2"] = true;
        }
    }

    public string Service { get; }
    public IReadOnlyDictionary<string, object?> Options => _options;

    public Task<JsonDocument> GetTokenAuthorizationAsync(bool force = false)
    {
        return Afip.GetServiceTaAsync(Service, force);
    }

    public async Task<JsonDocument> ExecuteRequestAsync(string method, object? parameters = null)
    {
        var requestData = new Dictionary<string, object?>
        {
            ["method"] = method,
            ["params"] = parameters ?? new { },
            ["environment"] = Afip.Options.Production ? "prod" : "dev",
            ["wsid"] = Service,
            ["soap_v_1_2"] = GetOptionBoolean("soapV1_2", true)
        };

        AddOption(requestData, "url");
        AddOption(requestData, "wsdl");
        AddOption(requestData, "url_test", "URL_TEST");
        AddOption(requestData, "wsdl_test", "WSDL_TEST");

        return await Afip.PostJsonAsync("v1/afip/requests", requestData);
    }

    private bool GetOptionBoolean(string key, bool defaultValue)
    {
        if (_options.TryGetValue(key, out var value) && value is bool boolValue)
        {
            return boolValue;
        }

        return defaultValue;
    }

    private void AddOption(Dictionary<string, object?> data, string jsonKey, string? optionKey = null)
    {
        optionKey ??= jsonKey;
        if (_options.TryGetValue(optionKey, out var value) && value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
        {
            data[jsonKey] = stringValue;
        }
    }
}
