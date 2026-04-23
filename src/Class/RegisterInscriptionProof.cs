using System.Text.Json;

namespace AfipSDK.Afip.Net;

public sealed class RegisterInscriptionProof : AfipWebService
{
    protected override string WSDL => "ws_sr_padron_a5-production.wsdl";
    protected override string URL => "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5";
    protected override string WSDL_TEST => "ws_sr_padron_a5.wsdl";
    protected override string URL_TEST => "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5";

    public RegisterInscriptionProof(Afip afip) : base(afip, new WebServiceConfig { Service = "ws_sr_constancia_inscripcion" }) { }

    #region Sync

    public Dictionary<string, object?> GetServerStatus()
        => GetServerStatusAsync().GetAwaiter().GetResult();

    public Dictionary<string, object?>? GetTaxpayerDetails(long identifier)
        => GetTaxpayerDetailsAsync(identifier).GetAwaiter().GetResult();

    public List<Dictionary<string, object?>>? GetTaxpayersDetails(long[] identifiers)
        => GetTaxpayersDetailsAsync(identifiers).GetAwaiter().GetResult();

    #endregion

    #region Async

    public async Task<Dictionary<string, object?>> GetServerStatusAsync()
        => await ExecuteAfipAsync("dummy");

    public async Task<Dictionary<string, object?>?> GetTaxpayerDetailsAsync(long identifier)
    {
        try
        {
            return await ExecuteAfipAsync("getPersona_v2", identifier);
        }
        catch (Exception ex) when (ex.Message.Contains("No existe"))
        {
            return null;
        }
    }

    public async Task<List<Dictionary<string, object?>>?> GetTaxpayersDetailsAsync(long[] identifiers)
    {
        var result = await ExecuteAfipAsync("getPersonaList_v2", identifiers);

        if (!result.TryGetValue("persona", out var val) || val is not JsonElement el) return null;
        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(el.GetRawText());
    }

    #endregion

    private async Task<Dictionary<string, object?>> ExecuteAfipAsync(string operation, object? identifier = null)
    {
        Dictionary<string, object?> parameters = [];

        if (!string.Equals(operation, "dummy", StringComparison.OrdinalIgnoreCase))
        {
            var ta = await afip.GetServiceTaAsync("ws_sr_constancia_inscripcion");
            parameters["token"] = ta.Token;
            parameters["sign"] = ta.Sign;
            parameters["cuitRepresentada"] = afip.Options.CUIT;

            if (identifier != null)
                parameters["idPersona"] = identifier;
        }

        var result = await ExecuteRequestAsync<Dictionary<string, object?>>(operation, parameters);

        var key = operation switch
        {
            "getPersona_v2" => "personaReturn",
            "getPersonaList_v2" => "personaListReturn",
            _ => "return"
        };

        if (!result.TryGetValue(key, out var val) || val is not JsonElement el)
            return [];

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(el.GetRawText()) ?? [];
    }
}
