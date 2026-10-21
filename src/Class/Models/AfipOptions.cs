namespace AfipSDK.Afip.Net;

public sealed class AfipOptions
{
    public string? CUIT { get; set; }
    public bool Production { get; set; }
    public string? Cert { get; set; }
    public string? Key { get; set; }
    public string? AccessToken { get; set; }

    /// <summary>
    /// Access token de un proyecto Factura SDK (fsk_dev_… o fsk_prod_…), cuando AccessToken no es uno.
    /// El token decide el ambiente de afip.FacturaSDK.
    /// </summary>
    public string? FacturaSdkAccessToken { get; set; }
}