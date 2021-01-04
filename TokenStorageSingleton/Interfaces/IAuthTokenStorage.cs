using System.Threading.Tasks;

namespace TokenStorageSingleton.Interfaces
{
    public interface IAuthTokenStorage
    {
        Task<string> GetAccessTokenAsync(string jsonAuthData);
        Task CleanApiTokensAsync(string jsonAuthData);
    }
}