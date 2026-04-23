using System.Text.Json.Serialization;

namespace AfipSDK.Afip.Net;

public class CreatePDFResponse
{
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;
}