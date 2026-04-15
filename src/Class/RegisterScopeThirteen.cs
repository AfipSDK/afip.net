using System.Text.Json;

namespace Afip.Net;

public sealed class RegisterScopeThirteen : AfipWebService
{
    protected override string WSDL => "ws_sr_padron_a13-production.wsdl";
    protected override string URL => "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA13";
    protected override string WSDL_TEST => "ws_sr_padron_a13.wsdl";
    protected override string URL_TEST => "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13";

    public RegisterScopeThirteen(Afip afip) : base(afip, new WebServiceConfig { Service = "ws_sr_padron_a13" }) { }

    #region Sync

    public Dictionary<string, object?> GetServerStatus()
        => GetServerStatusAsync().GetAwaiter().GetResult();

    public Dictionary<string, object?>? GetTaxpayerDetails(long identifier)
        => GetTaxpayerDetailsAsync(identifier).GetAwaiter().GetResult();

    public object? GetTaxIDByDocument(long documentNumber)
        => GetTaxIDByDocumentAsync(documentNumber).GetAwaiter().GetResult();

    #endregion

    #region Async

    public async Task<Dictionary<string, object?>> GetServerStatusAsync()
        => await ExecuteAfipAsync("dummy");

    public async Task<Dictionary<string, object?>?> GetTaxpayerDetailsAsync(long identifier)
    {
        try
        {
            var result = await ExecuteAfipAsync("getPersona", new Dictionary<string, object?> { ["idPersona"] = identifier });

            if (!result.TryGetValue("persona", out var val) || val is not JsonElement el) return null;
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(el.GetRawText());
        }
        catch (Exception ex) when (ex.Message.Contains("No existe"))
        {
            return null;
        }
    }

    public async Task<object?> GetTaxIDByDocumentAsync(long documentNumber)
    {
        try
        {
            var result = await ExecuteAfipAsync("getIdPersonaListByDocumento", new Dictionary<string, object?> { ["documento"] = documentNumber });

            if (!result.TryGetValue("idPersona", out var val) || val is not JsonElement el) return null;
            return JsonSerializer.Deserialize<object?>(el.GetRawText());
        }
        catch (Exception ex) when (ex.Message.Contains("No existe"))
        {
            return null;
        }
    }

    #endregion

    private async Task<Dictionary<string, object?>> ExecuteAfipAsync(string operation, Dictionary<string, object?>? extraParams = null)
    {
        Dictionary<string, object?> parameters = [];

        if (!string.Equals(operation, "dummy", StringComparison.OrdinalIgnoreCase))
        {
            var ta = await afip.GetServiceTaAsync("ws_sr_padron_a13");
            parameters["token"] = ta.Token;
            parameters["sign"] = ta.Sign;
            parameters["cuitRepresentada"] = afip.Options.CUIT;

            if (extraParams != null)
                foreach (var kv in extraParams)
                    parameters[kv.Key] = kv.Value;
        }

        var result = await ExecuteRequestAsync<Dictionary<string, object?>>(operation, parameters);

        var key = operation switch
        {
            "getPersona" => "personaReturn",
            "getIdPersonaListByDocumento" => "idPersonaListReturn",
            _ => "return"
        };

        if (!result.TryGetValue(key, out var val) || val is not JsonElement el)
            return [];

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(el.GetRawText()) ?? [];
    }
}
