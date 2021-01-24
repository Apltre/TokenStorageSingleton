using Newtonsoft.Json;
using TokenStorageSingleton.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TokenStorageSingleton.Models;
using TokenStorageSingleton.Exceptions;
using TokenStorageSingleton.Helpers;

namespace TokenStorageSingleton
{
    internal class Agent1SecurityToken : IAccessToken
    {
        [JsonProperty("AccessToken")]
        public string Token { get; set; }

        [JsonProperty("expires_in")]
        public DateTime MoscowTimeZoneTimeExpiresIn { get; set; }

        public DateTime ValidToUtc => MoscowTimeZoneTimeExpiresIn.AddHours(-3);
    }

    public class Agent1TokenProvider : AccessTokenProvider
    {
        protected CommonAuthData AuthData { get; set; }

        public Agent1TokenProvider(IHttpClientFactory httpClientFactory, string authDataJson, uint refreshBeforeExpirationSeconds) : base(httpClientFactory, refreshBeforeExpirationSeconds)
        {
            this.AuthData = JsonConvert.DeserializeObject<CommonAuthData>(authDataJson);
        }

        protected override async Task<IAccessToken> RequestAccessTokenAsync()
        {
            var response = await this.GetHttpClientWithBaseAddress(this.AuthData.AuthUrl)
                                     .PostJsonAsync("jwtAuth/access", new { username = AuthData.Username, password = AuthData.Password })
                                     .HandleTimeoutAsync("AgentOne token service timeout!");
         
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    this.CleanTokenWithoutLock();
                    break;
                case HttpStatusCode.OK:
                    var json = await response.Content.ReadAsStringAsync();
                    if (String.IsNullOrEmpty(json))
                    {
                        throw new RepeatableRequestException($"No AgentOne token received: Status code: {response.StatusCode}");
                    }
                    return JsonConvert.DeserializeObject<Agent1SecurityToken>(json);
            }

            throw new RepeatableRequestException($"AgentOne error received: Status code: {response.StatusCode}| Returned message: {response.Content}");
        }
    }
}

