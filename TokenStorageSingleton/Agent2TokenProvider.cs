using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TokenStorageSingleton.Exceptions;
using TokenStorageSingleton.Helpers;
using TokenStorageSingleton.Interfaces;
using TokenStorageSingleton.Models;

namespace TokenStorageSingleton
{
    internal class Agent2SecurityToken : IAccessToken
    {        
        [JsonProperty("access_token")]
        [JsonConverter(typeof(JsonToAgentTokensConverter))]
        public JwtSecurityToken Access { get; set; }

        public string Token => Access.RawData;

        [JsonProperty("expires_in")]
        private int expiresIn { get; set; }

        [JsonIgnore]
        private DateTime createdOn = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime ValidToUtc => createdOn.AddSeconds(this.expiresIn);
    }

    public class Agent2TokenProvider : AccessTokenProvider
    {
        protected Agent2AuthData AuthData { get; set; }

        public Agent2TokenProvider(IHttpClientFactory httpClientFactory, string authDataJson, uint refreshBeforeExpirationSeconds) : base(httpClientFactory, refreshBeforeExpirationSeconds)
        {
            this.AuthData = JsonConvert.DeserializeObject<Agent2AuthData>(authDataJson);
        }

        protected override async Task<IAccessToken> RequestAccessTokenAsync()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("field_1", this.AuthData.Field1);
            dict.Add("field_2", this.AuthData.Field2);
            dict.Add("username", this.AuthData.Username);
            dict.Add("password", this.AuthData.Password);

            var request = new HttpRequestMessage(HttpMethod.Post, "token/access") { Content = new FormUrlEncodedContent(dict) };

            var response = await this.GetHttpClientWithBaseAddress(this.AuthData.AuthUrl)
                                     .SendAsync(request)
                                     .HandleTimeoutAsync("Agent2 token service timeout!");

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    this.CleanTokenWithoutLock();
                    break;
                case HttpStatusCode.OK:
                    return JsonConvert.DeserializeObject<Agent2SecurityToken>(await response.Content.ReadAsStringAsync());
            }

            throw new RepeatableRequestException($"Agent2 token service error received: Status code: {response.StatusCode}| Returned message: {response.Content}");
        }
    }
}