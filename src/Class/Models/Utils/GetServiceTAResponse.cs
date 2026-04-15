namespace Afip.Net;

public class GetServiceTAResponse
{
    public DateTime Expiration { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
}
