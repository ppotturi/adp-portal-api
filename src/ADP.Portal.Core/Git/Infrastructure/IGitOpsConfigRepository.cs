using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitOpsConfigRepository
    {
        Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo);

        Task<Dictionary<string, Dictionary<string, object>>> GetAllFilesAsync(GitRepo gitRepo, string path);

        Task<bool> CommitGeneratedFilesToBranchAsync(GitRepo gitRepoFluxServices, Dictionary<string, Dictionary<string, object>> generatedFiles, string branchName);
    }
}