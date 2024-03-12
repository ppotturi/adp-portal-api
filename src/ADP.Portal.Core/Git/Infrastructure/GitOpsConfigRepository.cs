using ADP.Portal.Core.Git.Entities;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using Octokit;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
            var file = await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, fileName, gitRepo.BranchName);
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

        public async Task<Dictionary<string, Dictionary<object, object>>> GetAllFilesAsync(GitRepo gitRepo, string path)
        {
            return await GetAllFilesContentsAsync(gitRepo, path);
        }

        public async Task<bool> CommitGeneratedFilesToBranchAsync(GitRepo gitRepoFluxServices, Dictionary<string, Dictionary<object, object>> generatedFiles, string branchName)
        {
            // create a branch
            var mainRef = await gitHubClient.Git.Reference.Get(gitRepoFluxServices.Organisation, gitRepoFluxServices.Name, gitRepoFluxServices.BranchName);
            var newBranch = new NewReference(branchName, mainRef.Object.Sha);
            await gitHubClient.Git.Reference.Create(gitRepoFluxServices.Organisation, gitRepoFluxServices.Name, newBranch);

            //Commit changes


            //await CreateTree(gitHubClient, generatedFiles.Values);


            //var featureBranchTree = await gitHubClient.Git.CreateTree(repository, new Dictionary<string, string> { { "README.md", "I am overwriting this blob with something new\nand a second line too" } });
            return true;
        }

        private async Task<Dictionary<string, Dictionary<object, object>>> GetAllFilesContentsAsync(GitRepo gitRepo, string path)
        {
            var repositoryItems = await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, path, gitRepo.BranchName);
            var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

            var files = new Dictionary<string, Dictionary<object, object>>();
            foreach (var item in repositoryItems)
            {
                if (item.Type.Equals(ContentType.Dir))
                {
                    files.AddRange(await GetAllFilesContentsAsync(gitRepo, item.Path));
                }
                else if (item.Type.Equals(ContentType.File))
                {
                    var file = await gitHubClient.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, item.Path, gitRepo.BranchName);

                    var result = deserializer.Deserialize<Dictionary<object, object>>(file[0].Content);
                    files.Add(item.Path, result);
                }
            }
            return files;
        }


        private async Task<TreeResponse> CreateTree(IGitHubClient client, GitRepo gitRepoFluxServices, IEnumerable<KeyValuePair<string, string>> treeContents)
        {
            var collection = new List<NewTreeItem>();

            foreach (var c in treeContents)
            {
                var baselineBlob = new NewBlob
                {
                    Content = c.Value,
                    Encoding = EncodingType.Utf8
                };

                var baselineBlobResult = await client.Git.Blob.Create(gitRepoFluxServices.Organisation, gitRepoFluxServices.Name, baselineBlob);

                collection.Add(new NewTreeItem
                {
                    Type = TreeType.Blob,
                    Mode = Octokit.FileMode.File,
                    Path = c.Key,
                    Sha = baselineBlobResult.Sha
                });
            }

            var newTree = new NewTree();
            foreach (var item in collection)
            {
                newTree.Tree.Add(item);
            }

            return await client.Git.Tree.Create(gitRepoFluxServices.Organisation, gitRepoFluxServices.Name, newTree);
        }
    }
}
