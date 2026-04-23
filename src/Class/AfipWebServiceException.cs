namespace AfipSDK.Afip.Net;

public class AfipWebServiceException : Exception
{
    public int Code { get; }

    public AfipWebServiceException(string message, int code) : base(message)
    {
        Code = code;
    }
}
