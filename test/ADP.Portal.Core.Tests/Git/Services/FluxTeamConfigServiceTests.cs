using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using System.Collections.Generic;
using System.Net;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Tests.Git.Services
{
    [TestFixture]
    public class FluxTeamConfigServiceTests
    {
        private FluxTeamConfigService service = null!;
        private IGitHubRepository gitOpsConfigRepository = null!;
        private ILogger<FluxTeamConfigService> logger = null!;
        private IFluxTemplateService fluxTemplateService = null!;
        private readonly IOptionsSnapshot<GitRepo> gitRepoOptions = Substitute.For<IOptionsSnapshot<GitRepo>>();
        private GitRepo teamRepo = null!;
        private GitRepo fluxServicesRepo = null!;
        private GitRepo fluxTemplateRepo = null!;
        private readonly Fixture fixture = new();

        [SetUp]
        public void Setup()
        {
            gitOpsConfigRepository = Substitute.For<IGitHubRepository>();
            logger = Substitute.For<ILogger<FluxTeamConfigService>>();
            fluxTemplateService = Substitute.For<IFluxTemplateService>();

            var  templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(20).ToList();
            fluxTemplateService.GetFluxTemplatesAsync().Returns(templates);
            teamRepo = fixture.Build<GitRepo>().Create();
            fluxServicesRepo = fixture.Build<GitRepo>().Create();
            fluxTemplateRepo = fixture.Build<GitRepo>().Create();

            gitRepoOptions.Get(Constants.GitRepo.TEAM_REPO_CONFIG).Returns(teamRepo);
            gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_SERVICES_CONFIG).Returns(fluxServicesRepo);
            gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_TEMPLATES_CONFIG).Returns(fluxTemplateRepo);

            service = new FluxTeamConfigService(gitOpsConfigRepository, gitRepoOptions, fluxTemplateService, logger, Substitute.For<ISerializer>());
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_ShouldReturn_ConfigNotExists_WhenTeamConfig_NotFound(string? serviceName, string? environment)
        {
            // Arrange
            var tenantName = "tenant1";
            var teamName = "team1";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(default(FluxTeamConfig));

            // Act
            var result = await service.GenerateManifestAsync(tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_ShouldReturn_ConfigNotExists_WhenTenantConfig_NotFound(string? serviceName, string? environment)
        {
            // Arrange
            var tenantName = "tenant1";
            var teamName = "team1";

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(default(FluxTenant));

            // Act
            var result = await service.GenerateManifestAsync(tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_GetFluxTemplates_WhenConfig_Found(string? serviceName, string? environment)
        {
            // Arrange
            var tenantName = "tenant1";
            var teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);

            // Act
            var result = await service.GenerateManifestAsync(tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            await fluxTemplateService.Received(1).GetFluxTemplatesAsync();
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_DoNotRegerate_WhenService_NotFound(string? serviceName, string? environment)
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var tenantName = "tenant1";
            var teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns([]);

            // Act
            var result = await service.GenerateManifestAsync(tenantName, teamName, serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GenerateManifest_RegerateConfig_Create_BranchPullRequest_AllServices_WhenTemplates_Found()
        {
            // Arrange
            var fluxServices = fixture.Build<FluxService>().CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(Arg.Any<GitRepo>(), Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(Arg.Any<GitRepo>(), Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1");

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_RegerateConfig_Create_BranchPullRequest_OneServices_WhenTemplates_Found(string? serviceName, string? environment)
        {
            // Arrange

            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateManifest_RegerateConfig_NoServices_NoTemplates_Found()
        {
            // Arrange
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fixture.Build<FluxTenant>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_ServiceAndEnvironmentTemplates_Found(string? serviceName, string? environment)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).CreateMany(1)
                                .Union(fixture.Build<FluxService>().CreateMany(1)).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>(Constants.Flux.Templates.TEAM_ENV_FOLDER, x.Value));
            var templates_Services = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>($"{Constants.Flux.Templates.SERVICE_FOLDER}/{x.Key}", x.Value));
            var templates_Service_Env = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>($"{Constants.Flux.Templates.SERVICE_FOLDER}/{x.Key}/environment", x.Value));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates.Union(templates_Services).Union(templates_Service_Env));
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(commit);

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "templates/programme/team/service/kustomization.yaml", true)]
        [TestCase("service1", "templates/programme/team/service/kustomization.yaml", false)]
        public async Task GenerateManifest_UpdateServiceKustomizationFiles_ForListObjects(string serviceName, string template, bool helmOnly)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName)
                                                            .With(t => t.Type, helmOnly ? FluxServiceType.HelmOnly : FluxServiceType.Backend)
                                                            .With(c => c.ConfigVariables, value: [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "datastore1" }])
                                                            .With(e => e.Environments, envList).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();

            var templateValue = new Dictionary<object, object>
            {
                { Constants.Flux.Templates.RESOURCES_KEY, new List<string>() }
            };
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>(template, new FluxTemplateFile(templateValue)));


            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(commit);

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, "env1");

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "templates/programme/team/service/kustomization.yaml", true)]
        [TestCase("service1", "templates/programme/team/service/kustomization.yaml", false)]
        [TestCase("service1", "templates/programme/team/service/deploy-kustomize.yaml", true)]
        public void GenerateManifest_UpdateServiceKustomizationFiles_ForListObjects_ThrowsError(string serviceName, string template, bool helmOnly)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName)
                                                            .With(t => t.Type, helmOnly ? FluxServiceType.HelmOnly : FluxServiceType.Backend)
                                                            .With(c => c.ConfigVariables, value: helmOnly ? [] : [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "datastore1" }])
                                                            .With(e => e.Environments, envList).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();

            var templateValue = new Dictionary<object, object>
            {
                { Constants.Flux.Templates.RESOURCES_KEY, "" }
            };
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>(template, new FluxTemplateFile(templateValue)));

            // Act
            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            fluxTemplateService.GetFluxTemplatesAsync().Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(commit);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GenerateManifestAsync("tenant1", "team1", serviceName, "env1"));
        }

        [Test]
        [TestCase("service1", "templates/programme/team/service/deploy-kustomize.yaml", true)]
        public async Task GenerateManifest_UpdateServiceKustomizationFiles_ForDictionaryObjects(string? serviceName, string template, bool helmOnly)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName)
                                                            .With(t => t.Type, helmOnly ? FluxServiceType.HelmOnly : FluxServiceType.Backend)
                                                            .With(c => c.ConfigVariables, value: [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "datastore1" }])
                                                            .With(e => e.Environments, envList).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();

            var templateValue = new Dictionary<object, object>
            {
                { Constants.Flux.Templates.RESOURCES_KEY, new Dictionary<object, object>() }
            };
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>(template, new FluxTemplateFile(templateValue)));


            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(commit);

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, "env1");

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateManifest_BackendService_WithDatabase()
        {
            // Arrange
            var serviceName = "service1";

            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Backend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList.Select(x => new FluxEnvironment { Manifest = x.Manifest, Name = x.Name }).ToList()).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>($"flux/templates/programme/team/service/pre-deploy/{x.Key}", x.Value));
            var serviceEnvTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>("flux/templates/programme/team/service/pre-deploy-kustomize.yaml", x.Value));
            var resources = new Dictionary<object, object> { { "resources", new List<object>() } };

            var teamEnvTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>("flux/templates/programme/team/environment/kustomization.yaml",
                                        new FluxTemplateFile(x.Value.Content.Union(resources).ToDictionary())));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates.Union(serviceEnvTemplates).Union(teamEnvTemplates));
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateManifest_RegerateConfig_Create_UpdateBranch_WhenTemplates_Found()
        {
            // Arrange
            var tenantName = "tenant1";
            var teamName = "team1";
            var serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns(fixture.Build<Reference>().Create());
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync(tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().UpdateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.DidNotReceive().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.DidNotReceive().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GenerateManifest_RegerateConfig_Nochanges()
        {
            // Arrange
            var tenantName = "tenant1";
            var teamName = "team1";
            var serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns(fixture.Build<Reference>().Create());
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>()).Returns(default(Commit));

            // Act
            var result = await service.GenerateManifestAsync(tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task GenerateManifest_Should_Merge_EnvServicesManifests(bool configExists)
        {
            // Arrange
            var serviceName = "service1";

            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Backend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();

            var envPath = $"{envList[0].Name[..3]}/0{envList[0].Name[3..]}";
            var content = new FluxTemplateFile(new Dictionary<object, object> { { Constants.Flux.Templates.RESOURCES_KEY, new List<object>() } });


            var envServicesKustomization = string.Format(Constants.Flux.Services.TEAM_SERVICE_ENV_KUSTOMIZATION_FILE, fluxTeamConfig.ProgrammeName, fluxTeamConfig.TeamName, envPath);
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>(string.Format("{0}/{1}/{2}/kustomization.yaml", fluxTeamConfig.ProgrammeName, fluxTeamConfig.TeamName, envPath), content));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetConfigAsync<Dictionary<object, object>>(Arg.Is(envServicesKustomization), Arg.Any<GitRepo>()).Returns(configExists ? content.Content : null);
            fluxTemplateService.GetFluxTemplatesAsync().Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName);
            serviceTemplates.First().Value.Content.TryGetValue(Constants.Flux.Templates.RESOURCES_KEY, out var resources);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            if (configExists && resources != null)
            {
                Assert.That(((List<object>)resources)[0], Is.EqualTo($"../../{serviceName}"));
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task GenerateManifest_Should_Merge_EnvTeamsManifests(bool configExists)
        {
            // Arrange
            var serviceName = "service1";

            var envList = fixture.Build<FluxEnvironment>().CreateMany(2).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Backend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();

            var env = envList[0].Name[..3];
            var content = new FluxTemplateFile(new Dictionary<object, object> { { Constants.Flux.Templates.RESOURCES_KEY, new List<object>() } });

            var envServicesKustomization = string.Format(Constants.Flux.Services.TEAM_ENV_BASE_KUSTOMIZATION_FILE, env);
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>(string.Format("environments/{0}/base/kustomization.yaml", env), content));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetConfigAsync<Dictionary<object, object>>(Arg.Is(envServicesKustomization), Arg.Any<GitRepo>()).Returns(configExists ? content.Content : null);
            gitOpsConfigRepository.GetAllFilesAsync(fluxTemplateRepo, Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(fluxServicesRepo, Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName);
            serviceTemplates.First().Value.Content.TryGetValue(Constants.Flux.Templates.RESOURCES_KEY, out var resources);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(fluxServicesRepo, Arg.Any<string>(), Arg.Any<string>());
            if (configExists && resources != null)
            {
                Assert.That(((List<object>)resources)[0], Is.EqualTo($"../../../{fluxTeamConfig.ProgrammeName}/{fluxTeamConfig.TeamName}/base/patch"));
            }
        }

        [Test]
        public async Task CreateFluxConfigAsync_ShouldCreate_NewFile()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(2).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            gitOpsConfigRepository.CreateConfigAsync(Arg.Any<GitRepo>(), string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.CreateConfigAsync(teamName, fluxTeamConfig);

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
            var teamName = "team1";
            var serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(2).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            gitOpsConfigRepository.CreateConfigAsync(gitRepo, string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.CreateConfigAsync(teamName, fluxTeamConfig);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task AddServiceAsync_Should_Not_Add_When_TeamConfig_NotFound()
        {
            // Arrange
            var fluxService = fixture.Build<FluxService>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.AddServiceAsync("team1", fluxService);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        [TestCase(FluxServiceType.Frontend)]
        [TestCase(FluxServiceType.Backend)]
        public async Task AddServiceAsync_Should_AddService_When_Service_Not_Exists(FluxServiceType type)
        {
            // Arrange
            var teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxService = fixture.Build<FluxService>()
                .With(x => x.Type, type)
                .With(x => x.ConfigVariables, [])
                .Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceAsync(teamName, fluxService);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));

            if(type == FluxServiceType.Frontend)
            {
                Assert.That(fluxService.ConfigVariables[0].Key, Is.EqualTo(Constants.Flux.Templates.INGRESS_ENDPOINT_TOKEN_KEY));
                Assert.That(fluxService.ConfigVariables[0].Value, Is.EqualTo(fluxService.Name));
            }
        }


        [Test]
        public async Task AddServiceAsync_Should_Not_AddService_When_Service_Exists()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var teamName = "team1";
            var fluxServices = fixture.Build<FluxService>().CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fluxServices)
                .Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(gitRepo, string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceAsync(teamName, fluxServices[0]);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Is.EqualTo($"Service '{fluxServices[0].Name}' already exists in the team:'{teamName}'."));
        }

        [Test]
        public async Task AddServiceAsync_Should_Return_Error_TeamConfig_Update_Failed()
        {
            // Arrange
            var teamName = "team1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxService = fixture.Build<FluxService>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.AddServiceAsync(teamName, fluxService);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Is.EqualTo($"Failed to save the config for the team: {teamName}"));
        }

        [Test]
        [TestCase("service1", "dev")]
        public async Task GenerateManifest_BackendService_UpdatePatchFiles_Deploy(string? serviceName, string? environment)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().With(x => x.Name, environment).CreateMany(1).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Backend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var file = new Dictionary<object, object>
            {
                {
                    Constants.Flux.Templates.SPEC_KEY, new Dictionary<object, object>
                    {
                        {
                            Constants.Flux.Templates.VALUES_KEY, new Dictionary<object, object>
                            {
                                { Constants.Flux.Templates.LABELS_KEY, "labels" },
                                { Constants.Flux.Templates.INGRESS_KEY, "ingress" }
                            }
                        }
                    }
                }
            };
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>($"templates/programme/team/service/deploy/{envList[0].Name[..3]}/0{envList[0].Name[3..]}/patch.yaml", new FluxTemplateFile(file)));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(Arg.Any<GitRepo>(), Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(Arg.Any<GitRepo>(), Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "dev")]
        public async Task GenerateManifest_BackendService_UpdatePatchFiles_Infra(string? serviceName, string? environment)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().With(x => x.Name, environment).CreateMany(1).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Frontend)
                                    .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var file = new Dictionary<object, object>
            {
                {
                    Constants.Flux.Templates.SPEC_KEY, new Dictionary<object, object>
                    {
                        {
                            Constants.Flux.Templates.VALUES_KEY, new Dictionary<object, object>
                            {
                                { Constants.Flux.Templates.POSTGRESRESOURCEGROUPNAME_KEY, "group" },
                                { Constants.Flux.Templates.POSTGRESSERVERNAME_KEY, "server" }
                            }
                        }
                    }
                }
            };
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>($"templates/programme/team/service/infra/{envList[0].Name[..3]}/0{envList[0].Name[3..]}/patch.yaml", new FluxTemplateFile(file)));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(Arg.Any<GitRepo>(), Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(Arg.Any<GitRepo>(), Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        [TestCase("service1", "dev")]
        [TestCase("service1", null)]
        [TestCase(null, null)]
        public async Task GenerateManifest_FrontendService_UpdatePatchFiles(string? serviceName, string? environment)
        {
            // Arrange
            var envList = fixture.Build<FluxEnvironment>().With(x => x.ConfigVariables, default(List<FluxConfig>)).CreateMany(1).ToList();
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).With(e => e.Environments, envList).With(x => x.Type, FluxServiceType.Frontend)
                                    .With(x => x.ConfigVariables, [new FluxConfig { Key = Constants.Flux.Templates.POSTGRES_DB_KEY, Value = "db" }]).CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(1)
                .Select(x => new KeyValuePair<string, FluxTemplateFile>($"flux/templates/programme/team/service/infra/{envList[0].Name[..3]}/0{envList[0].Name[3..]}/patch.yaml", x.Value));

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(Arg.Any<GitRepo>(), Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(Arg.Any<GitRepo>(), Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName, environment);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task AddServiceEnvironment_Should_Not_Add_When_TeamConfig_NotFound()
        {
            // Arrange
            var fluxEnvironment = "dev";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.AddServiceEnvironmentAsync("team1", "service1", fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { "Flux team config not found for the team:'team1'." }));
        }

        [Test]
        public async Task AddServiceEnvironment_Should_Not_Add_When_ServiceConfig_NotFound()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxEnvironment = "dev";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceEnvironmentAsync(teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { "Service 'service1' not found in the team:'team1'." }));
        }

        [Test]
        public async Task AddServiceEnvironment_Should_Add_When_Config_NotFound()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxEnvironment = "dev";
            var fulxTeamServices = fixture.Build<FluxService>().With(i => i.Name, serviceName)
                .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();


            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceEnvironmentAsync(teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddServiceEnvironment_Should_Not_Add_When_Environment_Exists()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var envName = "devEnv";
            var fulxTeamServices = fixture.Build<FluxService>()
                .With(i => i.Name, serviceName)
                .With(i => i.Environments, fixture.Build<FluxEnvironment>().With(i => i.Name, envName).CreateMany(1).ToList())
                .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();
            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns("sha");

            // Act
            var result = await service.AddServiceEnvironmentAsync(teamName, serviceName, envName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddServiceEnvironment_Should_Return_Error_TeamConfig_Update_Failed()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxEnvironment = "dev";
            var fulxTeamServices = fixture.Build<FluxService>().With(i => i.Name, serviceName)
                .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), string.Format(Constants.Flux.Templates.GIT_REPO_TEAM_CONFIG_PATH, teamName), Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await service.AddServiceEnvironmentAsync(teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { $"Failed to save the config for the team: {teamName}" }));
        }

        [Test]
        public async Task GenerateManifest_HelmOnlyService()
        {
            // Arrange
            string serviceName = "helm-only-service";
            var envList = fixture.Build<FluxEnvironment>().With(x => x.ConfigVariables, default(List<FluxConfig>)).CreateMany(1).ToList();
            var fluxServices = fixture.Build<FluxService>()
                                    .With(p => p.Name, serviceName)
                                    .With(e => e.Environments, envList)
                                    .With(x => x.Type, FluxServiceType.HelmOnly)
                                    .CreateMany(1)
                                    .ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().With(p => p.Services, fluxServices).Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().With(x => x.Environments, envList).Create();
            var serviceTemplates = fixture.Build<KeyValuePair<string, FluxTemplateFile>>().CreateMany(2)
                .Select((x, index) =>
                {
                    return index switch
                    {
                        0 => new KeyValuePair<string, FluxTemplateFile>($"flux/templates/programme/team/service/deploy/{x.Key}", x.Value),
                        1 => new KeyValuePair<string, FluxTemplateFile>($"flux/templates/programme/team/service/kustomization.yaml", new FluxTemplateFile(new Dictionary<object, object>() { { "resources", new List<string>() { "infra-kustomize.yaml", "deploy-kustomize.yaml" } } })),
                        _ => new KeyValuePair<string, FluxTemplateFile>($"flux/templates/programme/team/service/deploy/{x.Key}", x.Value),
                    };
                });

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTenantConfig);
            gitOpsConfigRepository.GetAllFilesAsync(Arg.Any<GitRepo>(), Constants.Flux.Templates.GIT_REPO_TEMPLATE_PATH).Returns(serviceTemplates);
            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);
            gitOpsConfigRepository.CreateCommitAsync(Arg.Any<GitRepo>(), Arg.Any<Dictionary<string, FluxTemplateFile>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fixture.Build<Commit>().Create());

            // Act
            var result = await service.GenerateManifestAsync("tenant1", "team1", serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            await gitOpsConfigRepository.Received().CreateBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.Received().CreatePullRequestAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task GetServiceEnvironment_Should_Not_Return_Environment_When_TeamConfig_NotFound()
        {
            // Arrange
            var fluxEnvironment = "dev";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.GetServiceEnvironmentAsync("team1", "service1", fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Environment, Is.Null);
        }

        [Test]
        public async Task GetServiceEnvironment_Should_Not_Return_Environment_When_ServiceConfig_NotFound()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxEnvironment = "dev";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);

            // Act
            var result = await service.GetServiceEnvironmentAsync(teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Environment, Is.Null);
        }

        [Test]
        public async Task GetServiceEnvironment_Should_Not_Return_When_EnvironmentConfig_NotFound()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxEnvironment = "dev";
            var fulxTeamServices = fixture.Build<FluxService>().With(i => i.Name, serviceName)
              .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();


            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);

            // Act
            var result = await service.GetServiceEnvironmentAsync(teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
            Assert.That(result.Environment, Is.Null);
        }

        [Test]
        public async Task GetServiceEnvironment_Should_Return_When_Config_Found()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxEnvironment = "dev";

            var fulxTeamServices = fixture.Build<FluxService>()
                .With(i => i.Name, serviceName)
                .With(i => i.Environments, fixture.Build<FluxEnvironment>().With(i => i.Name, fluxEnvironment).CreateMany(1).ToList())
                .CreateMany(1).ToList();

            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();


            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);

            // Act
            var result = await service.GetServiceEnvironmentAsync(teamName, serviceName, fluxEnvironment);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Environment, Is.Not.Null);
        }

        [Test]
        public async Task UpdateServiceEnvironmentManifest_Should_Not_Return_Environment_When_TeamConfig_NotFound()
        {
            // Arrange
            var fluxEnvironment = "dev";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(default(FluxTeamConfig));

            // Act
            var result = await service.UpdateServiceEnvironmentManifestAsync("team1", "service1", fluxEnvironment, true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        public async Task UpdateServiceEnvironmentManifest_Should_Not_Return_Environment_When_ServiceConfig_NotFound()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxEnvironment = "dev";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);

            // Act
            var result = await service.UpdateServiceEnvironmentManifestAsync(teamName, serviceName, fluxEnvironment, true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        public async Task UpdateServiceEnvironmentManifest_Should_Not_Return_When_EnvironmentConfig_NotFound()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var fluxEnvironment = "dev";
            var fulxTeamServices = fixture.Build<FluxService>().With(i => i.Name, serviceName)
              .CreateMany(1).ToList();
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();


            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);

            // Act
            var result = await service.UpdateServiceEnvironmentManifestAsync(teamName, serviceName, fluxEnvironment, true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        public async Task UpdateServiceEnvironmentManifest_Should_Update_Manifest_When_Config_Found()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var environment = "env1";
            var generate = true;


            var fulxTeamServices = fixture.Build<FluxService>()
                .With(i => i.Name, serviceName)
                .With(i => i.Environments, fixture.Build<FluxEnvironment>().With(i => i.Name, environment).CreateMany(1).ToList())
                .CreateMany(1).ToList();

            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns("response");

            // Act
            var result = await service.UpdateServiceEnvironmentManifestAsync(teamName, serviceName, environment, generate);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
        }


        [Test]
        public async Task UpdateServiceEnvironmentManifest_Should_Return_Error_When_Failed_To_Save_Config_Found()
        {
            // Arrange
            var teamName = "team1";
            var serviceName = "service1";
            var environment = "env1";
            var generate = true;

            var fulxTeamServices = fixture.Build<FluxService>()
                .With(i => i.Name, serviceName)
                .With(i => i.Environments, fixture.Build<FluxEnvironment>().With(i => i.Name, environment).CreateMany(1).ToList())
                .CreateMany(1).ToList();

            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(c => c.Services, fulxTeamServices)
                .Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(fluxTeamConfig);
            gitOpsConfigRepository.UpdateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns("");

            // Act
            var result = await service.UpdateServiceEnvironmentManifestAsync(teamName, serviceName, environment, generate);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors, Is.EqualTo(new List<string>() { $"Failed to save the config for the team: {teamName}" }));
        }
    }
}
