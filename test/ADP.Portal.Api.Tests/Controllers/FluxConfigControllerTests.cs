using System.Reflection;
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Mapster;
using ADP.Portal.Api.Models.Flux;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class FluxConfigControllerTests
    {
        private readonly FluxConfigController controller;
        private readonly ILogger<FluxConfigController> loggerMock;
        private readonly IOptions<TeamGitRepoConfig> teamGitRepoConfigMock;
        private readonly IOptions<AzureAdConfig> azureAdConfigMock;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfigMock;
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigServiceMock;
        private readonly Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
            MapsterEntitiesConfig.EntitiesConfigure(Substitute.For<IServiceCollection>());
        }

        public FluxConfigControllerTests()
        {
            teamGitRepoConfigMock = Substitute.For<IOptions<TeamGitRepoConfig>>();
            azureAdConfigMock = Substitute.For<IOptions<AzureAdConfig>>();
            fluxServicesGitRepoConfigMock = Substitute.For<IOptions<FluxServicesGitRepoConfig>>();
            loggerMock = Substitute.For<ILogger<FluxConfigController>>();
            gitOpsFluxTeamConfigServiceMock = Substitute.For<IGitOpsFluxTeamConfigService>();
            controller = new FluxConfigController(gitOpsFluxTeamConfigServiceMock, loggerMock, teamGitRepoConfigMock, azureAdConfigMock, fluxServicesGitRepoConfigMock);
            fixture = new Fixture();
        }

        [Test]
        public async Task GenerateAsync_Returns_Ok()
        {
            // Arrange
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GenerateFluxTeamConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateFluxConfigResult());

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }


        [Test]
        public async Task GenerateAsync_Returns_BadRequest_When_ConfigNotExists()
        {
            // Arrange
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GenerateFluxTeamConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateFluxConfigResult() { IsConfigExists = false, Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo($"Flux generator config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task GenerateAsync_Returns_BadRequest_When_CompletedWithErros()
        {
            // Arrange
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GenerateFluxTeamConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateFluxConfigResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreateConfigAsync_Returns_BadRequest()
        {
            // Arrange
            var request = fixture.Build<CreateFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.CreateFluxConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.CreateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreateConfigAsync_Returns_Ok()
        {
            // Arrange
            var request = fixture.Build<CreateFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.CreateFluxConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult());

            // Act
            var result = await controller.CreateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            if (result != null)
            {
                var results = (NoContentResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task UpdateConfigAsync_Returns_BadRequest_When_FileNotFound()
        {
            // Arrange
            var request = fixture.Build<CreateFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.UpdateFluxConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult() { IsConfigExists = false });

            // Act
            var result = await controller.UpdateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo($"Flux config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task UpdateConfigAsync_Returns_BadRequest_When_Errors()
        {
            // Arrange
            var request = fixture.Build<CreateFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.UpdateFluxConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.UpdateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task UpdateConfigAsync_Returns_Ok()
        {
            // Arrange
            var request = fixture.Build<CreateFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.UpdateFluxConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult());

            // Act
            var result = await controller.UpdateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            if (result != null)
            {
                var results = (NoContentResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task GetConfigAsync_Returns_Ok()
        {
            // Arrange
            var fluxTeam = fixture.Build<FluxTeamConfig>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GetFluxConfigAsync<FluxTeamConfig>(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(fluxTeam);

            // Act
            var result = await controller.GetConfigAsync("teamName");

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            if (result != null)
            {
                var okResults = (OkObjectResult)result;
                var fluxTeamConfig = okResults.Value as FluxTeamConfig;
                
                Assert.That(okResults, Is.Not.Null);
                Assert.That(fluxTeamConfig, Is.Not.Null);
                Assert.That(fluxTeamConfig?.Services.Count, Is.EqualTo(fluxTeam.Services.Count));
            }
        }
    }
}
