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
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class FluxTeamConfigControllerTests
    {
        private readonly FluxTeamConfigController controller;
        private readonly ILogger<FluxTeamConfigController> loggerMock;
        private readonly IOptions<TeamGitRepoConfig> teamGitRepoConfigMock;
        private readonly IOptions<AzureAdConfig> azureAdConfigMock;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfigMock;
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigServiceMock;
        private readonly Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
            MapsterConfig.Configure(Substitute.For<IServiceCollection>());
        }

        public FluxTeamConfigControllerTests()
        {
            teamGitRepoConfigMock = Substitute.For<IOptions<TeamGitRepoConfig>>();
            azureAdConfigMock = Substitute.For<IOptions<AzureAdConfig>>();
            fluxServicesGitRepoConfigMock = Substitute.For<IOptions<FluxServicesGitRepoConfig>>();
            loggerMock = Substitute.For<ILogger<FluxTeamConfigController>>();
            gitOpsFluxTeamConfigServiceMock = Substitute.For<IGitOpsFluxTeamConfigService>();
            controller = new FluxTeamConfigController(gitOpsFluxTeamConfigServiceMock, loggerMock, teamGitRepoConfigMock, azureAdConfigMock, fluxServicesGitRepoConfigMock);
            fixture = new Fixture();
        }

        [Test]
        public async Task GenerateAsync_Returns_Ok()
        {
            // Arrange
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GenerateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
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
            gitOpsFluxTeamConfigServiceMock.GenerateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
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
            gitOpsFluxTeamConfigServiceMock.GenerateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
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
            var request = fixture.Build<TeamFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.CreateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
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
        public async Task CreateConfigAsync_Returns_Created()
        {
            // Arrange
            var request = fixture.Build<TeamFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.CreateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult());

            // Act
            var result = await controller.CreateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            if (result != null)
            {
                var results = (CreatedResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task UpdateConfigAsync_Returns_BadRequest_When_FileNotFound()
        {
            // Arrange
            var request = fixture.Build<TeamFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.UpdateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
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
            var request = fixture.Build<TeamFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.UpdateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
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
            var request = fixture.Build<TeamFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.UpdateConfigAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
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
            gitOpsFluxTeamConfigServiceMock.GetConfigAsync<FluxTeamConfig>(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>())
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

        [Test]
        public async Task GetConfigAsync_Returns_NotFound_WhenFluxConfigNotFound()
        {
            // Arrange
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GetConfigAsync<FluxTeamConfig>(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>())
                .ReturnsNull();

            // Act
            var result = await controller.GetConfigAsync("teamName");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }


        [Test]
        public async Task CreateServiceAsync_Returns_BadRequest_When_Errors()
        {
            // Arrange
            var request = fixture.Build<ServiceFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.AddServiceAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<Core.Git.Entities.FluxService>())
                .Returns(new FluxConfigResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.CreateServiceAsync("teamName", request);

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
        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        public async Task CreateServiceAsync_Returns_Created(bool isFrontend, bool isHelmOnly)
        {
            // Arrange
            var request = fixture.Build<ServiceFluxConfigRequest>().Create();
            request.IsFrontend = isFrontend;
            request.IsHelmOnly = isHelmOnly;
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.AddServiceAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<Core.Git.Entities.FluxService>())
                .Returns(new FluxConfigResult());

            // Act
            var result = await controller.CreateServiceAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            if (result != null)
            {
                var results = (CreatedResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task CreateServiceAsync_Returns_BadRequest_When_TeamFlux_Not_Exists()
        {
            // Arrange
            var request = fixture.Build<ServiceFluxConfigRequest>().Create();
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.AddServiceAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<Core.Git.Entities.FluxService>())
                .Returns(new FluxConfigResult() { IsConfigExists = false });

            // Act
            var result = await controller.CreateServiceAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo("Flux config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Returns_Created_When_Successfully_Add_Config()
        {
            // Arrange
            gitOpsFluxTeamConfigServiceMock.AddServiceEnvironmentAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FluxEnvironment>())
                .Returns(new FluxConfigResult() { IsConfigExists = true });

            // Act
            var result = await controller.AddServiceEnvironmentAsync("teamName", "serviceName", "snd");

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            if (result != null)
            {
                var results = (CreatedResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Returns_BadRequest_When_Config_Not_Exists()
        {
            // Arrange
            gitOpsFluxTeamConfigServiceMock.AddServiceEnvironmentAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FluxEnvironment>())
                .Returns(new FluxConfigResult() { IsConfigExists = false, Errors = ["Flux config not found for the team:teamName"] });

            // Act
            var result = await controller.AddServiceEnvironmentAsync("teamName", "serviceName", "snd");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo("Flux config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task AddServiceEnvironmentAsync_Returns_BadRequest_When_Failed_To_Save()
        {
            // Arrange
            gitOpsFluxTeamConfigServiceMock.AddServiceEnvironmentAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FluxEnvironment>())
                .Returns(new FluxConfigResult() { IsConfigExists = true, Errors = ["Failed to save the config for the team: teamName"] });

            // Act
            var result = await controller.AddServiceEnvironmentAsync("teamName", "serviceName", "snd");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo(new List<string>() { "Failed to save the config for the team: teamName" }));
            }
        }
    }
}
