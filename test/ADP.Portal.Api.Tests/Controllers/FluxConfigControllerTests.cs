using System.Reflection;
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Mapster;
using Microsoft.AspNetCore.Mvc;
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
        public async Task GenerateAsync_Returns_OkRequest()
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
            Assert.That(result, Is.InstanceOf<OkResult>());
        }


        [Test]
        public async Task GenerateAsync_Returns_BadRequest_When_ConfigNotExists()
        {
            // Arrange
            teamGitRepoConfigMock.Value.Returns(fixture.Build<TeamGitRepoConfig>().Create());
            fluxServicesGitRepoConfigMock.Value.Returns(fixture.Build<FluxServicesGitRepoConfig>().Create());
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            gitOpsFluxTeamConfigServiceMock.GenerateFluxTeamConfigAsync(Arg.Any<GitRepo>(), Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateFluxConfigResult() {  IsConfigExists =false, Errors= ["Flux team config not found"] } );

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo($"Flux generator config not for the team:teamName"));
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
    }
}
