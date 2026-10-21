using System.Text.Json;

namespace AfipSDK.Afip.Net;

/// <summary>
/// Error de la API de Factura SDK.
///
/// Trae el status HTTP y el cuerpo de la respuesta: message, los problemas por campo en
/// DataErrors y las reglas que el comprobante no cumplió en Errors
/// (https://afipsdk.com/docs/facturasdk/errors).
/// </summary>
public class FacturaSDKException : Exception
{
    /// <summary>Status HTTP de la respuesta, o 0 si el error no vino de una respuesta.</summary>
    public int Status { get; }

    /// <summary>Cuerpo de la respuesta, o null si no era JSON.</summary>
    public JsonElement? Body { get; }

    /// <summary>Problemas por campo, anidados como la solicitud, o null.</summary>
    public JsonElement? DataErrors { get; }

    /// <summary>Reglas que no se cumplieron, cada una con code y message, o null.</summary>
    public List<Dictionary<string, object?>>? Errors { get; }

    public FacturaSDKException(string message, int status = 0, JsonElement? body = null) : base(message)
    {
        Status = status;
        Body = body;

        // Una automatización fallida responde { status: "error", data: { message, data_errors, errors } }
        var detail = DetailOf(body);

        if (detail is JsonElement element)
        {
            if (element.TryGetProperty("data_errors", out var dataErrors) && dataErrors.ValueKind == JsonValueKind.Object)
            {
                DataErrors = dataErrors;
            }

            if (element.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                Errors = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(errors.GetRawText());
            }
        }
    }

    /// <summary>Arma la excepción a partir de una respuesta fallida de la API.</summary>
    public static FacturaSDKException FromResponse(int status, string body)
    {
        JsonElement? data = null;

        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                data = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                data = null;
            }
        }

        var parts = new List<string>();

        if (DetailOf(data) is JsonElement detail)
        {
            if (detail.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(message.GetString()))
            {
                parts.Add(message.GetString()!);
            }

            if (detail.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    parts.Add($"{Text(error, "code")}: {Text(error, "message")}");
                }
            }

            if (detail.TryGetProperty("data_errors", out var dataErrors) && dataErrors.ValueKind == JsonValueKind.Object)
            {
                parts.AddRange(FlattenDataErrors(dataErrors, ""));
            }
        }

        string text;

        if (parts.Count > 0)
        {
            text = string.Join(" ", parts);
        }
        else if (data is null && !string.IsNullOrEmpty(body))
        {
            text = body;
        }
        else
        {
            text = $"Factura SDK respondió {status}";
        }

        return new FacturaSDKException(text, status, data);
    }

    /// <summary>
    /// Dónde viven message, data_errors y errors en un cuerpo: en el cuerpo mismo, o en data cuando
    /// el cuerpo es una automatización fallida.
    /// </summary>
    private static JsonElement? DetailOf(JsonElement? data)
    {
        if (data is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String && status.GetString() == "error"
            && element.TryGetProperty("data", out var inner) && inner.ValueKind == JsonValueKind.Object)
        {
            return inner;
        }

        return element;
    }

    /// <summary>{ receiver: { vat_condition: "mensaje" } } -> ["receiver.vat_condition: mensaje"]</summary>
    private static IEnumerable<string> FlattenDataErrors(JsonElement errors, string prefix)
    {
        foreach (var property in errors.EnumerateObject())
        {
            var path = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var line in FlattenDataErrors(property.Value, path))
                {
                    yield return line;
                }
            }
            else
            {
                yield return $"{path}: {Text(property.Value)}";
            }
        }
    }

    private static string Text(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) ? Text(value) : string.Empty;

    private static string Text(JsonElement value)
        => value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
}
