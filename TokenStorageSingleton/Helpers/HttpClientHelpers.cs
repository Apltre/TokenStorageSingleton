using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TokenStorageSingleton.Helpers
{
    public static class JsonHttpExtensions
    {
        public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string uri, object obj)
        {
            return SendJsonAsync(client, uri, HttpMethod.Post, obj);
        }

        public static Task<HttpResponseMessage> SendJsonAsync(this HttpClient client, string uri, HttpMethod method,
            object obj)
        {
            var request = new HttpRequestMessage(method, uri);
            var body = new StringContent(JsonConvert.SerializeObject(obj));
            body.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = body;
            return client.SendAsync(request);
        }
    }
}
