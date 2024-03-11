using ADP.Portal.Core.Git.Entities;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Tokens;
using Microsoft.VisualStudio.Services.Common;

namespace ADP.Portal.Core.Git.Infrastructure
{
    public class GitOpsConfigRepository(IGitHubClient gitHubClient) : IGitOpsConfigRepository
    {
        private readonly IGitHubClient gitHubClient = gitHubClient;

        public async Task<T?> GetConfigAsync<T>(string fileName, GitRepo gitRepo)
        {
            var file = await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.RepoName, fileName, gitRepo.BranchName);
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(file[0].Content, typeof(T));
            }

            var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

            var result = deserializer.Deserialize<T>(file[0].Content);
            return result;
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> GetAllFilesAsync(GitRepo gitRepo, string path)
        {
            return await GetAllFilesContentsAsync(gitRepo, path);
        }

        private async Task<Dictionary<string, Dictionary<string, object>>> GetAllFilesContentsAsync(GitRepo gitRepo, string path)
        {
            var repositoryItems = await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.RepoName, path, gitRepo.BranchName);
            var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

            var files = new Dictionary<string, Dictionary<string, object>>();
            foreach (var item in repositoryItems)
            {
                if (item.Type.Equals(ContentType.Dir))
                {
                    files.AddRange(await GetAllFilesContentsAsync(gitRepo, item.Path));
                }
                else if (item.Type.Equals(ContentType.File))
                {
                    var file = await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.RepoName, item.Path, gitRepo.BranchName);

                    var result = deserializer.Deserialize<Dictionary<string, object>>(file[0].Content);
                    files.Add(item.Path, result);
                }
            }
            return files;
        }
    }
}
