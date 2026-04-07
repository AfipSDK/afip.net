using System.Text.Json;

namespace Afip.Net;

public sealed class RegisterScopeFour : AfipWebService
{
    public RegisterScopeFour(Afip afip)
        : base(afip, "ws_sr_padron_a4", new Dictionary<string, object?>
        {
            ["soapV1_2"] = false,
            ["WSDL"] = "ws_sr_padron_a4-production.wsdl",
            ["URL"] = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA4",
            ["WSDL_TEST"] = "ws_sr_padron_a4.wsdl",
            ["URL_TEST"] = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4"
        })
    {
    }

    public Task<JsonDocument> GetServerStatusAsync()
        => ExecuteRequestAsync("dummy");

    public Task<JsonDocument> GetTaxpayerDetailsAsync(object identifier)
        => ExecuteRequestAsync("getPersona", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, idPersona = identifier });

    public Task<JsonDocument> GetTaxpayersDetailsAsync(object identifiers)
        => ExecuteRequestAsync("getPersonaList_v2", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, idPersona = identifiers });
}
