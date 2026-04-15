namespace Afip.Net;

internal sealed class AfipHttpResponse(int status, string statusText, string data)
{
    public int Status { get; } = status;
    public string StatusText { get; } = statusText;
    public string Data { get; } = data;
}
