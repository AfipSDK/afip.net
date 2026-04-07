namespace Afip.Net;

public sealed class AfipOptions
{
    public AfipOptions()
    {
        ApiBaseUrl = "https://app.afipsdk.com/api/";
        Production = false;
    }

    public string? Cuit { get; set; }
    public bool Production { get; set; }
    public string? Cert { get; set; }
    public string? Key { get; set; }
    public string? AccessToken { get; set; }
    public string ApiBaseUrl { get; set; }
}
