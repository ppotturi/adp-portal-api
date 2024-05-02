using System.Net;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Tests.Git.Services
{
    [TestFixture]
    public class GitOpsFluxTeamConfigServiceTests
    {
        private readonly GitOpsFluxTeamConfigService service;
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsFluxTeamConfigService> logger;
        private readonly Fixture fixture;

        public GitOpsFluxTeamConfigServiceTests()
        {
            gitOpsConfigRepository = Substitute.For<IGitOpsConfigRepository>();
            logger = Substitute.For<ILogger<GitOpsFluxTeamConfigService>>();
            service = new GitOpsFluxTeamConfigService(gitOpsConfigRepository, logger, Substitute.For<ISerializer>());
            fixture = new Fixture();
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_ShouldReturn_ConfigNotExists_WhenTeamConfig_NotFound(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Throws(new NotFoundException("Config not found", HttpStatusCode.NotFound));

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_ShouldReturn_ConfigNotExists_WhenTenantConfig_NotFound(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Throws(new NotFoundException("Config not found", HttpStatusCode.NotFound));

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_GetFluxTemplates_WhenConfig_Found(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            await gitOpsConfigRepository.Received(1).GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH);
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_DoNotRegerate_WhenService_NotFound(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns([]);

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GenerateFluxTeamConfig_RegerateConfig_Create_BranchPullRequest_AllServices_WhenTemplates_Found()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            var fluxServices = fixture.Build<FluxService>().CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1");

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_RegerateConfig_Create_BranchPullRequest_OneServices_WhenTemplates_Found(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateFluxTeamConfig_RegerateConfig_NoServices_NoTemplates_Found()
        {
            // Arrange
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fixture.Build<FluxTenant>().Create());

            // Act
            var result = await service.GenerateConfigAsync(fixture.Build<GitRepo>().Create(), fixture.Build<GitRepo>().Create(), "tenant1", "team1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        // TenantConfig null
        // Service with Database
        // Templates having ENV folder -> Files
        // Templates having patch.yaml file
        // Service Env in list of Tenant Env

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_ServiceAndEnvironmentTemplates_Found(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();

            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).CreateMany(1)
                                .Union(fixture.Build<FluxService>().CreateMany(1)).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>(FluxConstants.TEAM_ENV_FOLDER, x.Value));
            var templates_Services = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>($"{FluxConstants.SERVICE_FOLDER}/{x.Key}", x.Value));
            var templates_Service_Env = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>($"{FluxConstants.SERVICE_FOLDER}/{x.Key}/environment", x.Value));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates.Union(templates_Services).Union(templates_Service_Env));
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(commit);

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateFluxTeamConfig_BackendService_WithDatabase()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string serviceName = "service1";

            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Backend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = FluxConstants.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>($"flux/templates/programme/team/service/pre-deploy/{x.Key}", x.Value));
            var serviceEnvTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>("flux/templates/programme/team/service/pre-deploy-kustomize.yaml", x.Value));
            var resources = new Dictionary<object, object>() { { "resources", new List<object>() } };

            var teamEnvTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>("flux/templates/programme/team/environment/kustomization.yaml",
                                        x.Value.Union(resources).ToDictionary()));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates.Union(serviceEnvTemplates).Union(teamEnvTemplates));
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1", serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateFluxTeamConfig_RegerateConfig_Create_UpdateBranch_WhenTemplates_Found()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns(fixture.Build<Reference>().Create());
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().UpdateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.DidNotReceive().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.DidNotReceive().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task CreateFluxConfigAsync_ShouldCreate_NewFile()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            string serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(2).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            gitOpsConfigRepository.CreateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.CreateConfigAsync(gitRepo, teamName, fluxTeamConfig);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task CreateFluxConfigAsync_Create_NewFile_Failed()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            string serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(2).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            gitOpsConfigRepository.CreateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.CreateConfigAsync(gitRepo, teamName, fluxTeamConfig);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateFluxConfigAsync_ShouldUpdate_ExistingFile()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.UpdateConfigAsync(gitRepo, teamName, fluxTeamConfig);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task CreateFluxConfigAsync_Update_ExistingFile_Failed()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.UpdateConfigAsync(gitRepo, teamName, fluxTeamConfig);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateFluxConfigAsync_WhenConfig_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.UpdateConfigAsync(gitRepo, "team1", fluxTeamConfig);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddFluxServiceAsync_Should_Not_Add_When_TeamConfig_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var fluxService = fixture.Build<FluxService>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.AddServiceAsync(gitRepo, "team1", fluxService);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddFluxServiceAsync_Should_AddService_When_Service_Not_Exists()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxService = fixture.Build<FluxService>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceAsync(gitRepo, teamName, fluxService);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }


        [Test]
        public async Task AddFluxServiceAsync_Should_Not_AddService_When_Service_Exists()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            var fluxServices = fixture.Build<FluxService>().CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fluxServices)
                .Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceAsync(gitRepo, teamName, fluxServices[0]);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Is.EqualTo($"Service '{fluxServices[0].Name}' already exists in the team:'{teamName}'."));
        }

        [Test]
        public async Task AddFluxServiceAsync_Should_Return_Error_TeamConfig_Update_Failed()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxService = fixture.Build<FluxService>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.AddServiceAsync(gitRepo, teamName, fluxService);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Is.EqualTo($"Failed to save the config for the team: {teamName}"));
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_BackendService_UpdatePatchFiles(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();


            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Backend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = FluxConstants.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>($"flux/templates/programme/team/service/deploy/{envList[0].Name[..3]}/0{envList[0].Name[3..]}/patch.yaml", x.Value));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateFluxTeamConfig_FrontendService_UpdatePatchFiles(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();


            var envList = fixture.Build<FluxEnvironment>().With(x => x.ConfigVariables, default(List<FluxConfig>)).CreateMany(1).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Frontend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = FluxConstants.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>($"flux/templates/programme/team/service/infra/{envList[0].Name[..3]}/0{envList[0].Name[3..]}/patch.yaml", x.Value));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Should_Not_Add_When_TeamConfig_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var fluxEnvironment = fixture.Build<FluxEnvironment>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.AddServiceEnvironmentAsync(gitRepo, "team1", "service1", fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { "Flux team config not found for the team:'team1'." }));
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Should_Not_Add_When_ServiceConfig_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            string teamName = "team1";
            string serviceName = "service1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxEnvironment = fixture.Build<FluxEnvironment>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceEnvironmentAsync(gitRepo, teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { "Service 'service1' not found in the team:'team1'." }));
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Should_Add_When_Config_NotFound()
        {
            // Arrange
            string teamName = "team1";
            string serviceName = "service1";
            var gitRepo = fixture.Build<GitRepo>().Create();
            var fulxTeamServices = fixture.Build<FluxService>().With(i => i.Name, serviceName)
                .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();
            var fluxEnvironment = fixture.Build<FluxEnvironment>().Create();
            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceEnvironmentAsync(gitRepo, teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Should_Not_Add_When_Environment_Exists()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var envName = "devEnv";
            var gitRepo = fixture.Build<GitRepo>().Create();
            var fulxTeamServices = fixture.Build<FluxService>()
                .With(i => i.Name, serviceName)
                .With(i => i.Environments, fixture.Build<FluxEnvironment>().With(i => i.Name, envName).CreateMany(1).ToList())
                .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();
            var fluxEnvironment = fixture.Build<FluxEnvironment>()
                .With(i => i.Name, envName)
                .Create();
            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceEnvironmentAsync(gitRepo, teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddServiceEnvironmentAsyncc_Should_Return_Error_TeamConfig_Update_Failed()
        {
            // Arrange
            string teamName = "team1";
            string serviceName = "service1";
            var gitRepo = fixture.Build<GitRepo>().Create();
            var fulxTeamServices = fixture.Build<FluxService>().With(i => i.Name, serviceName)
                .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();
            var fluxEnvironment = fixture.Build<FluxEnvironment>().Create();
            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(FluxConstants.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.AddServiceEnvironmentAsync(gitRepo, teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { $"Failed to save the config for the team: {teamName}" }));
        }

        [Test]
        public async Task GenerateFluxTeamConfig_HelmOnlyService()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string serviceName = "helm-only-service";

            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>()
                                    .With(p => p.Name, serviceName)
                                    .With(e => e.Environments, envList)
                                    .With(x => x.Type, FluxServiceType.HelmOnly)
                                    .CreateMany(1)
                                    .ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2)
                .Select((x, index) =>
                {
                    return index switch
                    {
                        0 => new KeyValuePair<string, Dictionary<object, object>>($"flux/templates/programme/team/service/deploy/{x.Key}", x.Value),
                        1 => new KeyValuePair<string, Dictionary<object, object>>($"flux/templates/programme/team/service/kustomization.yaml", new Dictionary<object, object>() { { "resources", new List<string>() { "infra-kustomize.yaml", "deploy-kustomize.yaml" } } }),
                        _ => new KeyValuePair<string, Dictionary<object, object>>($"flux/templates/programme/team/service/deploy/{x.Key}", x.Value),
                    };
                });

            var serviceEnvTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>("flux/templates/programme/team/service/deploy-kustomize.yaml", new Dictionary<object, object>() { { "spec", new Dictionary<object, object>() { { "dependsOn", new object() } } } }));
            var resources = new Dictionary<object, object>() { { "resources", new List<object>() } };

            var teamEnvTemplates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, Dictionary<object, object>>("flux/templates/programme/team/environment/kustomization.yaml",
                                        x.Value.Union(resources).ToDictionary()));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates.Union(serviceEnvTemplates).Union(teamEnvTemplates));
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateConfigAsync(gitRepo, gitRepoFluxServices, "tenant1", "team1", serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
