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
            await gitOpsConfigRepository.Received(1).GetAllFilesAsync(gitRepo, FluxConstants.GIT_REPO_TEMPLATE_PATH);
        }
    }
}
