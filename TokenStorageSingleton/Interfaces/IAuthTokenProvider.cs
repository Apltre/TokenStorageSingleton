using System.Threading.Tasks;

namespace TokenStorageSingleton.Interfaces
{ 
    public interface IAuthTokenProvider
    {
        Task<string> GetAccessTokenAsync();
        Task CleanApiTokensAsync();
    }
}