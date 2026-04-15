namespace Afip.Net;

public sealed class WebServiceConfig
{
    public string WSDL { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string WSDL_TEST { get; set; } = string.Empty;
    public string URL_TEST { get; set; } = string.Empty;
    public bool Generic { get; set; }
    public string Service { get; set; } = string.Empty;

    public bool SoapV1_2 {get; set;}
}
