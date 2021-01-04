using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApi;
using Microsoft.AspNetCore.Mvc.Testing;
using TokenStorageSingleton;
using System.Threading.Tasks;
using TokenStorageSingleton.Models;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using TestApi.Models;
using System.Net.Http;

namespace TokenStorageTests
{
    [TestClass]
    public class TokenStorageTests
    {
        private WebApplicationFactory<Startup>  factory = new WebApplicationFactory<Startup>();
        private HttpClientFactoryStub clientFactory { get; set; }
        private AgentsTokenStorageAggregator agentsTokenStorage { get; set; }

        public TokenStorageTests()
        {
            this.clientFactory = new HttpClientFactoryStub(factory);
            this.agentsTokenStorage = new AgentsTokenStorageAggregator(clientFactory, 1);
        }

        private async Task delayTillTokenExpiration(string jwtToken)
        {
            JwtSecurityTokenHandler jwtTokenHandler = new JwtSecurityTokenHandler();
            var token = jwtTokenHandler.ReadJwtToken(jwtToken);
            await this.delayTillTokenExpiration(token.ValidTo);
        }

        private async Task delayTillTokenExpiration(DateTime expirationDate)
        {
            if (expirationDate > DateTime.UtcNow)
            {
                var timeTillExpiration = new TimeSpan((expirationDate - DateTime.UtcNow).Ticks);
                await Task.Delay(timeTillExpiration);
            }
        }

        private async Task<T> getTokensInfo<T>(string agentType)
        {
            var client = this.clientFactory.CreateClient();
            var responseJson = await (await client.GetAsync($"http://localhost/{agentType}/tokensInfo")).Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseJson);
        }

        private async Task assertNewAccessTokenGeneration()
        {
            await Task.Delay(1000);
        }

        [TestMethod]
        public async Task Should_TestStandardTokenProvider()
        {
            var providerType = "standard";
            var authData = new CommonAuthData()
            {
                Username = "username1",
                Password = "password1",
                AuthType = providerType,
                AuthUrl = $"http://localhost/{providerType}/"
            };

            var jsonAuthData = JsonConvert.SerializeObject(authData);

            var accessToken1 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            var accessToken2 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);

            var tokensInfo = await this.getTokensInfo<StandardTokensInfo>(providerType);
            Assert.AreEqual(accessToken1, accessToken2, "Access token changed before expiration");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 1, "Current access token request count isn't correct. Expected: 1");
            Assert.AreEqual(tokensInfo.RefreshTokenRequestCount, 1, "Current refresh token request count isn't correct. Expected: 1");

            await this.delayTillTokenExpiration(accessToken2);
            var accessToken3 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<StandardTokensInfo>(providerType);

            Assert.AreNotEqual(accessToken3, accessToken2, "Access token hasn't been changed");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 2, "Access token wasn't refreshed");
            Assert.AreEqual(tokensInfo.RefreshTokenRequestCount, 1, "Current refresh token request count isn't correct. Expected: 1");

            await this.delayTillTokenExpiration(tokensInfo.LastRefreshToken);

            var accessToken4 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<StandardTokensInfo>(providerType);

            Assert.AreNotEqual(accessToken4, accessToken3, "Access token hasn't been changed");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 3, "Access token wasn't refreshed on refresh tonen change");
            Assert.AreEqual(tokensInfo.RefreshTokenRequestCount, 2, "Current refresh token request count isn't correct. Expected: 2");
            
            var accessToken5 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            Assert.AreEqual(accessToken5, accessToken4, "Access token changed before expiration");

            await this.assertNewAccessTokenGeneration();
            await this.agentsTokenStorage.CleanApiTokensAsync(jsonAuthData);

            var accessToken6 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<StandardTokensInfo>(providerType);

            Assert.AreNotEqual(accessToken6, accessToken5, "Access token hasn't been changed");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 4, "Access token wasn't refreshed on cleanup");
            Assert.AreEqual(tokensInfo.RefreshTokenRequestCount, 3, "Current refresh token request count isn't correct. Expected: 3");
        }

        [TestMethod]
        public async Task Should_TestAgent1TokenProvider()
        {
            var providerType = "agent1";
            var authData = new CommonAuthData()
            {
                Username = "username2",
                Password = "password2",
                AuthType = providerType,
                AuthUrl = $"http://localhost/{providerType}/"
            };

            var jsonAuthData = JsonConvert.SerializeObject(authData);

            var accessToken1 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            var accessToken2 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);

            Assert.AreEqual(accessToken1, accessToken2, "Access token refreshed before expiration");
            var tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 1, "Current access token request count isn't correct. Expected: 1");

            await this.delayTillTokenExpiration(tokensInfo.LastAccessTokenExpirationDate);

            var accessToken3 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);

            Assert.AreNotEqual(accessToken3, accessToken2, "Access token wasn't refreshed after expiration");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 2, "Current access token request count isn't correct. Expected: 2");

            var accessToken4 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);
            Assert.AreEqual(accessToken4, accessToken3, "Access token refreshed before expiration");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 2, "Current access token request count isn't correct. Expected: 2");

            await this.assertNewAccessTokenGeneration();
            await this.agentsTokenStorage.CleanApiTokensAsync(jsonAuthData);

            var accessToken5 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);

            Assert.AreNotEqual(accessToken5, accessToken4, "Access token hasn't been changed");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 3, "Current access token request count isn't correct. Expected: 3");
        }

        [TestMethod]
        public async Task Should_TestAgent2TokenProvider()
        {
            var providerType = "agent2";
            var authData = new TokenStorageSingleton.Models.Agent2AuthData
            {
                Username = "username3",
                Password = "password3",
                AuthType = providerType,
                AuthUrl = $"http://localhost/{providerType}/",
                Field1 = "field1",
                Field2 = "field2"
            };

            var jsonAuthData = JsonConvert.SerializeObject(authData);

            var accessToken1 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            var accessToken2 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);

            Assert.AreEqual(accessToken1, accessToken2, "Access token refreshed before expiration");
            var tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 1, "Current access token request count isn't correct. Expected: 1");

            await this.delayTillTokenExpiration(tokensInfo.LastAccessTokenExpirationDate);

            var accessToken3 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);

            Assert.AreNotEqual(accessToken3, accessToken2, "Access token wasn't refreshed after expiration");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 2, "Current access token request count isn't correct. Expected: 2");

            var accessToken4 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);
            Assert.AreEqual(accessToken4, accessToken3, "Access token refreshed before expiration");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 2, "Current access token request count isn't correct. Expected: 2");

            await this.assertNewAccessTokenGeneration();
            await this.agentsTokenStorage.CleanApiTokensAsync(jsonAuthData);

            var accessToken5 = await this.agentsTokenStorage.GetAccessTokenAsync(jsonAuthData);
            tokensInfo = await this.getTokensInfo<Agent1TokensInfo>(providerType);

            Assert.AreNotEqual(accessToken5, accessToken4, "Access token hasn't been changed");
            Assert.AreEqual(tokensInfo.AccessTokenRequestCount, 3, "Current access token request count isn't correct. Expected: 3");
        }
    }
}
