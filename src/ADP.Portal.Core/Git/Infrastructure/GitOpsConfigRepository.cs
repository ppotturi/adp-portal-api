using ADP.Portal.Core.Git.Entities;
using Octokit;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;


namespace ADP.Portal.Core.Git.Infrastructure
{
    public class GitOpsConfigRepository : IGitOpsConfigRepository
    {
        private readonly IGitHubClient gitHubClient;

        public GitOpsConfigRepository(IGitHubClient gitHubClient)
        {
            this.gitHubClient = gitHubClient;
        }

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
    }
}
