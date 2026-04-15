using System.Text.Json;
using Afip.Net;
using AfipClient = Afip.Net.Afip;

var options = new AfipOptions
{
    CUIT = "20111111112",
    Production = false,
    AccessToken = "YOUR_ACCESS_TOKEN"
};

var afip = new AfipClient(options);

var date = DateTime.UtcNow.ToString("yyyyMMdd");
var data = new Dictionary<string, object?>
{
    ["CantReg"] = 1,
    ["PtoVta"] = 1,
    ["CbteTipo"] = 6,
    ["Concepto"] = 1,
    ["DocTipo"] = 80,
    ["DocNro"] = 20111111112,
    ["CbteDesde"] = 1,
    ["CbteHasta"] = 1,
    ["CbteFch"] = date,
    ["ImpTotal"] = 184.05,
    ["ImpTotConc"] = 0,
    ["ImpNeto"] = 150,
    ["ImpOpEx"] = 0,
    ["ImpIVA"] = 26.25,
    ["ImpTrib"] = 7.8,
    ["FchServDesde"] = null,
    ["FchServHasta"] = null,
    ["FchVtoPago"] = null,
    ["MonId"] = "PES",
    ["MonCotiz"] = 1,
    ["CbtesAsoc"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Tipo"] = 6,
            ["PtoVta"] = 1,
            ["Nro"] = 1,
            ["Cuit"] = 20111111112
        }
    },
    ["Tributos"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Id"] = 99,
            ["Desc"] = "Ingresos Brutos",
            ["BaseImp"] = 150,
            ["Alic"] = 5.2,
            ["Importe"] = 7.8
        }
    },
    ["Iva"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Id"] = 5,
            ["BaseImp"] = 100,
            ["Importe"] = 21
        }
    },
    ["Opcionales"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Id"] = 17,
            ["Valor"] = "2"
        }
    },
    ["Compradores"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["DocTipo"] = 80,
            ["DocNro"] = 20111111112,
            ["Porcentaje"] = 100
        }
    }
};

var response = await afip.ElectronicBilling.CreateVoucherAsync(data);
Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
