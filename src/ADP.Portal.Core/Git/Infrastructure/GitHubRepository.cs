using ADP.Portal.Core.Git.Entities;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public class GitHubRepository : IGitHubRepository
    {
        private readonly IGitHubClient gitHubClient;
        private readonly IDeserializer deserializer;
        private readonly ISerializer serializer;

        public GitHubRepository(IGitHubClient gitHubClient, IDeserializer deserializer, ISerializer serializer)
        {
            this.gitHubClient = gitHubClient;
            this.deserializer = deserializer;
            this.serializer = serializer;
        }

        public async Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo)
        {
            try
            {
                var file = await GetRepositoryFiles(gitRepo, fileName);
                if (typeof(T) == typeof(string))
                {
                    return (T)Convert.ChangeType(file[0].Content, typeof(T));
                }

                var result = deserializer.Deserialize<T>(file[0].Content);
                return result;
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public async Task<string> CreateConfigAsync(GitRepo gitRepo, string fileName, string content)
        {
            var response = await gitHubClient.Repository.Content.CreateFile(gitRepo.Organisation, gitRepo.Name, fileName, new CreateFileRequest($"Create config file: {fileName}", content, gitRepo.Reference));
            return response.Commit.Sha;
        }

        public async Task<string> UpdateConfigAsync(GitRepo gitRepo, string fileName, string content)
        {
            var existingFile = await GetRepositoryFiles(gitRepo, fileName);

            if (existingFile.Any())
            {
                var response = await gitHubClient.Repository.Content.UpdateFile(gitRepo.Organisation, gitRepo.Name, fileName,
                    new UpdateFileRequest($"Update config file: {fileName}", content, existingFile[0].Sha, gitRepo.Reference));
                return response.Commit.Sha;
            }
            return string.Empty;
        }

        public async Task<IEnumerable<KeyValuePair<string, FluxTemplateFile>>> GetAllFilesAsync(GitRepo gitRepo, string path)
        {
            return await GetAllFilesContentsAsync(gitRepo, path);
        }

        public async Task<bool> CreatePullRequestAsync(GitRepo gitRepo, string branchName, string message)
        {
            var repository = await gitHubClient.Repository.Get(gitRepo.Organisation, gitRepo.Name);

            var pullRequest = new NewPullRequest(message, branchName, gitRepo.Reference);
            var createdPullRequest = await gitHubClient.PullRequest.Create(repository.Owner.Login, repository.Name, pullRequest);

            return createdPullRequest != null;
        }

        public async Task<IEnumerable<KeyValuePair<string, FluxTemplateFile>>> GetAllFilesContentsAsync(GitRepo gitRepo, string path)
        {
            var repositoryItems = await GetRepositoryFiles(gitRepo, path);

            var fileTasks = repositoryItems
                .Where(item => item.Type == ContentType.File)
                .Select(async item =>
                {
                    var file = await GetRepositoryFiles(gitRepo, item.Path);
                    var result = deserializer.Deserialize<Dictionary<object, object>>(file[0].Content);
                    var list = new List<KeyValuePair<string, FluxTemplateFile>>() { new(item.Path, new FluxTemplateFile(result)) };
                    return list.AsEnumerable();
                });

            var dirTasks = repositoryItems
                .Where(item => item.Type == ContentType.Dir)
                .Select(async item => await GetAllFilesContentsAsync(gitRepo, item.Path));

            var allTasks = fileTasks.Concat(dirTasks);
            var allResults = await Task.WhenAll(allTasks);

            return allResults.SelectMany(x => x);
        }

        public async Task<Reference?> GetBranchAsync(GitRepo gitRepo, string branchName)
        {
            try
            {
                return await gitHubClient.Git.Reference.Get(gitRepo.Organisation, gitRepo.Name, branchName);
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public async Task<Reference> CreateBranchAsync(GitRepo gitRepo, string branchName, string sha)
        {
            return await gitHubClient.Git.Reference.Create(gitRepo.Organisation, gitRepo.Name, new NewReference(branchName, sha));
        }

        public async Task<Reference> UpdateBranchAsync(GitRepo gitRepo, string branchName, string sha)
        {
            return await gitHubClient.Git.Reference.Update(gitRepo.Organisation, gitRepo.Name, branchName, new ReferenceUpdate(sha));
        }

        public async Task<Commit?> CreateCommitAsync(GitRepo gitRepo, Dictionary<string, FluxTemplateFile> generatedFiles, string message, string? branchName = null)
        {
            var branch = branchName ?? $"heads/{gitRepo.Reference}";

            var repository = await gitHubClient.Repository.Get(gitRepo.Organisation, gitRepo.Name);

            var branchRef = await gitHubClient.Git.Reference.Get(repository.Owner.Login, repository.Name, branch);

            var latestCommit = await gitHubClient.Git.Commit.Get(repository.Owner.Login, repository.Name, branchRef.Object.Sha);

            var featureBranchTree = await CreateTree(gitHubClient, repository, generatedFiles, latestCommit.Sha);
            if (featureBranchTree != null)
            {
                return await CreateCommit(gitHubClient, repository, message, featureBranchTree.Sha, branchRef.Object.Sha);
            }
            return default;
        }

        private async Task<TreeResponse?> CreateTree(IGitHubClient client, Repository repository, Dictionary<string, FluxTemplateFile> treeContents, string parentSha)
        {
            var newTree = new NewTree() { BaseTree = parentSha };

            var existingTree = await client.Git.Tree.GetRecursive(repository.Owner.Login, repository.Name, parentSha);

            var tasks = treeContents.Select(async treeContent =>
            {
                var baselineBlob = new NewBlob
                {
                    Content = serializer.Serialize(treeContent.Value.Content).Replace(Constants.Flux.Templates.IMAGEPOLICY_KEY, Constants.Flux.Templates.IMAGEPOLICY_KEY_VALUE),
                    Encoding = EncodingType.Utf8
                };

                var baselineBlobResult = await client.Git.Blob.Create(repository.Owner.Login, repository.Name, baselineBlob);

                return new NewTreeItem
                {
                    Type = TreeType.Blob,
                    Mode = Octokit.FileMode.File,
                    Path = treeContent.Key,
                    Sha = baselineBlobResult.Sha
                };
            });

            var newTreeItems = await Task.WhenAll(tasks);

            newTree.Tree.AddRange(newTreeItems.Where(newItem => !existingTree.Tree.Any(existingItem => existingItem.Path == newItem.Path && existingItem.Sha == newItem.Sha)));

            if (newTree.Tree.Count > 0)
            {
                return await client.Git.Tree.Create(repository.Owner.Login, repository.Name, newTree);
            }
            return default;
        }

        private static async Task<Commit> CreateCommit(IGitHubClient client, Repository repository, string message, string sha, string parent)
        {
            var newCommit = new NewCommit(message, sha, parent);
            return await client.Git.Commit.Create(repository.Owner.Login, repository.Name, newCommit);
        }
        private async Task<IReadOnlyList<RepositoryContent>> GetRepositoryFiles(GitRepo gitRepo, string filePathOrName)
        {
            return await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, filePathOrName, gitRepo.Reference);
        }
    }
}