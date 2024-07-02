using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Models.Group;
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

namespace ADP.Portal.Api.Tests.Controllers;

[TestFixture]
public class AadGroupControllerTests
{
    private readonly AadGroupController controller;
    private readonly IOptions<AzureAdConfig> azureAdConfigMock;
    private readonly ILogger<AadGroupController> loggerMock;
    private readonly IGroupsConfigService groupsConfigServiceMock;
    private readonly Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

    }

    public AadGroupControllerTests()
    {
        azureAdConfigMock = Substitute.For<IOptions<AzureAdConfig>>();
        loggerMock = Substitute.For<ILogger<AadGroupController>>();
        groupsConfigServiceMock = Substitute.For<IGroupsConfigService>();
        controller = new AadGroupController(groupsConfigServiceMock, loggerMock, azureAdConfigMock);
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
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GroupType?>())
            .Returns(groupSyncresult);

        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.SyncGroupsAsync("teamName", groupType);

        // Assert
        var resultObject = (BadRequestObjectResult)result;
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
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GroupType?>())
            .Returns(new GroupSyncResult { Errors = ["Error"] });

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
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<GroupType?>())
            .Returns(new GroupSyncResult { Errors = new List<string>() });

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
        groupsConfigServiceMock.GetGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(groups);

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
        groupsConfigServiceMock.CreateGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                        Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(),
                                                        Arg.Any<IEnumerable<string>>()).Returns(new GroupConfigResult());
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null).Returns(new GroupSyncResult());

        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.CreateGroupsConfigAsync("teamName", new CreateGroupsConfigRequest
        {
            AdminMembers = groups,
            NonTechUserMembers = groups,
            TechUserMembers = groups
        });

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That((NoContentResult)result, Is.Not.Null);
    }

    [Test]
    public async Task CreateGroupsConfigAsync_CreatesConfig_OnConfigSave_ReturnsBadRequest()
    {
        // Arrange
        var groups = fixture.Build<string>().CreateMany(2).ToList();
        groupsConfigServiceMock.CreateGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                        Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(),
                                                        Arg.Any<IEnumerable<string>>())
            .Returns(new GroupConfigResult { Errors = ["Failed to save groups"] });

        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.CreateGroupsConfigAsync("teamName", new CreateGroupsConfigRequest
        {
            AdminMembers = groups,
            NonTechUserMembers = groups,
            TechUserMembers = groups
        });

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That((BadRequestObjectResult)result, Is.Not.Null);
    }

    [Test]
    public async Task CreateGroupsConfigAsync_CreatesConfig_OnGroupSync_ReturnsBadRequest()
    {
        // Arrange
        var groups = fixture.Build<string>().CreateMany(2).ToList();
        groupsConfigServiceMock.CreateGroupsConfigAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                        Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(),
                                                        Arg.Any<IEnumerable<string>>())
            .Returns(new GroupConfigResult());
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null)
            .Returns(new GroupSyncResult { Errors = ["Failed to save groups"] });

        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.CreateGroupsConfigAsync("teamName", new CreateGroupsConfigRequest
        {
            AdminMembers = groups,
            NonTechUserMembers = groups,
            TechUserMembers = groups
        });

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That((BadRequestObjectResult)result, Is.Not.Null);
    }

    [Test]
    public async Task SetGroupMembersAsync_SetsMembers_ReturnsCreated()
    {
        // Arrange
        var setMembersRequest = new SetGroupMembersRequest
        {
            AdminMembers = fixture.Build<string>().CreateMany(2),
            TechUserMembers = fixture.Build<string>().CreateMany(2),
            NonTechUserMembers = fixture.Build<string>().CreateMany(2)
        };
        groupsConfigServiceMock.SetGroupMembersAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                     Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(),
                                                     Arg.Any<IEnumerable<string>>()).Returns(new GroupConfigResult());
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), GroupType.UserGroup).Returns(new GroupSyncResult());
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), GroupType.OpenVpnGroup).Returns(new GroupSyncResult());
        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.SetGroupMembersAsync("teamName", setMembersRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<CreatedResult>());
    }

    [Test]
    public async Task SetGroupMembersAsync_SetsMembers_OnSave_ReturnsBadRequest()
    {
        // Arrange
        var setMembersRequest = new SetGroupMembersRequest
        {
            AdminMembers = fixture.Build<string>().CreateMany(2),
            TechUserMembers = fixture.Build<string>().CreateMany(2),
            NonTechUserMembers = fixture.Build<string>().CreateMany(2)
        };
        groupsConfigServiceMock.SetGroupMembersAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                     Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(),
                                                     Arg.Any<IEnumerable<string>>()).Returns(new GroupConfigResult { Errors = ["Something broke"] });

        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.SetGroupMembersAsync("teamName", setMembersRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That((BadRequestObjectResult)result, Is.Not.Null);
    }

    [Test]
    public async Task SetGroupMembersAsync_SetsMembers_OnGroupSync_ReturnsBadRequest()
    {
        // Arrange
        var setMembersRequest = new SetGroupMembersRequest
        {
            AdminMembers = fixture.Build<string>().CreateMany(2),
            TechUserMembers = fixture.Build<string>().CreateMany(2),
            NonTechUserMembers = fixture.Build<string>().CreateMany(2)
        };
        groupsConfigServiceMock.SetGroupMembersAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                     Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>(),
                                                     Arg.Any<IEnumerable<string>>()).Returns(new GroupConfigResult());
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), GroupType.UserGroup).Returns(new GroupSyncResult { Errors = ["Something broke"] });
        groupsConfigServiceMock.SyncGroupsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), GroupType.OpenVpnGroup).Returns(new GroupSyncResult { Errors = ["Something broke"] });

        azureAdConfigMock.Value.Returns(fixture.Create<AzureAdConfig>());

        // Act
        var result = await controller.SetGroupMembersAsync("teamName", setMembersRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That((BadRequestObjectResult)result, Is.Not.Null);
    }
}
