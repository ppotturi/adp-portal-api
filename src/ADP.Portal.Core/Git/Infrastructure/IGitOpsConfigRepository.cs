using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitOpsConfigRepository
    {
        Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo);
    }
}