namespace TestApi.Models
{
    public class StandardTokensInfo
    {
        public int RefreshTokenRequestCount { get; set; }
        public int AccessTokenRequestCount { get; set; }
        public string LastRefreshToken { get; set; }
    }
}
