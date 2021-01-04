using System;
using Microsoft.AspNetCore.Mvc;
using TestApi.Models;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("agent1")]
    public class Agent1Controller : ControllerBase
    {
        private static int accessTokenRequestCount = 0;
        private static DateTime lastAccessTokenExpirationDate;
        private const int accessTokenLifetimeSeconds = 3;
        private const int moscowTimezone = 3;

        [HttpPost("jwtAuth/access")]
        public IActionResult Tokens([FromBody]StandardAuthData authData)
        {
            if (authData != null
                && authData.Password == "password2"
                && authData.Username == "username2")
            {
                accessTokenRequestCount++;
                lastAccessTokenExpirationDate = DateTime.UtcNow.AddSeconds(accessTokenLifetimeSeconds);
                return this.Ok(new
                {
                    AccessToken = Guid.NewGuid().ToString(),
                    Expires_in = lastAccessTokenExpirationDate.AddHours(moscowTimezone)
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
                LastAccessTokenExpirationDate = lastAccessTokenExpirationDate,
            });
        }
    }
}
