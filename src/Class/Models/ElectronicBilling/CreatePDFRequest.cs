namespace Afip.Net;

public sealed class CreatePDFRequest
{
    public string FileName { get; set; } = string.Empty;
    public string? SendTo { get; set; }
    public Dictionary<string, object> Template { get; set; } = [];
}