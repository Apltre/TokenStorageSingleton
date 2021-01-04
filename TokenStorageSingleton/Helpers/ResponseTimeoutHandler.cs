using System.Net.Http;
using System.Threading.Tasks;
using TokenStorageSingleton.Exceptions;

namespace TokenStorageSingleton.Helpers
{
    public static class HttpClientResponseTimeoutHandler
    {
        public static async Task<HttpResponseMessage> HandleTimeoutAsync(this Task<HttpResponseMessage> response, string timeoutErrorMessage = "HttpClient timeout on auth!")
        {
            try
            {
                return await response;
            }
            catch (TaskCanceledException)
            {
                throw new RepeatableRequestException(timeoutErrorMessage);
            }
        }
    }
}
