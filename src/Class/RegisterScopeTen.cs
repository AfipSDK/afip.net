using System.Text.Json;

namespace Afip.Net;

public sealed class RegisterScopeTen : AfipWebService
{
    public RegisterScopeTen(Afip afip)
        : base(afip, "ws_sr_padron_a10", new Dictionary<string, object?>
        {
            ["soapV1_2"] = false,
            ["WSDL"] = "ws_sr_padron_a10-production.wsdl",
            ["URL"] = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA10",
            ["WSDL_TEST"] = "ws_sr_padron_a10.wsdl",
            ["URL_TEST"] = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA10"
        })
    {
    }

    public Task<JsonDocument> GetServerStatusAsync()
        => ExecuteRequestAsync("dummy");

    public Task<JsonDocument> GetTaxpayerDetailsAsync(object identifier)
        => ExecuteRequestAsync("getPersona", new { token = "", sign = "", cuitRepresentada = Afip.Options.Cuit, idPersona = identifier });
}
