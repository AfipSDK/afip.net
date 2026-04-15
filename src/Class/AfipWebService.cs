namespace Afip.Net;

public class AfipWebService
{
    protected readonly Afip afip;
    private readonly WebServiceConfig _options;

    protected virtual bool SoapV12 => false;
    protected virtual string WSDL => string.Empty;
    protected virtual string URL => string.Empty;
    protected virtual string WSDL_TEST => string.Empty;
    protected virtual string URL_TEST => string.Empty;

    public AfipWebService(Afip afip, WebServiceConfig options)
    {
        this.afip = afip;
        _options = options;

        if (options.Generic)
        {
            if (string.IsNullOrEmpty(options.Service))
                throw new ArgumentNullException(nameof(options.Service), "service field is required in options");

            if (!options.SoapV1_2)
                options.SoapV1_2 = true;
        }
    }

    public GetServiceTAResponse GetTokenAuthorization(bool force = false)
        => GetTokenAuthorizationAsync(force).GetAwaiter().GetResult();

    public Dictionary<string, object?> ExecuteRequest(string method, Dictionary<string, object?>? parameters = null)
        => ExecuteRequestAsync<Dictionary<string, object?>>(method, parameters).GetAwaiter().GetResult();

    public Task<GetServiceTAResponse> GetTokenAuthorizationAsync(bool force = false)
        => afip.GetServiceTaAsync(_options.Service, force);

    public async Task<T> ExecuteRequestAsync<T>(string method, Dictionary<string, object?>? parameters = null)
    {
        var wsdl = !string.IsNullOrEmpty(_options.WSDL) ? _options.WSDL : WSDL;
        var url = !string.IsNullOrEmpty(_options.URL) ? _options.URL : URL;
        var wsdlTest = !string.IsNullOrEmpty(_options.WSDL_TEST) ? _options.WSDL_TEST : WSDL_TEST;
        var urlTest = !string.IsNullOrEmpty(_options.URL_TEST) ? _options.URL_TEST : URL_TEST;
        var soapV12 = _options.SoapV1_2 || SoapV12;

        var requestData = new Dictionary<string, object?>
        {
            ["method"] = method,
            ["params"] = parameters,
            ["environment"] = afip.Options.Production ? "prod" : "dev",
            ["wsid"] = _options.Service,
            ["url"] = afip.Options.Production ? url : urlTest,
            ["wsdl"] = afip.Options.Production ? wsdl : wsdlTest,
            ["soap_v_1_2"] = soapV12
        };

        return await afip.AdminClient.MakeRequestAsync<T>(HttpMethod.Post, "v1/afip/requests", requestData);
    }
}
