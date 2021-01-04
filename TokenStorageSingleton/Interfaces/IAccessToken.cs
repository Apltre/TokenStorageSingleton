using System;

namespace TokenStorageSingleton.Interfaces
{
    public interface IAccessToken
    {
        string Token { get; }
        DateTime ValidToUtc { get; }
    }
}