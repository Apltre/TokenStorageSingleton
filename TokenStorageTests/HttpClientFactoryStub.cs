using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using TestApi;

namespace TokenStorageTests
{
    public class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClientFactoryStub(WebApplicationFactory<Startup> factory)
        {
            this.Factory = factory;
        }

        public WebApplicationFactory<Startup> Factory { get; }

        public HttpClient CreateClient(string name)
        {
            return this.Factory.CreateClient();
        }
    }
}