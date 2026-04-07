using System.Text.Json;

namespace Afip.Net;

public sealed class RegisterScopeThirteen : AfipWebService
{
    public RegisterScopeThirteen(Afip afip)
        : base(afip, "ws_sr_padron_a13", new Dictionary<string, object?>
        {
            ["soapV1_2"] = false,
            ["WSDL"] = "ws_sr_padron_a13-production.wsdl",
            ["URL"] = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA13",
            ["WSDL_TEST"] = "ws_sr_padron_a13.wsdl",
            ["URL_TEST"] = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13"
        })
    {
    }

    public Task<JsonDocument> GetServerStatusAsync()
        => ExecuteRequestAsync("dummy");

    public Task<JsonDocument> GetTaxpayerDetailsAsync(object identifier)
        => ExecuteRequestAsync("getPersona", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, idPersona = identifier });

    public Task<JsonDocument> GetTaxIDByDocumentAsync(object documentNumber)
        => ExecuteRequestAsync("getIdPersonaListByDocumento", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, documento = documentNumber });
}
