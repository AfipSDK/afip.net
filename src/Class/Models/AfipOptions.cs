namespace AfipSDK.Afip.Net;

public sealed class AfipOptions
{
    public string? CUIT { get; set; }
    public bool Production { get; set; }
    public string? Cert { get; set; }
    public string? Key { get; set; }
    public string? AccessToken { get; set; }
}