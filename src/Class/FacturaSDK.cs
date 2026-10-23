using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AfipSDK.Afip.Net;

/// <summary>
/// Factura SDK: emisión de comprobantes de punta a punta.
///
/// Emisores, certificados, comprobantes y webhooks de un proyecto Factura SDK. El access token
/// (fsk_dev_… para el sandbox, fsk_prod_… para producción) decide el ambiente.
///
/// Las entidades se devuelven como Dictionary&lt;string, object?&gt; tal como las responde la API
/// (https://afipsdk.com/docs/facturasdk/), con los valores como JsonElement; las automatizaciones
/// como AutomationResponse.
/// </summary>
public sealed class FacturaSDK
{
    // Las automatizaciones (ARCA con la clave fiscal, verificación) tardan de segundos a un par de
    // minutos; un comprobante se autoriza en segundo plano en segundos
    private const int AutomationIntervalMs = 3000;
    private const int AutomationAttempts = 60;
    private const int InvoiceIntervalMs = 2000;
    private const int InvoiceAttempts = 60;

    private static readonly string[] PendingInvoiceStatuses = { "queued", "processing" };

    private readonly Afip afip;

    /// <summary>Access token enviado en cada solicitud: el AccessToken de Afip.</summary>
    public string? AccessToken { get; }

    /// <summary>Ambiente informado a la API: el del token cuando es un token de Factura SDK, si no el de Production.</summary>
    public string Environment { get; }

    public FacturaSDK(Afip afip)
    {
        this.afip = afip;

        AccessToken = afip.Options.AccessToken;

        if (AccessToken is not null && AccessToken.StartsWith("fsk_prod_", StringComparison.Ordinal))
        {
            Environment = "prod";
        }
        else if (AccessToken is not null && AccessToken.StartsWith("fsk_dev_", StringComparison.Ordinal))
        {
            Environment = "dev";
        }
        else
        {
            Environment = afip.Options.Production ? "prod" : "dev";
        }
    }

    #region Sync

    public Dictionary<string, object?> CreateInvoice(Dictionary<string, object?> data, string? idempotencyKey = null, bool wait = false)
        => CreateInvoiceAsync(data, idempotencyKey, wait).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetInvoice(string id, bool wait = false)
        => GetInvoiceAsync(id, wait).GetAwaiter().GetResult();

    public Dictionary<string, object?> WaitForInvoice(string id)
        => WaitForInvoiceAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetInvoices(Dictionary<string, object?>? parameters = null)
        => GetInvoicesAsync(parameters).GetAwaiter().GetResult();

    public Dictionary<string, object?> CancelInvoice(string id)
        => CancelInvoiceAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> CreateIssuer(Dictionary<string, object?> data)
        => CreateIssuerAsync(data).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetIssuer(string id)
        => GetIssuerAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetIssuers(Dictionary<string, object?>? parameters = null)
        => GetIssuersAsync(parameters).GetAwaiter().GetResult();

    public Dictionary<string, object?> UpdateIssuer(string id, Dictionary<string, object?> data)
        => UpdateIssuerAsync(id, data).GetAwaiter().GetResult();

    public void DeleteIssuer(string id)
        => DeleteIssuerAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> LookupIssuer(string taxId)
        => LookupIssuerAsync(taxId).GetAwaiter().GetResult();

    public AutomationResponse GetSalesPoints(Dictionary<string, object?> data, bool wait = true)
        => GetSalesPointsAsync(data, wait).GetAwaiter().GetResult();

    public AutomationResponse CreateSalesPoint(Dictionary<string, object?> data, bool wait = true)
        => CreateSalesPointAsync(data, wait).GetAwaiter().GetResult();

    public Dictionary<string, object?> CreateIssuerCertificate(string id, Dictionary<string, object?> data, bool wait = true)
        => CreateIssuerCertificateAsync(id, data, wait).GetAwaiter().GetResult();

    public AutomationResponse CreateIssuerDelegation(string id, Dictionary<string, object?> data, bool wait = true)
        => CreateIssuerDelegationAsync(id, data, wait).GetAwaiter().GetResult();

    public AutomationResponse VerifyIssuer(string id, bool wait = true)
        => VerifyIssuerAsync(id, wait).GetAwaiter().GetResult();

    public Dictionary<string, object?> CreateCertificate(Dictionary<string, object?> data, bool wait = true)
        => CreateCertificateAsync(data, wait).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetCertificate(string id)
        => GetCertificateAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetCertificates(Dictionary<string, object?>? parameters = null)
        => GetCertificatesAsync(parameters).GetAwaiter().GetResult();

    public Dictionary<string, object?> RevokeCertificate(string id)
        => RevokeCertificateAsync(id).GetAwaiter().GetResult();

    public AutomationResponse AuthorizeCertificate(string id, Dictionary<string, object?> data, bool wait = true)
        => AuthorizeCertificateAsync(id, data, wait).GetAwaiter().GetResult();

    public Dictionary<string, object?> CreateWebhookEndpoint(Dictionary<string, object?> data)
        => CreateWebhookEndpointAsync(data).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetWebhookEndpoint(string id)
        => GetWebhookEndpointAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetWebhookEndpoints(Dictionary<string, object?>? parameters = null)
        => GetWebhookEndpointsAsync(parameters).GetAwaiter().GetResult();

    public Dictionary<string, object?> UpdateWebhookEndpoint(string id, Dictionary<string, object?> data)
        => UpdateWebhookEndpointAsync(id, data).GetAwaiter().GetResult();

    public Dictionary<string, object?> RotateWebhookEndpointSecret(string id)
        => RotateWebhookEndpointSecretAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> TestWebhookEndpoint(string id)
        => TestWebhookEndpointAsync(id).GetAwaiter().GetResult();

    public Dictionary<string, object?> GetWebhookDeliveries(string id, Dictionary<string, object?>? parameters = null)
        => GetWebhookDeliveriesAsync(id, parameters).GetAwaiter().GetResult();

    public AutomationResponse GetAutomation(string id, bool wait = false)
        => GetAutomationAsync(id, wait).GetAwaiter().GetResult();

    public AutomationResponse WaitForAutomation(string id)
        => WaitForAutomationAsync(id).GetAwaiter().GetResult();

    #endregion

    #region Async

    /// <summary>
    /// Emite un comprobante.
    ///
    /// Por defecto Factura SDK responde enseguida con el comprobante en cola (queued) y lo autoriza
    /// en ARCA en segundo plano: el resultado llega a tu webhook. Con wait consulta el comprobante
    /// hasta que quede decidido; con "sync": true en los datos Factura SDK lo decide dentro de la
    /// solicitud. idempotencyKey identifica la venta en tu sistema: repetirlo devuelve el mismo
    /// comprobante en vez de emitir otro.
    /// </summary>
    public async Task<Dictionary<string, object?>> CreateInvoiceAsync(Dictionary<string, object?> data, string? idempotencyKey = null, bool wait = false)
    {
        Dictionary<string, string>? headers = null;

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            headers = new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey! };
        }

        var invoice = (await RequestAsync(HttpMethod.Post, "invoices", data, null, headers))!;

        if (wait && IsPending(invoice))
        {
            return await WaitForInvoiceAsync(Text(invoice, "id"));
        }

        return invoice;
    }

    /// <summary>Devuelve un comprobante con su estado actual. Con wait, espera hasta que quede approved, rejected o failed.</summary>
    public async Task<Dictionary<string, object?>> GetInvoiceAsync(string id, bool wait = false)
    {
        if (wait)
        {
            return await WaitForInvoiceAsync(id);
        }

        return (await RequestAsync(HttpMethod.Get, $"invoices/{Uri.EscapeDataString(id)}"))!;
    }

    /// <summary>
    /// Espera a que un comprobante quede decidido: approved, rejected o failed. Lo consulta cada 2
    /// segundos durante 2 minutos como máximo; si la espera termina, el comprobante sigue su curso
    /// en Factura SDK y el resultado llega igual a tu webhook.
    /// </summary>
    public async Task<Dictionary<string, object?>> WaitForInvoiceAsync(string id)
    {
        var attempts = InvoiceAttempts;

        while (attempts-- > 0)
        {
            var invoice = (await RequestAsync(HttpMethod.Get, $"invoices/{Uri.EscapeDataString(id)}"))!;

            if (!IsPending(invoice))
            {
                return invoice;
            }

            await Task.Delay(InvoiceIntervalMs);
        }

        throw new InvalidOperationException("Error: Waiting for too long.");
    }

    /// <summary>Lista los comprobantes, del más nuevo al más viejo, con paginación por cursor. Filtros: issuer_cuit, status, invoice_type, start, end, limit, cursor.</summary>
    public async Task<Dictionary<string, object?>> GetInvoicesAsync(Dictionary<string, object?>? parameters = null)
        => (await RequestAsync(HttpMethod.Get, "invoices", null, parameters))!;

    /// <summary>Cancela un comprobante que todavía está en cola. Nunca se emite.</summary>
    public async Task<Dictionary<string, object?>> CancelInvoiceAsync(string id)
        => (await RequestAsync(HttpMethod.Post, $"invoices/{Uri.EscapeDataString(id)}/cancel"))!;

    /// <summary>Crea un emisor: un CUIT que factura. Nace pending y emite cuando se verifica.</summary>
    public async Task<Dictionary<string, object?>> CreateIssuerAsync(Dictionary<string, object?> data)
        => (await RequestAsync(HttpMethod.Post, "issuers", data))!;

    /// <summary>Devuelve un emisor.</summary>
    public async Task<Dictionary<string, object?>> GetIssuerAsync(string id)
        => (await RequestAsync(HttpMethod.Get, $"issuers/{Uri.EscapeDataString(id)}"))!;

    /// <summary>Lista los emisores, con paginación por cursor. Filtros: tax_id, certificate_id, status, limit, cursor.</summary>
    public async Task<Dictionary<string, object?>> GetIssuersAsync(Dictionary<string, object?>? parameters = null)
        => (await RequestAsync(HttpMethod.Get, "issuers", null, parameters))!;

    /// <summary>Modifica un emisor: solo cambian los campos enviados.</summary>
    public async Task<Dictionary<string, object?>> UpdateIssuerAsync(string id, Dictionary<string, object?> data)
        => (await RequestAsync(HttpMethod.Put, $"issuers/{Uri.EscapeDataString(id)}", data))!;

    /// <summary>Borra un emisor. Sus comprobantes quedan.</summary>
    public async Task DeleteIssuerAsync(string id)
        => await RequestAsync(HttpMethod.Delete, $"issuers/{Uri.EscapeDataString(id)}");

    /// <summary>Devuelve los datos de la constancia de inscripción de un CUIT en ARCA, para dar de alta el emisor sin cargarlos a mano.</summary>
    public async Task<Dictionary<string, object?>> LookupIssuerAsync(string taxId)
        => (await RequestAsync(HttpMethod.Get, "issuers/lookup", null, new Dictionary<string, object?> { ["tax_id"] = taxId }))!;

    /// <summary>
    /// Lista los puntos de venta de un CUIT en ARCA, con su clave fiscal (tax_id, username, password).
    /// Es una automatización: con wait la devuelve terminada, con los puntos de venta en Data.
    /// </summary>
    public async Task<AutomationResponse> GetSalesPointsAsync(Dictionary<string, object?> data, bool wait = true)
        => await AutomationAsync(await RequestAsync(HttpMethod.Post, "issuers/sales-points", data), wait);

    /// <summary>
    /// Crea en ARCA un punto de venta de web services para un CUIT, con su clave fiscal (tax_id,
    /// username, password y opcionalmente vat_condition, number, name, system). Es una
    /// automatización: con wait la devuelve terminada, con el punto de venta en Data.
    /// </summary>
    public async Task<AutomationResponse> CreateSalesPointAsync(Dictionary<string, object?> data, bool wait = true)
        => await AutomationAsync(await RequestAsync(HttpMethod.Post, "issuers/sales-points/create", data), wait);

    /// <summary>
    /// Crea un certificado para el emisor y se lo asigna. Subido (cert y key en PEM) responde el
    /// certificado. Creado en ARCA (source "automation", con username y password) es una
    /// automatización: con wait devuelve la automatización terminada (id, status, data con el certificado).
    /// </summary>
    public async Task<Dictionary<string, object?>> CreateIssuerCertificateAsync(string id, Dictionary<string, object?> data, bool wait = true)
        => await AutomationAsDictionaryAsync((await RequestAsync(HttpMethod.Post, $"issuers/{Uri.EscapeDataString(id)}/certificate", data))!, wait);

    /// <summary>
    /// Acepta en ARCA la delegación del emisor al dueño de su certificado y autoriza el certificado
    /// para facturar por él, con la clave fiscal del dueño del certificado (username, password). Es
    /// una automatización: con wait la devuelve terminada.
    /// </summary>
    public async Task<AutomationResponse> CreateIssuerDelegationAsync(string id, Dictionary<string, object?> data, bool wait = true)
        => await AutomationAsync(await RequestAsync(HttpMethod.Post, $"issuers/{Uri.EscapeDataString(id)}/delegation", data), wait);

    /// <summary>
    /// Verifica un emisor: emite una factura de $1 y la nota de crédito que la anula, y el emisor
    /// pasa a active. En producción los dos comprobantes son reales. Es una automatización: con
    /// wait la devuelve terminada, con verified, issuer, invoice y credit_note en Data.
    /// </summary>
    public async Task<AutomationResponse> VerifyIssuerAsync(string id, bool wait = true)
        => await AutomationAsync(await RequestAsync(HttpMethod.Post, $"issuers/{Uri.EscapeDataString(id)}/verify"), wait);

    /// <summary>
    /// Agrega un certificado al proyecto. Subido (cert y key en PEM) responde el certificado. Creado
    /// en ARCA (source "automation", con tax_id, username y password) es una automatización: con
    /// wait devuelve la automatización terminada (id, status, data con el certificado).
    /// </summary>
    public async Task<Dictionary<string, object?>> CreateCertificateAsync(Dictionary<string, object?> data, bool wait = true)
        => await AutomationAsDictionaryAsync((await RequestAsync(HttpMethod.Post, "certificates", data))!, wait);

    /// <summary>Devuelve un certificado.</summary>
    public async Task<Dictionary<string, object?>> GetCertificateAsync(string id)
        => (await RequestAsync(HttpMethod.Get, $"certificates/{Uri.EscapeDataString(id)}"))!;

    /// <summary>Lista los certificados, con paginación por cursor. Filtros: tax_id, status, limit, cursor.</summary>
    public async Task<Dictionary<string, object?>> GetCertificatesAsync(Dictionary<string, object?>? parameters = null)
        => (await RequestAsync(HttpMethod.Get, "certificates", null, parameters))!;

    /// <summary>Revoca un certificado: no firma más.</summary>
    public async Task<Dictionary<string, object?>> RevokeCertificateAsync(string id)
        => (await RequestAsync(HttpMethod.Post, $"certificates/{Uri.EscapeDataString(id)}/revoke"))!;

    /// <summary>
    /// Autoriza en ARCA un certificado para el web service de facturación (wsfe), con la clave
    /// fiscal de su dueño (password y opcionalmente username). Es una automatización: con wait la
    /// devuelve terminada.
    /// </summary>
    public async Task<AutomationResponse> AuthorizeCertificateAsync(string id, Dictionary<string, object?> data, bool wait = true)
        => await AutomationAsync(await RequestAsync(HttpMethod.Post, $"certificates/{Uri.EscapeDataString(id)}/authorize", data), wait);

    /// <summary>
    /// Crea el endpoint de webhook (url): la URL a la que Factura SDK le manda el resultado de cada
    /// comprobante. La respuesta trae el secret, que se muestra solo esta vez.
    /// </summary>
    public async Task<Dictionary<string, object?>> CreateWebhookEndpointAsync(Dictionary<string, object?> data)
        => (await RequestAsync(HttpMethod.Post, "webhook-endpoints", data))!;

    /// <summary>Devuelve un endpoint de webhook.</summary>
    public async Task<Dictionary<string, object?>> GetWebhookEndpointAsync(string id)
        => (await RequestAsync(HttpMethod.Get, $"webhook-endpoints/{Uri.EscapeDataString(id)}"))!;

    /// <summary>Lista los endpoints de webhook. Filtros: status.</summary>
    public async Task<Dictionary<string, object?>> GetWebhookEndpointsAsync(Dictionary<string, object?>? parameters = null)
        => (await RequestAsync(HttpMethod.Get, "webhook-endpoints", null, parameters))!;

    /// <summary>Modifica un endpoint de webhook: url o status (active o inactive).</summary>
    public async Task<Dictionary<string, object?>> UpdateWebhookEndpointAsync(string id, Dictionary<string, object?> data)
        => (await RequestAsync(HttpMethod.Put, $"webhook-endpoints/{Uri.EscapeDataString(id)}", data))!;

    /// <summary>Genera un secret nuevo para un endpoint de webhook. Desde entonces cada entrega se firma con él.</summary>
    public async Task<Dictionary<string, object?>> RotateWebhookEndpointSecretAsync(string id)
        => (await RequestAsync(HttpMethod.Post, $"webhook-endpoints/{Uri.EscapeDataString(id)}/rotate"))!;

    /// <summary>Manda un evento de prueba firmado a un endpoint de webhook.</summary>
    public async Task<Dictionary<string, object?>> TestWebhookEndpointAsync(string id)
        => (await RequestAsync(HttpMethod.Post, $"webhook-endpoints/{Uri.EscapeDataString(id)}/test"))!;

    /// <summary>Lista las entregas de un endpoint de webhook: qué se mandó, cuándo y con qué resultado. Filtros: status, invoice_id, limit, cursor.</summary>
    public async Task<Dictionary<string, object?>> GetWebhookDeliveriesAsync(string id, Dictionary<string, object?>? parameters = null)
        => (await RequestAsync(HttpMethod.Get, $"webhook-endpoints/{Uri.EscapeDataString(id)}/deliveries", null, parameters))!;

    /// <summary>Devuelve una automatización. Con wait, espera hasta que termine. Lanza FacturaSDKException si terminó con error.</summary>
    public async Task<AutomationResponse> GetAutomationAsync(string id, bool wait = false)
    {
        if (wait)
        {
            return await WaitForAutomationAsync(id);
        }

        return ToAutomation((await RequestAsync(HttpMethod.Get, $"automations/{Uri.EscapeDataString(id)}"))!);
    }

    /// <summary>Espera a que termine una automatización. La consulta cada 3 segundos durante 3 minutos como máximo. Lanza FacturaSDKException si terminó con error.</summary>
    public async Task<AutomationResponse> WaitForAutomationAsync(string id)
    {
        var attempts = AutomationAttempts;

        while (attempts-- > 0)
        {
            var automation = (await RequestAsync(HttpMethod.Get, $"automations/{Uri.EscapeDataString(id)}"))!;

            if (Text(automation, "status") != "in_process")
            {
                return ToAutomation(automation);
            }

            await Task.Delay(AutomationIntervalMs);
        }

        throw new InvalidOperationException("Error: Waiting for too long.");
    }

    #endregion

    #region Webhooks

    /// <summary>
    /// Verifica la firma de una entrega de webhook.
    ///
    /// El header X-FacturaSDK-Signature es "t=&lt;timestamp&gt;,v1=&lt;firma&gt;", donde v1 es el
    /// HMAC-SHA256 de "&lt;timestamp&gt;.&lt;cuerpo&gt;" con el secret del endpoint. El cuerpo tiene
    /// que ser el crudo recibido, byte a byte. tolerance son los segundos que el timestamp puede
    /// diferir de la hora actual.
    /// </summary>
    public bool VerifyWebhookSignature(string? signature, string? body, string? secret, int tolerance = 300)
        => VerifyWebhookSignature(signature, body is null ? null : Encoding.UTF8.GetBytes(body), secret, tolerance);

    /// <summary>Verifica la firma de una entrega de webhook a partir del cuerpo en bytes.</summary>
    public bool VerifyWebhookSignature(string? signature, byte[]? body, string? secret, int tolerance = 300)
    {
        if (signature is null || body is null || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        var parts = new Dictionary<string, string>();

        foreach (var part in signature.Split(','))
        {
            var pair = part.Split(new[] { '=' }, 2);
            parts[pair[0]] = pair.Length > 1 ? pair[1] : string.Empty;
        }

        if (!parts.TryGetValue("t", out var t) || !long.TryParse(t, out var timestamp) || timestamp < 0)
        {
            return false;
        }

        var received = parts.TryGetValue("v1", out var v1) ? v1 : string.Empty;

        string expected;

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var prefix = Encoding.UTF8.GetBytes($"{timestamp}.");
            var payload = new byte[prefix.Length + body.Length];
            Buffer.BlockCopy(prefix, 0, payload, 0, prefix.Length);
            Buffer.BlockCopy(body, 0, payload, prefix.Length, body.Length);
            expected = BitConverter.ToString(hmac.ComputeHash(payload)).Replace("-", string.Empty).ToLowerInvariant();
        }

        var fresh = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp) <= tolerance;

        return fresh && SecureEquals(expected, received);
    }

    /// <summary>
    /// Verifica la firma de una entrega de webhook y devuelve el evento (event_id, type, created, data).
    /// Lanza FacturaSDKException si la firma no es válida.
    /// </summary>
    public Dictionary<string, object?> ConstructWebhookEvent(string body, string? signature, string? secret, int tolerance = 300)
    {
        if (!VerifyWebhookSignature(signature, body, secret, tolerance))
        {
            throw new FacturaSDKException("La firma del webhook no es válida");
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(body)!;
    }

    #endregion

    /// <summary>
    /// Resuelve la respuesta de un endpoint que inicia una automatización (202 con automation_id):
    /// con wait la espera terminada; si no, devuelve la automatización recién iniciada.
    /// </summary>
    private async Task<AutomationResponse> AutomationAsync(Dictionary<string, object?>? result, bool wait)
    {
        var automationId = result is null ? string.Empty : Text(result, "automation_id");

        if (string.IsNullOrEmpty(automationId))
        {
            return ToAutomation(result ?? []);
        }

        if (wait)
        {
            return await WaitForAutomationAsync(automationId);
        }

        return new AutomationResponse { Id = automationId, Status = Text(result!, "status") };
    }

    /// <summary>
    /// Igual que AutomationAsync para endpoints que pueden responder una entidad (un certificado
    /// subido) o iniciar una automatización: la automatización se devuelve como diccionario.
    /// </summary>
    private async Task<Dictionary<string, object?>> AutomationAsDictionaryAsync(Dictionary<string, object?> result, bool wait)
    {
        var automationId = Text(result, "automation_id");

        if (wait && !string.IsNullOrEmpty(automationId))
        {
            var automation = await WaitForAutomationAsync(automationId);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(automation, JsonOptions))!;
        }

        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static AutomationResponse ToAutomation(Dictionary<string, object?> automation)
        => JsonSerializer.Deserialize<AutomationResponse>(JsonSerializer.Serialize(automation), JsonOptions)!;

    private static bool IsPending(Dictionary<string, object?> invoice)
        => Array.IndexOf(PendingInvoiceStatuses, Text(invoice, "status")) >= 0;

    private static string Text(Dictionary<string, object?> dictionary, string key)
    {
        if (!dictionary.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return value is JsonElement element
            ? (element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.ToString())
            : value.ToString() ?? string.Empty;
    }

    private static bool SecureEquals(string expected, string received)
    {
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(received);

        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;

        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    private static string Query(Dictionary<string, object?>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return string.Empty;
        }

        var pairs = new List<string>();

        foreach (var parameter in parameters)
        {
            if (parameter.Value is null) continue;
            pairs.Add($"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value.ToString() ?? string.Empty)}");
        }

        return pairs.Count == 0 ? string.Empty : "?" + string.Join("&", pairs);
    }

    /// <summary>Ejecuta una request a la API de Factura SDK. Lanza FacturaSDKException si la API responde un error.</summary>
    private async Task<Dictionary<string, object?>?> RequestAsync(
        HttpMethod method,
        string path,
        Dictionary<string, object?>? body = null,
        Dictionary<string, object?>? parameters = null,
        Dictionary<string, string>? headers = null)
    {
        var response = await afip.AdminClient.SendAsync(method, $"v1/{path}{Query(parameters)}", body, headers, AccessToken, Environment);

        if (response.Status >= 400)
        {
            throw FacturaSDKException.FromResponse(response.Status, response.Data);
        }

        return string.IsNullOrEmpty(response.Data)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(response.Data);
    }
}
