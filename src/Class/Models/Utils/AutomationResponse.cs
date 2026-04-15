namespace Afip.Net;

public class AutomationResponse
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
}
