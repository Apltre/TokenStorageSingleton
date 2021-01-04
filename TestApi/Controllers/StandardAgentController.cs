using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using TestApi.Models;
using TestApi.Helpers;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("standard")]
    public class StandardAgentController : ControllerBase
    {
        private static string lastRefreshToken;
        private static int refreshTokenRequestCount = 0;
        private static int accessTokenRequestCount = 0;
        private const int accessTokenLifetime = 3;
        private const int refreshTokenLifetime = 8;
       

        [HttpPost("jwtAuth/tokens")]
        public IActionResult Tokens([FromBody] StandardAuthData authData)
        {
            if (authData != null
                && authData.Password == "password1"
                && authData.Username == "username1")
            {
                var refreshToken = JwtTokenGenerator.Create(DateTime.UtcNow.AddSeconds(refreshTokenLifetime), "refresh");

                lastRefreshToken = refreshToken;
                refreshTokenRequestCount++;
                accessTokenRequestCount++;

                return this.Ok(new
                {
                    access = JwtTokenGenerator.Create(DateTime.UtcNow.AddSeconds(accessTokenLifetime), "access"),
                    refresh = refreshToken
                });
            }

            return this.Unauthorized(new { Error = "Wrong credentials" });
        }

        [HttpPost("jwtAuth/refresh")]
        public IActionResult Refresh([FromBody] RefreshToken authorizationHeader)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(authorizationHeader.Refresh);

            if (token.ValidTo >= DateTime.UtcNow)
            {
                var accessToken = JwtTokenGenerator.Create(DateTime.UtcNow.AddSeconds(accessTokenLifetime), "access");
                accessTokenRequestCount++;
                return this.Ok(new
                {
                    access = accessToken,
                });
            }
            return this.Unauthorized(new { Error = "Wrong refresh token" });
        }

        [HttpGet("tokensInfo")]
        public IActionResult GetTokensInfo()
        {
            return this.Ok(new StandardTokensInfo
            {
                RefreshTokenRequestCount = refreshTokenRequestCount,
                AccessTokenRequestCount = accessTokenRequestCount,
                LastRefreshToken = lastRefreshToken
            });
        }
    }
}
