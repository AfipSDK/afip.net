namespace AfipSDK.Afip.Net;

public sealed class Afip
{
    private const string SdkVersionNumber = "1.1.1";
    private const string ApiBaseUrl = "https://app.afipsdk.com/api/";
    internal AfipHttpClient AdminClient { get; }

    public AfipOptions Options { get; }
    public ElectronicBilling ElectronicBilling { get; }
    public RegisterInscriptionProof RegisterInscriptionProof { get; }
    public RegisterScopeThirteen RegisterScopeThirteen { get; }

    public Afip(AfipOptions? options = null, HttpClient? httpClient = null)
    {
        Options = options ?? new AfipOptions();
        AdminClient = new AfipHttpClient(
            Options,
            httpClient ?? new HttpClient() { Timeout = TimeSpan.FromSeconds(30) },
            SdkVersionNumber,
            ApiBaseUrl
        );

        ElectronicBilling = new ElectronicBilling(this);
        RegisterInscriptionProof = new RegisterInscriptionProof(this);
        RegisterScopeThirteen = new RegisterScopeThirteen(this);
    }

    #region Sync

    public GetServiceTAResponse GetServiceTa(string service, bool force = false)
        => GetServiceTaAsync(service, force).GetAwaiter().GetResult();

    public GetLastRequestXmlResponse GetLastRequestXml()
        => GetLastRequestXmlAsync().GetAwaiter().GetResult();

    public AutomationResponse CreateAutomation(string automation, Dictionary<string, object?> parameters, bool wait = true)
        => CreateAutomationAsync(automation, parameters, wait).GetAwaiter().GetResult();

    public AutomationResponse GetAutomationDetails(string id, bool wait = false)
        => GetAutomationDetailsAsync(id, wait).GetAwaiter().GetResult();

    public AfipWebService WebService(string service, WebServiceConfig? options = null)
    {
        options ??= new WebServiceConfig();
        options.Service = service;
        options.Generic = true;
        return new AfipWebService(this, options);
    }

    #endregion

    #region Async

    public async Task<GetServiceTAResponse> GetServiceTaAsync(string service, bool force = false)
    {
        var body = new Dictionary<string, object?>
        {
            ["environment"] = Options.Production ? "prod" : "dev",
            ["wsid"] = service,
            ["tax_id"] = Options.CUIT,
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

        return await AdminClient.MakeRequestAsync<GetServiceTAResponse>(HttpMethod.Post, "v1/afip/auth", body);
    }

    public async Task<GetLastRequestXmlResponse> GetLastRequestXmlAsync()
    {
        return await AdminClient.MakeRequestAsync<GetLastRequestXmlResponse>(HttpMethod.Get, "v1/afip/requests/last-xml");
    }

    public async Task<AutomationResponse> CreateAutomationAsync(string automation, Dictionary<string, object?> parameters, bool wait = true)
    {
        var body = new Dictionary<string, object?>
        {
            ["automation"] = automation,
            ["params"] = parameters
        };

        var result = await AdminClient.MakeRequestAsync<AutomationResponse>(HttpMethod.Post, "v1/automations", body);

        if (!wait || string.Equals(result.Status, "complete", StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(result.Id))
        {
            return result;
        }

        return await GetAutomationDetailsAsync(result.Id!, true);
    }

    public async Task<AutomationResponse> GetAutomationDetailsAsync(string id, bool wait = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Automation id is required.", nameof(id));
        }

        int retries = 24;

        while (retries-- >= 0)
        {
            var result = await AdminClient.MakeRequestAsync<AutomationResponse>(HttpMethod.Get, $"v1/automations/{Uri.EscapeDataString(id)}");

            if (!wait || string.Equals(result.Status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        throw new InvalidOperationException("Error: Waiting for too long.");
    }

    #endregion
}