using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TokenStorageSingleton.Interfaces;
using TokenStorageSingleton.Models;

namespace TokenStorageSingleton
{
    public class AgentsTokenStorageAggregator : IAuthTokenStorage
    {
        private readonly ConcurrentDictionary<string, IAuthTokenProvider> jwtProviders = new ConcurrentDictionary<string, IAuthTokenProvider>();
        private IHttpClientFactory httpClientFactory { get; }
        private uint refreshBeforeExpirationSeconds { get; }

        private readonly SemaphoreSlim storageLock = new SemaphoreSlim(1, 1);
        private readonly string providersNamespace = "TokenStorageSingleton";

        public AgentsTokenStorageAggregator(IHttpClientFactory httpClientFactory, uint refreshBeforeExpirationSeconds)
        {
            this.httpClientFactory = httpClientFactory;
            this.refreshBeforeExpirationSeconds = refreshBeforeExpirationSeconds;
        }

        public async Task CleanApiTokensAsync(string authDataJsonWithType)
        {
            await this.jwtProviders[authDataJsonWithType].CleanApiTokensAsync();
        }

        private string formatFullClassName(string authType)
        {
            if (string.IsNullOrWhiteSpace(authType))
            {
                throw new ArgumentException("No agents authorization type defined.");
            }
            return $"{providersNamespace}.{authType[0].ToString().ToUpper()}{authType.ToLower().Substring(1)}TokenProvider";
        }

        private IAuthTokenProvider getInstance(string authDataJsonWithType, string authType, uint refreshBeforeExpirationSeconds)
        {
            var providerType = Type.GetType(this.formatFullClassName(authType));
            if (providerType == null)
            {
                throw new ArgumentException($"No provider for agents authorization type {authType} exists.");
            }

            if (!providerType.GetInterfaces().Contains(typeof(IAuthTokenProvider)))
            {
                throw new Exception($"Found type {providerType.FullName} doesn't implement IAuthTokenProvider interface.");
            }
            return (IAuthTokenProvider)Activator.CreateInstance(providerType, new object[] { this.httpClientFactory, authDataJsonWithType, refreshBeforeExpirationSeconds });
        }

        public async Task<string> GetAccessTokenAsync(string authDataJsonWithType)
        {
            var authDataType = JsonConvert.DeserializeObject<TypedAuthData>(authDataJsonWithType).AuthType;

            if (!this.jwtProviders.TryGetValue(authDataJsonWithType, out var provider))
            {
                await this.storageLock.WaitAsync();
                try
                {
                    if (!this.jwtProviders.TryGetValue(authDataJsonWithType, out provider))
                    {
                        provider = this.getInstance(authDataJsonWithType, authDataType, this.refreshBeforeExpirationSeconds);
                        this.jwtProviders.TryAdd(authDataJsonWithType, provider);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    this.storageLock.Release();
                }
            }

            return await provider.GetAccessTokenAsync();
        }
    }
}