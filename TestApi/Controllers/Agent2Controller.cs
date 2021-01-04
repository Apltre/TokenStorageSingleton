using System;
using Microsoft.AspNetCore.Mvc;
using TestApi.Models;
using TestApi.Helpers;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("agent2")]
    public class Agent2Controller : ControllerBase
    {
        private static int accessTokenRequestCount = 0;
        private const int accessTokenLifetimeSeconds = 3;
        private const int unexpectedAgentTimeShift = 6;
        private static DateTime lastAccessTokenExpirationDate;

        [HttpPost("token/access")]
        public IActionResult Tokens([FromForm]Agent2AuthData authData)
        {
            if (authData != null
                && authData.Password == "password3"
                && authData.Username == "username3"
                && authData.Field1 == "field1"
                && authData.Field2 == "field2")
            {
                accessTokenRequestCount++;
                lastAccessTokenExpirationDate = DateTime.UtcNow.AddSeconds(unexpectedAgentTimeShift);
                return this.Ok(new
                {
                    access_token = JwtTokenGenerator.Create(lastAccessTokenExpirationDate.AddHours(6), "access"),
                    expires_in = accessTokenLifetimeSeconds
                });
            }

            return this.Unauthorized("Wrong credentials");
        }

        [HttpGet("tokensInfo")]
        public IActionResult GetTokensInfo()
        {
            return this.Ok(new Agent1TokensInfo
            {
                AccessTokenRequestCount = accessTokenRequestCount,
                LastAccessTokenExpirationDate = lastAccessTokenExpirationDate
            });
        }
    }
}