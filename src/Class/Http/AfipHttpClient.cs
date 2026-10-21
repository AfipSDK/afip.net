using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AfipSDK.Afip.Net;

internal sealed class AfipHttpClient
{
    private readonly string SdkVersionNumber;
    private readonly string ApiBaseUrl;

    private readonly HttpClient _httpClient;
    private readonly AfipOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AfipHttpClient(AfipOptions options, HttpClient httpClient, string sdkVersionNumber, string apiBaseUrl)
    {
        SdkVersionNumber = sdkVersionNumber;
        ApiBaseUrl = apiBaseUrl;
        _options = options;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Ejecuta una request con headers adicionales y, opcionalmente, otro token y ambiente, y
    /// devuelve la respuesta tal cual, sin lanzar excepción por el status HTTP.
    /// </summary>
    public async Task<AfipHttpResponse> SendAsync(
        HttpMethod method,
        string requestUri,
        Dictionary<string, object?>? body = null,
        Dictionary<string, string>? headers = null,
        string? accessToken = null,
        string? environment = null)
    {
        using var request = CreateRequest(method, requestUri, accessToken, environment);

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var requestResponse = await _httpClient.SendAsync(request);

        return new AfipHttpResponse(
            status: (int)requestResponse.StatusCode,
            statusText: requestResponse.ReasonPhrase ?? string.Empty,
            data: await requestResponse.Content.ReadAsStringAsync()
        );
    }

    public async Task<T> MakeRequestAsync<T>(HttpMethod method, string requestUri, Dictionary<string, object?>? body = null)
    {
        using var request = CreateRequest(method, requestUri);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var requestResponse = await _httpClient.SendAsync(request);
        if (!requestResponse.IsSuccessStatusCode)
        {
            var errorBody = await requestResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)requestResponse.StatusCode} ({requestResponse.ReasonPhrase}). Body: {errorBody}"
            );
        }

        var response = new AfipHttpResponse(
            status: (int)requestResponse.StatusCode,
            statusText: requestResponse.ReasonPhrase ?? string.Empty,
            data: await requestResponse.Content.ReadAsStringAsync()
        );

        return JsonSerializer.Deserialize<T>(response.Data, JsonOptions)!;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string? accessToken = null, string? environment = null)
    {
        var request = new HttpRequestMessage(method, $"{ApiBaseUrl}{path}");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("Afip.Net/1.0");
        request.Headers.Add("sdk-version-number", SdkVersionNumber);
        request.Headers.Add("sdk-library", "dotnet");
        request.Headers.Add("sdk-environment", environment ?? (_options.Production ? "prod" : "dev"));

        var token = accessToken ?? _options.AccessToken;

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }
}
