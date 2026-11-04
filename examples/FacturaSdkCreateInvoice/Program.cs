//dotnet run --project examples/FacturaSdkCreateInvoice

using AfipSDK.Afip.Net;

// El access token de Factura SDK decide el ambiente: fsk_dev_… es el sandbox, fsk_prod_… es producción
var afip = new Afip(new AfipOptions
{
    AccessToken = "fsk_dev_…",
});

// Factura B a un consumidor final por un producto de $1.210 con IVA 21% incluido.
// El emisor se indica por su CUIT y tiene que estar verificado (ver https://afipsdk.com/docs/facturasdk/getting-started)
var data = new Dictionary<string, object?>
{
    ["issuer_cuit"] = "20111111112", // CUIT del emisor
    ["invoice_type"] = 6, // Tipo de comprobante (6: Factura B)
    ["concept"] = 1, // Concepto: (1) Productos, (2) Servicios, (3) Productos y Servicios
    ["receiver"] = new Dictionary<string, object?>
    {
        ["doc_type"] = 96, // Tipo de documento del receptor (80: CUIT, 96: DNI, 99: sin identificar)
        ["doc_number"] = "12345678", // Número de documento
        ["vat_condition"] = 5, // Condición frente al IVA (5: Consumidor Final)
        ["name"] = "Juan Pérez",
        ["address"] = "Calle Falsa 123, CABA",
        ["email"] = "juan@example.com" // (Opcional) Factura SDK le manda el PDF al aprobarse
    },
    ["amounts"] = new Dictionary<string, object?>
    {
        ["net"] = 1000, // Importe neto gravado
        ["vat"] = 210, // IVA
        ["total"] = 1210 // Importe total
    },
    ["vat_items"] = new[] // Alícuotas de IVA (5: 21%)
    {
        new Dictionary<string, object?> { ["id"] = 5, ["base"] = 1000, ["amount"] = 210 }
    },
    ["items"] = new[] // Detalle impreso en el comprobante
    {
        new Dictionary<string, object?> { ["description"] = "Teclado inalámbrico", ["quantity"] = 1, ["unit_price"] = 1210, ["subtotal"] = 1210 }
    },
    ["sale_condition"] = "Contado"
};

// Factura SDK responde enseguida con el comprobante en cola (queued) y lo autoriza en ARCA en
// segundo plano: el resultado llega a tu webhook. idempotencyKey identifica la venta en tu
// sistema: si repetís la solicitud con la misma clave, recibís el mismo comprobante
var invoice = await afip.FacturaSDK.CreateInvoiceAsync(data, idempotencyKey: "venta-0001");

Console.WriteLine($"{invoice["id"]} {invoice["status"]}"); // queued

// Si necesitás el resultado acá, esperalo: wait consulta el comprobante hasta que sea final
var decided = await afip.FacturaSDK.GetInvoiceAsync(invoice["id"]!.ToString()!, wait: true);

Console.WriteLine($"{decided["status"]} {decided["cae"]} {decided["pdf_url"]}"); // approved, CAE y URL pública del PDF
