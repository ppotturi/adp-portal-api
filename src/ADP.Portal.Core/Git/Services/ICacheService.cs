namespace ADP.Portal.Core.Git.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value);
    }
}
