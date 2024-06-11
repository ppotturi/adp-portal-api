using ADP.Portal.Core.Git.Entities;
using Octokit;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public interface IGitHubRepository
    {
        Task<T?> GetFileContentAsync<T>(GitRepo gitRepo, string fileName);

        Task<string> CreateFileAsync(GitRepo gitRepo, string fileName, string content);

        Task<string> UpdateFileAsync(GitRepo gitRepo, string fileName, string content);

        Task<IEnumerable<KeyValuePair<string, FluxTemplateFile>>> GetAllFilesAsync(GitRepo gitRepo, string path);

        Task<Reference?> GetBranchAsync(GitRepo gitRepo, string branchName);

        Task<Reference> CreateBranchAsync(GitRepo gitRepo, string branchName, string sha);

        Task<Reference> UpdateBranchAsync(GitRepo gitRepo, string branchName, string sha);

        Task<Commit?> CreateCommitAsync(GitRepo gitRepo, Dictionary<string, FluxTemplateFile> generatedFiles, string message, string? branchName = null);

        Task<bool> CreatePullRequestAsync(GitRepo gitRepo, string branchName, string message);
    }
}