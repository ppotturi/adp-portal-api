using ADP.Portal.Core.Git.Entities;
using Octokit;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitOpsConfigRepository
    {
        Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo);

        Task<Dictionary<string, Dictionary<string, object>>> GetAllFilesAsync(GitRepo gitRepo, string path);
    }
}