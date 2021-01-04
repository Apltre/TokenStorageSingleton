using TokenStorageSingleton.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TokenStorageSingleton
{
    public abstract class AccessTokenProvider : TokenProviderBase
    {     
        protected IAccessToken accessToken;

        public AccessTokenProvider(IHttpClientFactory httpClientFactory, uint refreshBeforeExpirationSeconds) : base(httpClientFactory, refreshBeforeExpirationSeconds){ }
    
        protected abstract Task<IAccessToken> RequestAccessTokenAsync();

        public override async Task CleanApiTokensAsync()
        {
            await this.StorageLock.WaitAsync();
            try
            {
                this.accessToken = null;
            }
            finally
            {
                this.StorageLock.Release();
            }
        }

        public override async Task<string> GetAccessTokenAsync()
        {
            if (this.accessToken != null)
            {
                if (this.accessToken.ValidToUtc > DateTime.UtcNow.AddSeconds(this.RefreshBeforeExpirationSeconds))
                {
                    return this.accessToken.Token;
                }

                await this.StorageLock.WaitAsync();
                try
                {
                    if (this.accessToken.ValidToUtc <= DateTime.UtcNow.AddSeconds(this.RefreshBeforeExpirationSeconds))
                    {
                        var newToken = await this.RequestAccessTokenAsync();
                        this.accessToken = newToken;
                        return newToken.Token;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    this.StorageLock.Release();
                }
            }

            await this.StorageLock.WaitAsync();
            try
            {
                if (this.accessToken == null)
                {
                    var newToken = await this.RequestAccessTokenAsync();
                    this.accessToken = newToken;
                    return newToken.Token;
                }

                return this.accessToken.Token;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.StorageLock.Release();
            }
        }
    }
}

