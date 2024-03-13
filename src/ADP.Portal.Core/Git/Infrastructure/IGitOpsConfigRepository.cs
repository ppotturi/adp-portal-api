using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitOpsConfigRepository
    {
        Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo);

        Task<IEnumerable<KeyValuePair<string, Dictionary<object, object>>>> GetAllFilesAsync(GitRepo gitRepo, string path);

        Task<bool> CommitFilesToBranchAsync(GitRepo gitRepo, Dictionary<string, Dictionary<object, object>> generatedFiles, string branchName, string message);

        Task<bool> CreatePullRequestAsync(GitRepo gitRepo, string branchName, string message);
    }
}