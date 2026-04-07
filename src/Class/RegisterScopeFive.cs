using System.Text.Json;

namespace Afip.Net;

public sealed class RegisterScopeFive : AfipWebService
{
    public RegisterScopeFive(Afip afip)
        : base(afip, "ws_sr_padron_a5", new Dictionary<string, object?>
        {
            ["soapV1_2"] = false,
            ["WSDL"] = "ws_sr_padron_a5-production.wsdl",
            ["URL"] = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5",
            ["WSDL_TEST"] = "ws_sr_padron_a5.wsdl",
            ["URL_TEST"] = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5"
        })
    {
    }

    public Task<JsonDocument> GetServerStatusAsync()
        => ExecuteRequestAsync("dummy");

    public Task<JsonDocument> GetTaxpayerDetailsAsync(object identifier)
        => ExecuteRequestAsync("getPersona_v2", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, idPersona = identifier });

    public Task<JsonDocument> GetTaxpayersDetailsAsync(object identifiers)
        => ExecuteRequestAsync("getPersonaList_v2", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, idPersona = identifiers });
}
