using System.Text.Json;

namespace Afip.Net;

public sealed class ElectronicBilling : AfipWebService
{
    public ElectronicBilling(Afip afip)
        : base(afip, "wsfe", new Dictionary<string, object?>
        {
            ["soapV1_2"] = true,
            ["WSDL"] = "wsfe-production.wsdl",
            ["URL"] = "https://servicios1.afip.gov.ar/wsfev1/service.asmx",
            ["WSDL_TEST"] = "wsfe.wsdl",
            ["URL_TEST"] = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx"
        })
    {
    }

    public Task<JsonDocument> GetServerStatusAsync()
        => ExecuteRequestAsync("FEDummy");

    public Task<JsonDocument> GetLastVoucherAsync(int salesPoint, int type)
        => ExecuteRequestAsync("FECompUltimoAutorizado", new { PtoVta = salesPoint, CbteTipo = type });

    public Task<JsonDocument> CreateVoucherAsync(object data, bool returnResponse = false)
        => ExecuteRequestAsync("FECAESolicitar", new { FeCAEReq = data, ReturnResponse = returnResponse });

    public Task<JsonDocument> GetVoucherInfoAsync(int number, int salesPoint, int type)
        => ExecuteRequestAsync("FECompConsultar", new { FeCompConsReq = new { CbteNro = number, PtoVta = salesPoint, CbteTipo = type } });
}
