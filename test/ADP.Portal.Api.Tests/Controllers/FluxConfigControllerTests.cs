using System.Reflection;
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
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
        private readonly IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfigMock;
        private readonly IGitOpsConfigService gitOpsConfigServiceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public FluxConfigControllerTests()
        {
            adpTeamGitRepoConfigMock = Substitute.For<IOptions<AdpTeamGitRepoConfig>>();
            loggerMock = Substitute.For<ILogger<FluxConfigController>>();
            gitOpsConfigServiceMock = Substitute.For<IGitOpsConfigService>();
            controller = new FluxConfigController(gitOpsConfigServiceMock, loggerMock, adpTeamGitRepoConfigMock);
        }

        [Test]
        public async Task SyncGroupsAsync_ConfigDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            gitOpsConfigServiceMock.IsConfigExistsAsync(Arg.Any<string>(), Arg.Any<ConfigType>(), Arg.Any<GitRepo>()).Returns(false);

            // Act
            var result = await controller.GenerateTeamConfigAsync("teamName", string.Empty);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
    }
}
