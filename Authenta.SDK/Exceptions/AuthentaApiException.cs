
namespace Authenta.SDK.Exceptions
{
    public class AuthentaApiException : AuthentaException
    {
        public int StatusCode { get; }

        public AuthentaApiException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
