using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Afip.Net;

public sealed class Afip
{
    private const string DefaultApiBaseUrl = "https://app.afipsdk.com/api/";
    private readonly HttpClient _adminClient;

    public AfipOptions Options { get; }

    public Afip(AfipOptions? options = null, HttpClient? httpClient = null)
    {
        Options = options ?? new AfipOptions();
        _adminClient = httpClient ?? new HttpClient();
        _adminClient.BaseAddress = new Uri(Options.ApiBaseUrl ?? DefaultApiBaseUrl);
        _adminClient.Timeout = TimeSpan.FromSeconds(30);
        _adminClient.DefaultRequestHeaders.Accept.Clear();
        _adminClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _adminClient.DefaultRequestHeaders.UserAgent.ParseAdd("Afip.Net/1.0");
        _adminClient.DefaultRequestHeaders.Add("sdk-version-number", "1.0.0");
        _adminClient.DefaultRequestHeaders.Add("sdk-library", "dotnet");
        _adminClient.DefaultRequestHeaders.Add("sdk-environment", Options.Production ? "prod" : "dev");

        if (!string.IsNullOrWhiteSpace(Options.AccessToken))
        {
            _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.AccessToken);
        }

        ElectronicBilling = new ElectronicBilling(this);
        RegisterScopeFour = new RegisterScopeFour(this);
        RegisterScopeFive = new RegisterScopeFive(this);
        RegisterInscriptionProof = new RegisterInscriptionProof(this);
        RegisterScopeTen = new RegisterScopeTen(this);
        RegisterScopeThirteen = new RegisterScopeThirteen(this);
    }

    internal HttpClient AdminClient => _adminClient;

    public ElectronicBilling ElectronicBilling { get; }
    public RegisterScopeFour RegisterScopeFour { get; }
    public RegisterScopeFive RegisterScopeFive { get; }
    public RegisterInscriptionProof RegisterInscriptionProof { get; }
    public RegisterScopeTen RegisterScopeTen { get; }
    public RegisterScopeThirteen RegisterScopeThirteen { get; }

    public AfipWebService WebService(string service, IDictionary<string, object?>? options = null)
    {
        return new AfipWebService(this, service, options);
    }

    public async Task<JsonDocument> GetServiceTaAsync(string service, bool force = false)
    {
        var body = new Dictionary<string, object?>
        {
            ["environment"] = Options.Production ? "prod" : "dev",
            ["wsid"] = service,
            ["tax_id"] = Options.Cuit,
            ["force_create"] = force
        };

        if (!string.IsNullOrWhiteSpace(Options.Cert))
        {
            body["cert"] = Options.Cert;
        }

        if (!string.IsNullOrWhiteSpace(Options.Key))
        {
            body["key"] = Options.Key;
        }

        return await PostJsonAsync("v1/afip/auth", body);
    }

    public async Task<JsonDocument> GetLastRequestXmlAsync()
    {
        return await GetJsonAsync("v1/afip/requests/last-xml");
    }

    public async Task<JsonDocument> CreateAutomationAsync(string automation, object? parameters = null, bool wait = true)
    {
        var body = new Dictionary<string, object?>
        {
            ["automation"] = automation,
            ["params"] = parameters ?? new { }
        };

        var result = await PostJsonAsync("v1/automations", body);

        if (!wait)
        {
            return result;
        }

        var id = GetPropertyString(result, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return result;
        }

        return await GetAutomationDetailsAsync(id, true);
    }

    public async Task<JsonDocument> GetAutomationDetailsAsync(string id, bool wait = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Automation id is required.", nameof(id));
        }

        if (!wait)
        {
            return await GetJsonAsync($"v1/automations/{Uri.EscapeDataString(id)}");
        }

        const int maxRetries = 24;
        var retries = maxRetries;
        while (retries-- >= 0)
        {
            var result = await GetJsonAsync($"v1/automations/{Uri.EscapeDataString(id)}");
            var status = GetPropertyString(result, "status");
            if (string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            if (retries < 0)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        throw new InvalidOperationException("Error: Waiting for too long.");
    }

    public async Task<JsonDocument> CreateCertAsync(string username, string password, string alias)
    {
        var body = new Dictionary<string, object?>
        {
            ["environment"] = Options.Production ? "prod" : "dev",
            ["tax_id"] = Options.Cuit,
            ["username"] = username,
            ["password"] = password,
            ["alias"] = alias
        };

        if (!string.IsNullOrWhiteSpace(Options.Cert))
        {
            body["cert"] = Options.Cert;
        }

        if (!string.IsNullOrWhiteSpace(Options.Key))
        {
            body["key"] = Options.Key;
        }

        return await WaitForCompletionAsync("v1/afip/certs", body);
    }

    public async Task<JsonDocument> CreateWsAuthAsync(string username, string password, string alias, string wsid)
    {
        var body = new Dictionary<string, object?>
        {
            ["environment"] = Options.Production ? "prod" : "dev",
            ["tax_id"] = Options.Cuit,
            ["username"] = username,
            ["password"] = password,
            ["alias"] = alias,
            ["wsid"] = wsid
        };

        return await WaitForCompletionAsync("v1/afip/ws-auths", body);
    }

    internal async Task<JsonDocument> PostJsonAsync(string requestUri, object body)
    {
        var json = await SendJsonStringAsync(HttpMethod.Post, requestUri, body);
        return JsonDocument.Parse(json);
    }

    internal async Task<JsonDocument> GetJsonAsync(string requestUri)
    {
        var json = await SendJsonStringAsync(HttpMethod.Get, requestUri);
        return JsonDocument.Parse(json);
    }

    private async Task<string> SendJsonStringAsync(HttpMethod method, string requestUri, object? body = null)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var response = await _adminClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<JsonDocument> WaitForCompletionAsync(string endpoint, IDictionary<string, object?> body)
    {
        var payload = new Dictionary<string, object?>(body);
        var retries = 24;

        while (retries-- >= 0)
        {
            var result = await PostJsonAsync(endpoint, payload);
            var status = GetPropertyString(result, "status");
            if (string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            if (GetPropertyString(result, "long_job_id") is string longJobId && !string.IsNullOrWhiteSpace(longJobId))
            {
                payload["long_job_id"] = longJobId;
            }

            if (retries < 0)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        throw new InvalidOperationException("Error: Waiting for too long.");
    }

    private static string? GetPropertyString(JsonDocument document, string propertyName)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (document.RootElement.TryGetProperty(propertyName, out var property))
        {
            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => property.GetRawText()
            };
        }

        return null;
    }
}
