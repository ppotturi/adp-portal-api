using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using AutoFixture;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ADP.Portal.Core.Tests.Git.Infrastructure
{
    [TestFixture]
    public class GitHubRepositoryTests
    {
        private IGitHubClient gitHubClientMock = null!;
        private GitHubRepository repository = null!;
        private IDeserializer deserializer = null!;
        private ISerializer serializer = null!;
        private Fixture fixture = null!;


        [SetUp]
        public void SetUp()
        {
            gitHubClientMock = Substitute.For<IGitHubClient>();
            serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            repository = new GitHubRepository(gitHubClientMock, deserializer, serializer);
            fixture = new Fixture();
        }

        [Test]
        public async Task GetFileContentAsync_WhenCalledWithStringType_ReturnsStringContent_Test()
        {
            // Arrange
            var fileName = "file1.yaml";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var contentFile = CreateRepositoryContent("fileContent");
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, fileName, gitRepo.Reference)
                .Returns([contentFile]);

            // Act
            var result = await repository.GetFileContentAsync<string>(gitRepo, fileName);

            // Assert
            Assert.That(result, Is.EqualTo("fileContent"));
        }

        [Test]
        public async Task GetFileContentAsync_WhenCalledWithNonStringType_DeserializesContent_Test()
        {
            // Arrange
            var fileName = "file1.yaml";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var yamlContent = "property:\n - name: \"test\"";
            var contentFile = CreateRepositoryContent(yamlContent);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, fileName, gitRepo.Reference)
                .Returns([contentFile]);

            // Act
            var result = await repository.GetFileContentAsync<TestType>(gitRepo, fileName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Property[0].Name, Is.EqualTo("test"));
        }

        [Test]
        public async Task GetFileContentAsync_NotFound_Test()
        {
            // Arrange
            var fileName = "file1.yaml";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, fileName, gitRepo.Reference)
                .Throws(new NotFoundException("", System.Net.HttpStatusCode.NotFound));

            // Act
            var result = await repository.GetFileContentAsync<TestType>(gitRepo, fileName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAllFilesAsync_SingleFile_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var yamlContent = "property:\n - name: \"test\"";
            var contentFile = CreateRepositoryContent(yamlContent);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "file", gitRepo.Reference)
                .Returns([contentFile]);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "path", gitRepo.Reference)
                .Returns([CreateRepositoryDirectoryContent()]);

            // Act
            var result = await repository.GetAllFilesAsync(gitRepo, "path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllFilesAsync_Multiple_NestedFile_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var yamlContent = "property:\n - name: \"test\"";
            var contentFile = CreateRepositoryContent(yamlContent);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "file", gitRepo.Reference)
                .Returns([contentFile, contentFile]);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "path", gitRepo.Reference)
                .Returns([CreateRepositoryDirectoryContent()]);

            // Act
            var result = await repository.GetAllFilesAsync(gitRepo, "path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetBranchAsync_Success_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var subReference = Substitute.For<Reference>();
            gitHubClientMock.Git.Reference.Get(gitRepo.Organisation, gitRepo.Name, "test-branch")
                .Returns(subReference);

            // Act
            var branch = await repository.GetBranchAsync(gitRepo, "test-branch");

            // Assert
            Assert.That(branch, Is.Not.Null);
            await gitHubClientMock.Git.Reference.Received().Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GetBranchAsync_Error_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            gitHubClientMock.Git.Reference.Get(gitRepo.Organisation, gitRepo.Name, "test")
                .Throws(new NotFoundException("Branch not found", System.Net.HttpStatusCode.NotFound));

            // Act
            var branch = await repository.GetBranchAsync(gitRepo, "test");

            // Assert
            Assert.That(branch, Is.Null);
            await gitHubClientMock.Git.Reference.Received().Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task CreateBranchAsync_Success_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var subReference = Substitute.For<Reference>();
            gitHubClientMock.Git.Reference.Create(gitRepo.Organisation, gitRepo.Name, new NewReference("refs/heads/features/test", "sha"))
                .Returns(subReference);

            // Act
            await repository.CreateBranchAsync(gitRepo, "refs/heads/features/test", "sha");

            // Assert
            await gitHubClientMock.Git.Reference.Received().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewReference>());
        }

        [Test]
        public async Task UpdateBranchAsync_Success_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var subReference = Substitute.For<Reference>();
            gitHubClientMock.Git.Reference.Update(gitRepo.Organisation, gitRepo.Name, "test", new ReferenceUpdate("sha"))
                .Returns(subReference);

            // Act
            await repository.UpdateBranchAsync(gitRepo, "refs/heads/features/test", "sha");

            // Assert
            await gitHubClientMock.Git.Reference.Received().Update(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ReferenceUpdate>());
        }

        [Test]
        public async Task CreateCommitAsync_Success_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var subReference = new Reference("", "", "", fixture.Create<TagObject>());
            var commit = fixture.Create<Commit>();
            var treeItem = new TreeItem("path", "mode", TreeType.Tree, 10, "sha", "url");
            var tree = new TreeResponse("4a8a08f09d37b73795649038408b5f33ccf5c497", "", [treeItem], false);

            string generateYaml = GeneratedTestYamlContent();

            string existingYaml = ExistingTestYamlContent();
            var generatedfiles = new Dictionary<string, FluxTemplateFile>
            {
                { "demo.yaml", new FluxTemplateFile(deserializer.Deserialize<Dictionary<object, object>>(generateYaml)) }
            };

            var repo = new Repository("", "", "", "", "", "", "", "", default, "", fixture.Create<User>(), "", "", default, "", "", "", default, default, default, default, "", default, default, default, default, fixture.Create<RepositoryPermissions>(),
                fixture.Create<Repository>(), fixture.Create<Repository>(), fixture.Create<LicenseMetadata>(), default, default, default, default, default, default, default, default, default, default, default, default, default,
                fixture.Create<RepositoryVisibility>(), new List<string>(), default, default, default);

            gitHubClientMock.Repository.Get(gitRepo.Organisation, gitRepo.Name).Returns(repo);
            gitHubClientMock.Git.Reference.Get(repo.Owner.Login, repo.Name, "test").Returns(subReference);
            gitHubClientMock.Git.Blob.Create(repo.Owner.Login, repo.Name, Arg.Any<NewBlob>()).Returns(fixture.Create<BlobReference>());
            gitHubClientMock.Git.Commit.Get(repo.Owner.Login, repo.Name, subReference.Object.Sha).Returns(commit);
            gitHubClientMock.Git.Tree.GetRecursive(repo.Owner.Login, repo.Name, commit.Sha).Returns(tree);
            gitHubClientMock.Git.Tree.Create(repo.Owner.Login, repo.Name, Arg.Any<NewTree>()).Returns(tree);
            var fileContant = CreateRepositoryContent(existingYaml);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "demo.yaml", gitRepo.Reference).Returns(new List<RepositoryContent>() { fileContant });

            // Act
            var actualValue = await repository.CreateCommitAsync(gitRepo, generatedfiles, "refs/heads/features/test", "test");

            // Assert
            Assert.That(actualValue, Is.Null);
            await gitHubClientMock.Git.Tree.Received().GetRecursive(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            await gitHubClientMock.Git.Tree.Received().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewTree>());
            await gitHubClientMock.Git.Commit.Received().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewCommit>());
        }

        [Test]
        public async Task CreateCommitAsync_Success_NoExstingFlux_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var subReference = new Reference("", "", "", fixture.Create<TagObject>());
            var commit = fixture.Create<Commit>();
            var treeItem = new TreeItem("path", "mode", TreeType.Tree, 10, "sha", "url");
            var tree = new TreeResponse("4a8a08f09d37b73795649038408b5f33ccf5c497", "", [treeItem], false);

            string generateYaml = GeneratedTestYamlContent();

            var existingYaml = @"";
            var generatedfiles = new Dictionary<string, FluxTemplateFile>
            {
                { "demo.yaml", new FluxTemplateFile(deserializer.Deserialize<Dictionary<object, object>>(generateYaml)) }
            };

            var repo = new Repository("", "", "", "", "", "", "", "", default, "", fixture.Create<User>(), "", "", default, "", "", "", default, default, default, default, "", default, default, default, default, fixture.Create<RepositoryPermissions>(),
                fixture.Create<Repository>(), fixture.Create<Repository>(), fixture.Create<LicenseMetadata>(), default, default, default, default, default, default, default, default, default, default, default, default, default,
                fixture.Create<RepositoryVisibility>(), new List<string>(), default, default, default);

            gitHubClientMock.Repository.Get(gitRepo.Organisation, gitRepo.Name).Returns(repo);
            gitHubClientMock.Git.Reference.Get(repo.Owner.Login, repo.Name, "test").Returns(subReference);
            gitHubClientMock.Git.Blob.Create(repo.Owner.Login, repo.Name, Arg.Any<NewBlob>()).Returns(fixture.Create<BlobReference>());
            gitHubClientMock.Git.Commit.Get(repo.Owner.Login, repo.Name, subReference.Object.Sha).Returns(commit);
            gitHubClientMock.Git.Tree.GetRecursive(repo.Owner.Login, repo.Name, commit.Sha).Returns(tree);
            gitHubClientMock.Git.Tree.Create(repo.Owner.Login, repo.Name, Arg.Any<NewTree>()).Returns(tree);
            var fileContant = CreateRepositoryContent(existingYaml);
            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "demo.yaml", gitRepo.Reference).Returns(new List<RepositoryContent>() { fileContant });

            // Act
            var actualValue = await repository.CreateCommitAsync(gitRepo, generatedfiles, "refs/heads/features/test", "test");

            // Assert
            Assert.That(actualValue, Is.Null);
            await gitHubClientMock.Git.Tree.Received().GetRecursive(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            await gitHubClientMock.Git.Tree.Received().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewTree>());
            await gitHubClientMock.Git.Commit.Received().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewCommit>());
        }

        [Test]
        public async Task CreateCommitAsync_NoChanges_Success_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var subReference = new Reference("", "", "", fixture.Create<TagObject>());
            var commit = fixture.Create<Commit>();
            var treeItem = new TreeItem("path", "mode", TreeType.Tree, 10, "sha", "url");
            var tree = new TreeResponse(commit.Sha, "", [treeItem], false);
            var files = Substitute.For<Dictionary<string, FluxTemplateFile>>();
            var repo = new Repository("", "", "", "", "", "", "", "", default, "", fixture.Create<User>(), "", "", default, "", "", "", default, default, default, default, "", default, default, default, default, fixture.Create<RepositoryPermissions>(),
                fixture.Create<Repository>(), fixture.Create<Repository>(), fixture.Create<LicenseMetadata>(), default, default, default, default, default, default, default, default, default, default, default, default, default,
                fixture.Create<RepositoryVisibility>(), new List<string>(), default, default, default);

            gitHubClientMock.Repository.Get(gitRepo.Organisation, gitRepo.Name).Returns(repo);
            gitHubClientMock.Git.Reference.Get(repo.Owner.Login, repo.Name, "heads/branch").Returns(subReference);
            gitHubClientMock.Git.Blob.Create(repo.Owner.Login, repo.Name, Arg.Any<NewBlob>()).Returns(fixture.Create<BlobReference>());
            gitHubClientMock.Git.Commit.Get(repo.Owner.Login, repo.Name, subReference.Object.Sha).Returns(commit);
            gitHubClientMock.Git.Tree.GetRecursive(repo.Owner.Login, repo.Name, commit.Sha).Returns(tree);
            gitHubClientMock.Git.Tree.Create(repo.Owner.Login, repo.Name, Arg.Any<NewTree>()).Returns(tree);

            // Act
            var actualValue = await repository.CreateCommitAsync(gitRepo, files, "refs/heads/features/test");

            // Assert
            Assert.That(actualValue, Is.Null);
            await gitHubClientMock.Git.Tree.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewTree>());
            await gitHubClientMock.Git.Commit.DidNotReceive().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewCommit>());
        }

        [Test]
        public async Task CreatePullRequestAsync_Success_Test()
        {
            // Arrange
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var repo = new Repository("", "", "", "", "", "", "", "", default, "", fixture.Create<User>(), "", "", default, "", "", "", default, default, default, default, "", default, default, default, default, fixture.Create<RepositoryPermissions>(),
                fixture.Create<Repository>(), fixture.Create<Repository>(), fixture.Create<LicenseMetadata>(), default, default, default, default, default, default, default, default, default, default, default, default, default,
                fixture.Create<RepositoryVisibility>(), new List<string>(), default, default, default);

            gitHubClientMock.Repository.Get(gitRepo.Organisation, gitRepo.Name).Returns(repo);
            gitHubClientMock.PullRequest.Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewPullRequest>()).Returns(fixture.Create<PullRequest>());

            // Act
            var actualValue = await repository.CreatePullRequestAsync(gitRepo, "test", "New PR");

            // Assert
            Assert.That(actualValue, Is.True);
            await gitHubClientMock.Repository.Received().Get(Arg.Any<string>(), Arg.Any<string>());
            await gitHubClientMock.PullRequest.Received().Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NewPullRequest>());
        }

        [Test]
        public async Task CreateFileAsync_Success_Creates_File()
        {
            // Arrange
            var reference = new GitReference(default, default, default, default, "sha", default, default);
            var commit = new Commit(default, default, default, default, "sha", default, default, default, default, default, reference, Enumerable.Empty<GitReference>(), 0, default);
            var content = new RepositoryContentChangeSet(default, commit);
            var yamlContent = "property:\n - name: \"test\"";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            gitHubClientMock.Repository.Content.GetAllContentsByRef(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Throws(new Octokit.NotFoundException("File not found", System.Net.HttpStatusCode.NotFound));
            gitHubClientMock.Repository.Content.CreateFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CreateFileRequest>()).Returns(content);

            // Act
            var response = await repository.CreateFileAsync(gitRepo, "test", yamlContent);

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response, Is.EqualTo(commit.Sha));
            await gitHubClientMock.Repository.Content.Received().GetAllContentsByRef(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            await gitHubClientMock.Repository.Content.Received().CreateFile(gitRepo.Organisation, gitRepo.Name, "test", Arg.Any<CreateFileRequest>());
        }

        [Test]
        public async Task CreateFileAsync_Success_Skips_If_File_Exists()
        {
            // Arrange
            var yamlContent = "property:\n - name: \"test\"";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var files = CreateRepositoryContent(yamlContent);

            gitHubClientMock.Repository.Content.GetAllContentsByRef(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns([files]);

            // Act
            var response = await repository.CreateFileAsync(gitRepo, "test", yamlContent);

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response, Is.EqualTo(files.Sha));
            await gitHubClientMock.Repository.Content.Received().GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "test", gitRepo.Reference);
            await gitHubClientMock.Repository.Content.DidNotReceive().CreateFile(gitRepo.Organisation, gitRepo.Name, "test", Arg.Any<CreateFileRequest>());
        }

        [Test]
        public async Task UpdateFileAsync_Skip_FileNotFound_Test()
        {
            // Arrange
            var yamlContent = "property:\n - name: \"test\"";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };

            // Act
            await repository.UpdateFileAsync(gitRepo, "test", yamlContent);

            // Assert
            await gitHubClientMock.Repository.Content.DidNotReceive().UpdateFile(gitRepo.Organisation, gitRepo.Name, "test", Arg.Any<UpdateFileRequest>());
        }

        [Test]
        public async Task UpdateFileAsync_UpdateFile_Success_Test()
        {
            // Arrange
            var reference = new GitReference(default, default, default, default, "sha", default, default);
            var commit = new Commit(default, default, default, default, "sha", default, default, default, default, default, reference, Enumerable.Empty<GitReference>(), 0, default);
            var content = new RepositoryContentChangeSet(default, commit);
            var yamlContent = "property:\n - name: \"test\"";
            var gitRepo = new GitRepo { Name = "repo", Reference = "branch", Organisation = "org" };
            var files = CreateRepositoryContent(yamlContent);

            gitHubClientMock.Repository.Content.GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "test", gitRepo.Reference).Returns(new List<RepositoryContent>() { files });
            gitHubClientMock.Repository.Content.UpdateFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UpdateFileRequest>()).Returns(content);

            // Act
            var response = await repository.UpdateFileAsync(gitRepo, "test", yamlContent);

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response, Is.EqualTo(commit.Sha));
            await gitHubClientMock.Repository.Content.Received().GetAllContentsByRef(gitRepo.Organisation, gitRepo.Name, "test", gitRepo.Reference);
            await gitHubClientMock.Repository.Content.Received().UpdateFile(gitRepo.Organisation, gitRepo.Name, "test", Arg.Any<UpdateFileRequest>());
        }

        private static RepositoryContent CreateRepositoryContent(string content)
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var encodedContent = Convert.ToBase64String(contentBytes);
            return new RepositoryContent("name", "file", "sha", 0, ContentType.File, "downloadUrl", "url", "gitUrl", "htmlUrl", "base64", encodedContent, "target", "sub");
        }

        private static RepositoryContent CreateRepositoryDirectoryContent()
        {
            return new RepositoryContent("name", "file", "sha", 0, ContentType.Dir, "downloadUrl", "url", "gitUrl", "htmlUrl", "base64", null, "target", "sub");
        }

        public class TestType
        {
            public required List<TestPropertyObject> Property { get; set; }
        }

        public class TestPropertyObject
        {
            public required string Name { get; set; }
        }

        private static string ExistingTestYamlContent()
        {
            return @"apiVersion: helm.toolkit.fluxcd.io/v2beta1
kind: HelmRelease
metadata:
  name: adp-demo
  namespace: flux-config
spec:
  chart:
    spec:
      version: '66666 # {""$imagepolicy"": ""flux-config:adp-demo-01:tag""}'
  values:
    environment: production
    name: adp-demo
    namespace: adp-demo
    version: '2.1.0 # {""$imagepolicy"": ""flux-config:adp-demo-tst-01:tag""}'
    image: 'adp.azurecr.io/image/adp-demo:3.1.0 # {""$imagepolicy"": ""flux-config:adp-demo-01""}'
    image1: 'adp.azurecr.io/image/adp-demo:3.1.0 # {""$imagepolicy"": ""flux-config:adp-demo-01""}'
";
        }

        private static string GeneratedTestYamlContent()
        {
            return @"apiVersion: helm.toolkit.fluxcd.io/v2beta1
kind: HelmRelease
metadata:
  name: adp-demo
  namespace: flux-config
spec:
  chart:
    spec:
      version: '12345 # {""$imagepolicy"": ""flux-config:adp-demo-01:tag""}'
  values:
    environment: production
    name: adp-demo
    namespace: adp-demo
    version: '0.1.0 # {""$imagepolicy"": ""flux-config:adp-demo-tst-01:tag""}'
    image: 'adp.azurecr.io/image/adp-demo:0.1.0 # {""$imagepolicy"": ""flux-config:adp-demo-01""}'
    image1: 'adp.azurecr.io/image/adp-demo:0.1.0 # {""$imagepolicy"": ""flux-config:adp-demo-01""}'
";
        }
    }
}
