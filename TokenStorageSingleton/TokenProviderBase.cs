using TokenStorageSingleton.Interfaces;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TokenStorageSingleton
{
    public abstract class TokenProviderBase : IAuthTokenProvider
    {
        protected readonly SemaphoreSlim StorageLock = new SemaphoreSlim(1, 1);
        private IHttpClientFactory httpClientFactory { get; set; }
        protected uint RefreshBeforeExpirationSeconds { get; }

        protected TokenProviderBase(IHttpClientFactory httpClientFactory, uint refreshBeforeExpirationSeconds)
        {
            this.httpClientFactory = httpClientFactory;
            this.RefreshBeforeExpirationSeconds = refreshBeforeExpirationSeconds;
        }

        protected HttpClient GetHttpClientWithBaseAddress(string authUrl)
        {
            var client = this.httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(authUrl);
            return client;
        }

        public abstract Task<string> GetAccessTokenAsync();
        public abstract Task CleanApiTokensAsync();
    }
}