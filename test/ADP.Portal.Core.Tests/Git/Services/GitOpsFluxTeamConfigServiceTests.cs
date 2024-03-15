using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using System.Net;

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
            service = new GitOpsFluxTeamConfigService(gitOpsConfigRepository, logger);
            fixture = new Fixture();
        }


        [Test]
        public async Task GenerateFluxTeamConfig_ShouldReturn_ConfigNotExists_WhenTeamConfig_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Throws(new NotFoundException("Config not found", HttpStatusCode.NotFound));

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }


        [Test]
        public async Task GenerateFluxTeamConfig_ShouldReturn_ConfigNotExists_WhenTenantConfig_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Throws(new NotFoundException("Config not found", HttpStatusCode.NotFound));

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.False);
        }

        [Test]
        public async Task GenerateFluxTeamConfig_GetFluxTemplates_WhenConfig_Found()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTeamConfig);

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTenantConfig);

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            await gitOpsConfigRepository.Received(1).GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH);
        }

        [Test]
        public async Task GenerateFluxTeamConfig_DoNotRegerateConfig_WhenTemplates_NotFound()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";
            var fluxTeamConfig = fixture.Build<FluxTeamConfig>().Create();
            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTeamConfig);

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTenantConfig);

            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns([]);

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsConfigExists, Is.True);
            Assert.That(result.Errors.Count,Is.EqualTo(0));
        }


        [Test]
        public async Task GenerateFluxTeamConfig_RegerateConfig_Create_BranchPullRequest_WhenTemplates_Found()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p=>p.Name, serviceName).CreateMany(1).ToList();

            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(p=>p.Services, fluxServices)
                .Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTeamConfig);

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTenantConfig);

            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates);

            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns((Reference?)default);

            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(),Arg.Any<string>())
                .Returns(commit);

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

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

            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(p => p.Services, fluxServices)
                .Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTeamConfig);

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTenantConfig);

            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates);

            gitOpsConfigRepository.GetBranchAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns(fixture.Build<Reference>().Create());

            var commit = fixture.Build<Commit>().Create();
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(commit);

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);

            await gitOpsConfigRepository.Received().UpdateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.DidNotReceive().CreateBranchAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());
            await gitOpsConfigRepository.DidNotReceive().CreatePullRequestAsync(gitRepoFluxServices, Arg.Any<string>(), Arg.Any<string>());

        }

        [Test]
        public async Task GenerateFluxTeamConfig_Return_Error_WhenNoChanged_InGeneratedFiles()
        {
            // Arrange
            var gitRepo = fixture.Build<GitRepo>().Create();
            var gitRepoFluxServices = fixture.Build<GitRepo>().Create();
            string tenantName = "tenant1";
            string teamName = "team1";
            string serviceName = "service1";
            var fluxServices = fixture.Build<FluxService>().With(p => p.Name, serviceName).CreateMany(1).ToList();

            var fluxTeamConfig = fixture.Build<FluxTeamConfig>()
                .With(p => p.Services, fluxServices)
                .Create();

            var fluxTenantConfig = fixture.Build<FluxTenant>().Create();
            var templates = fixture.Build<KeyValuePair<string, Dictionary<object, object>>>().CreateMany(2).AsEnumerable();

            gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTeamConfig);

            gitOpsConfigRepository.GetConfigAsync<FluxTenant>(Arg.Any<string>(), Arg.Any<GitRepo>())
                .Returns(fluxTenantConfig);

            gitOpsConfigRepository.GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH).Returns(templates);
            
            gitOpsConfigRepository.CreateCommitAsync(gitRepoFluxServices, Arg.Any<Dictionary<string, Dictionary<object, object>>>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Commit?)null);

            // Act
            var result = await service.GenerateFluxTeamConfigAsync(gitRepo, gitRepoFluxServices, tenantName, teamName, serviceName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Is.EqualTo($"No changes found in the flux files for the team:'{teamName}' and service:{serviceName}."));
        }
    }
}
