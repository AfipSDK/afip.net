//dotnet run --project examples/CreateVoucher

using System.Text.Json;
using Afip.Net;
using AfipClient = Afip.Net.Afip;

var options = new AfipOptions
{
    CUIT = "TU_CUIT",
    Production = false,
    AccessToken = "TU_ACCESS_TOKEN",
};

var afip = new AfipClient(options);

// Punto de venta y tipo de factura
var puntoDeVenta = 1;
var tipoDeFactura = 6; // 6 = Factura B

// Obtener el último número de factura y calcular el siguiente
var lastVoucher = await afip.ElectronicBilling.GetLastVoucherAsync(puntoDeVenta, tipoDeFactura);
var numeroDeFactura = lastVoucher + 1;

// Concepto: 1 = Productos, 2 = Servicios, 3 = Productos y Servicios
var concepto = 1;

// Tipo de documento: 80 = CUIT, 86 = CUIL, 96 = DNI, 99 = Consumidor Final
var tipoDeDocumento = 99;
var numeroDeDocumento = 0;

// Fecha en formato yyyyMMdd (equivalente al Date.now() con offset de timezone del JS)
var fecha = DateTime.UtcNow.ToString("yyyyMMdd");

// Importes
var importeGravado = 100m;
var importeExentoIva = 0m;
var importeIva = 21m;

// Condición IVA receptor: 5 = Consumidor Final
var condicionIvaReceptor = 5;

// Fechas de servicio (solo obligatorias para concepto 2 o 3)
object? fechaServicioDesde = null;
object? fechaServicioHasta = null;
object? fechaVencimientoPago = null;

if (concepto == 2 || concepto == 3)
{
    fechaServicioDesde = 20191213;
    fechaServicioHasta = 20191213;
    fechaVencimientoPago = 20191213;
}

var data = new Dictionary<string, object?>
{
    ["CantReg"] = 1,
    ["PtoVta"] = puntoDeVenta,
    ["CbteTipo"] = tipoDeFactura,
    ["Concepto"] = concepto,
    ["DocTipo"] = tipoDeDocumento,
    ["DocNro"] = numeroDeDocumento,
    ["CbteDesde"] = numeroDeFactura,
    ["CbteHasta"] = numeroDeFactura,
    ["CbteFch"] = int.Parse(fecha),
    ["FchServDesde"] = fechaServicioDesde,
    ["FchServHasta"] = fechaServicioHasta,
    ["FchVtoPago"] = fechaVencimientoPago,
    ["ImpTotal"] = importeGravado + importeIva + importeExentoIva,
    ["ImpTotConc"] = 0,
    ["ImpNeto"] = importeGravado,
    ["ImpOpEx"] = importeExentoIva,
    ["ImpIVA"] = importeIva,
    ["ImpTrib"] = 0,
    ["MonId"] = "PES",
    ["MonCotiz"] = 1,
    ["CondicionIVAReceptorId"] = condicionIvaReceptor,
    ["Iva"] = new[]
    {
        new Dictionary<string, object?>
        {
            ["Id"] = 5, // 5 = 21%
            ["BaseImp"] = importeGravado,
            ["Importe"] = importeIva
        }
    }
};

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

var response = await afip.ElectronicBilling.CreateVoucherAsync(data);
Console.WriteLine(JsonSerializer.Serialize(new
{
    cae = response["CAE"],
    vencimiento = response["CAEFchVto"]
}, jsonOptions));
