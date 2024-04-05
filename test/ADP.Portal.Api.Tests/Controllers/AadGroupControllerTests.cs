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
using System.Reflection;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class AadGroupControllerTests
    {
        private readonly AadGroupController controller;
        private readonly IOptions<AzureAdConfig> azureAdConfigMock;
        private readonly ILogger<AadGroupController> loggerMock;
        private readonly IOptions<TeamGitRepoConfig> adpTeamGitRepoConfigMock;
        private readonly IGitOpsGroupsConfigService gitOpsConfigServiceMock;
        private readonly Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        }

        public AadGroupControllerTests()
        {
            azureAdConfigMock = Substitute.For<IOptions<AzureAdConfig>>();
            adpTeamGitRepoConfigMock = Substitute.For<IOptions<TeamGitRepoConfig>>();
            loggerMock = Substitute.For<ILogger<AadGroupController>>();
            gitOpsConfigServiceMock = Substitute.For<IGitOpsGroupsConfigService>();
            controller = new AadGroupController(gitOpsConfigServiceMock, loggerMock, azureAdConfigMock, adpTeamGitRepoConfigMock);
            fixture = new Fixture();
        }

        [Test]
        public async Task SyncGroupsAsync_InvalidSyncConfigType_ReturnsBadRequest()
        {
            // Arrange

            // Act
            var result = await controller.SyncGroupsAsync("teamName", "invalidSyncConfigType");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SyncGroupsAsync_InvalidConfigType_ReturnsBadRequest()
        {

            // Act
            var result = await controller.SyncGroupsAsync("teamName", "ValidSyncConfigType");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }


        [TestCase("UserGroup")]
        [TestCase("AccessGroup")]
        [TestCase("OpenVpnGroup")]
        public async Task SyncGroupsAsync_ConfigNotFound_ReturnsBadRequest(string groupType)
        {
            // Arrange
            var groupSyncresult = new GroupSyncResult() { Errors = ["Config not found"], IsConfigExists = false };
            gitOpsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GroupType?>(), Arg.Any<GitRepo>())
                .Returns(groupSyncresult);

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.SyncGroupsAsync("teamName", groupType);

            // Assert
            var resultObject = (BadRequestObjectResult) result;
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var errors = resultObject.Value as List<string>;
            Assert.That(errors?.FirstOrDefault(), Is.EqualTo("Config not found"));
            
        }

        [TestCase("UserGroup")]
        [TestCase("AccessGroup")]
        [TestCase("OpenVpnGroup")]
        public async Task SyncGroupsAsync_ConfigExistsAndSyncHasErrors_ReturnsOk(string groupType)
        {
            // Arrange
            gitOpsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GroupType?>(), Arg.Any<GitRepo>())
                .Returns(new GroupSyncResult { Errors = ["Error"] });

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.SyncGroupsAsync("teamName", groupType);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TestCase("UserGroup")]
        [TestCase("AccessGroup")]
        [TestCase("OpenVpnGroup")]
        public async Task SyncGroupsAsync_ConfigExistsAndSyncHasNoErrors_ReturnsNoContent(string groupType)
        {
            // Arrange
            gitOpsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GroupType?>(), Arg.Any<GitRepo>())
                .Returns(new GroupSyncResult { Errors = new List<string>() });

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.SyncGroupsAsync("teamName", groupType);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task GetGroupsAsync_ReadGroups_ReturnsCollectionOfGroups()
        {
            // Arrange
            var groups = fixture.Build<Group>().CreateMany(2).ToList();
            gitOpsConfigServiceMock.GetGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GitRepo>()).Returns(groups);

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.GetGroupsConfigAsync("teamName");

            // Assert
            Assert.That(result, Is.Not.Null);
            if (result != null)
            {
                var okResults = (OkObjectResult)result;
                var retGroups = okResults.Value as List<Group>;

                Assert.That(okResults, Is.Not.Null);
                Assert.That(retGroups, Is.Not.Null);
                Assert.That(retGroups?.Count, Is.EqualTo(groups.Count));
            }
        }

        [Test]
        public async Task CreateGroupsConfigAsync_CreatesConfig_ReturnsCreated()
        {
            // Arrange
            var groups = fixture.Build<string>().CreateMany(2).ToList();
            gitOpsConfigServiceMock.CreateGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GitRepo>(), Arg.Any<IEnumerable<string>>()).Returns(new GroupConfigResult());
            gitOpsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null, Arg.Any<GitRepo>()).Returns(new GroupSyncResult());

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.CreateGroupsConfigAsync("teamName", new Models.Group.CreateGroupsConfigRequest { Members = groups });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That((CreatedResult)result, Is.Not.Null);
        }

        [Test]
        public async Task CreateGroupsConfigAsync_CreatesConfig_OnConfigSave_ReturnsBadRequest()
        {
            // Arrange
            var groups = fixture.Build<string>().CreateMany(2).ToList();
            gitOpsConfigServiceMock.CreateGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GitRepo>(), Arg.Any<IEnumerable<string>>())
                .Returns(new GroupConfigResult { Errors = ["Failed to save groups"] });

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.CreateGroupsConfigAsync("teamName", new Models.Group.CreateGroupsConfigRequest { Members = groups });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That((BadRequestObjectResult)result, Is.Not.Null);
        }

        [Test]
        public async Task CreateGroupsConfigAsync_CreatesConfig_OnGroupSync_ReturnsBadRequest()
        {
            // Arrange
            var groups = fixture.Build<string>().CreateMany(2).ToList();
            gitOpsConfigServiceMock.CreateGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GitRepo>(), Arg.Any<IEnumerable<string>>())
                .Returns(new GroupConfigResult());
            gitOpsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null, Arg.Any<GitRepo>())
                .Returns(new GroupSyncResult { Errors = ["Failed to save groups"] });

            adpTeamGitRepoConfigMock.Value.Returns(fixture.Create<TeamGitRepoConfig>());
            azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

            // Act
            var result = await controller.CreateGroupsConfigAsync("teamName", new Models.Group.CreateGroupsConfigRequest { Members = groups });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That((BadRequestObjectResult)result, Is.Not.Null);
        }
    }
}
