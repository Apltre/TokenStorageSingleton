using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TokenStorageSingleton.Exceptions;
using TokenStorageSingleton.Helpers;
using TokenStorageSingleton.Models;

namespace TokenStorageSingleton
{
    public class AgentSecurityTokens
    {
        [JsonConverter(typeof(JsonToAgentTokensConverter))]
        public JwtSecurityToken Access { get; set; }
        [JsonConverter(typeof(JsonToAgentTokensConverter))]
        public JwtSecurityToken Refresh { get; set; }

        [JsonIgnore]
        public readonly SemaphoreSlim TokenLock = new SemaphoreSlim(1, 1);
    }

    public class AgentRefreshedToken
    {
        [JsonConverter(typeof(JsonToAgentTokensConverter))]
        public JwtSecurityToken Access { get; set; }
    }

    public class StandardTokenProvider : TokenProviderBase
    {
        protected string TokensApiUrlEnding = "jwtAuth/tokens";
        protected string RefreshTokenApiUrlEnding = "jwtAuth/refresh";
        protected AgentSecurityTokens tokens { get; set; }
        protected CommonAuthData AuthData { get; set; }

        public StandardTokenProvider(IHttpClientFactory httpClientFactory, string authDataJson, uint refreshBeforeExpirationSeconds) : base(httpClientFactory, refreshBeforeExpirationSeconds)
        {
            this.AuthData = JsonConvert.DeserializeObject<CommonAuthData>(authDataJson);
        }

        protected async Task<string> GetAccessToken(AgentSecurityTokens tokens)
        {
            if (tokens.Access.Payload.ValidTo > DateTime.UtcNow.AddSeconds(this.RefreshBeforeExpirationSeconds))
            {
                return tokens.Access.RawData;
            }

            if (tokens.Refresh.Payload.ValidTo > DateTime.UtcNow.AddSeconds(this.RefreshBeforeExpirationSeconds))
            {
                await tokens.TokenLock.WaitAsync();
                try
                {
                    if (tokens.Access.Payload.ValidTo < DateTime.UtcNow.AddSeconds(this.RefreshBeforeExpirationSeconds))
                    {
                        tokens.Access = await this.RefreshAccessToken(tokens.Refresh.RawData);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    tokens.TokenLock.Release();
                }

                return tokens.Access.RawData;
            }

            await tokens.TokenLock.WaitAsync();
            try
            {
                if (tokens.Refresh.Payload.ValidTo < DateTime.UtcNow.AddSeconds(this.RefreshBeforeExpirationSeconds))
                {
                    var newAgentSecurityTokens = await this.RequestSecurityTokensAsync();
                    tokens.Access = newAgentSecurityTokens.Access;
                    tokens.Refresh = newAgentSecurityTokens.Refresh;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                tokens.TokenLock.Release();
            }

            return tokens.Access.RawData;
        }

        protected virtual async Task<AgentSecurityTokens> RequestSecurityTokensAsync()
        {
            var httpClient = this.GetHttpClientWithBaseAddress(this.AuthData.AuthUrl);

            var response = await httpClient.PostJsonAsync(this.TokensApiUrlEnding, new { username = AuthData.Username, password = AuthData.Password })
                                           .HandleTimeoutAsync("Agent token service timeout!");

            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<AgentSecurityTokens>(content);
            }

            throw new RepeatableRequestException($"Agent service error received: Status code: {response.StatusCode}| Returned message: {content}");
        }

        protected async Task<JwtSecurityToken> RefreshAccessToken(string refreshToken)
        {
            var httpClient = this.GetHttpClientWithBaseAddress(this.AuthData.AuthUrl);

            var response = await httpClient.PostJsonAsync($"{this.RefreshTokenApiUrlEnding}", new { refresh = refreshToken })
                                           .HandleTimeoutAsync("Agent refresh token service timeout!");
            
            var content = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    await this.CleanApiTokensAsync();
                    break;
                case HttpStatusCode.OK:
                    return JsonConvert.DeserializeObject<AgentRefreshedToken>(content).Access;
            }

            throw new RepeatableRequestException($"Agent refresh token service error received: Status code: {response.StatusCode}| Error: {content}");
        }

        public override async Task CleanApiTokensAsync()
        {
            await this.StorageLock.WaitAsync();
            try
            {
                this.tokens = null;
            }
            finally
            {
                this.StorageLock.Release();
            }
        }

        public override async Task<string> GetAccessTokenAsync()
        {
            if (this.tokens != null)
            {
                return await this.GetAccessToken(this.tokens);
            }

            await this.StorageLock.WaitAsync();
            try
            {
                if (this.tokens == null)
                {
                    this.tokens = await this.RequestSecurityTokensAsync();
                    return this.tokens.Access.RawData;
                }

                return await this.GetAccessToken(this.tokens);
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