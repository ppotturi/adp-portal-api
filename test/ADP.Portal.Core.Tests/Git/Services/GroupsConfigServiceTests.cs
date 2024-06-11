using System.Net;
using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using Octokit;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Tests.Git.Services;

[TestFixture]
public class GroupsConfigServiceTests
{
    private IGitHubRepository gitOpsConfigRepositoryMock = null!;
    private GroupsConfigService gitOpsConfigService = null!;
    private ILogger<GroupsConfigService> loggerMock = null!;
    private IGroupService groupServiceMock = null!;
    private IOptionsSnapshot<GitRepo> gitRepoOptionsMock = null!;
    private Fixture fixture = null!;

    [SetUp]
    public void SetUp()
    {
        gitOpsConfigRepositoryMock = Substitute.For<IGitHubRepository>();
        loggerMock = Substitute.For<ILogger<GroupsConfigService>>();
        groupServiceMock = Substitute.For<IGroupService>();
        gitRepoOptionsMock = Substitute.For<IOptionsSnapshot<GitRepo>>();
        gitOpsConfigService = new GroupsConfigService(gitOpsConfigRepositoryMock, gitRepoOptionsMock, loggerMock, groupServiceMock, Substitute.For<ISerializer>());
        fixture = new Fixture();
    }

    [Test]
    public async Task SyncGroupsAsync_GroupsConfigIsNull_ReturnsErrorResult()
    {
        // Arrange
        GroupsRoot? groupsRoot = null;
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
        if (result.Errors.Count > 0)
        {
            Assert.That(result.Errors[0], Is.EqualTo("Groups config not found for the team:teamName in the tenant:tenantName"));
        }
    }

    [Test]
    public async Task SyncGroupsAsync_GroupsConfigIs_NotFound_ReturnsErrorResult()
    {
        // Arrange
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>())
            .Throws(new NotFoundException("Not found", HttpStatusCode.NotFound));

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
        if (result.Errors.Count > 0)
        {
            Assert.That(result.Errors[0], Is.EqualTo("Groups config not found for the team:teamName in the tenant:tenantName"));
        }
    }

    [Test]
    public async Task SyncGroupsAsync_Returns_Success_WhenOpenVpn_Members_Synced()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [
              new() { DisplayName = "group1" , Type = GroupType.OpenVpnGroup,  Members= ["vpnuser@test.com"] }
            ]
        };

        var groupId = "openVpngroupId";
        var memberId = "memberId";
        var exstingMemberToberemoved = fixture.Build<AadGroupMember>().CreateMany(1).ToList();
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
        groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns(exstingMemberToberemoved);
        groupServiceMock.GetUserIdAsync(Arg.Is(groupsRoot.Groups[0].Members[0].ToString())).Returns(memberId);

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.OpenVpnGroup);

        // Assert
        Assert.That(result.Errors, Is.Empty);
        await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Is(exstingMemberToberemoved[0].Id));
        await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Is(memberId));
    }

    [Test]
    public async Task SyncGroupsAsync_Returns_Success_WhenOpenVpn_NoMembers()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [
              new() {  DisplayName = "group1" , Type = GroupType.OpenVpnGroup}
            ]
        };

        var groupId = "openNoMenbersVpnGroupId";
        var memberId = "memberId";
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
        groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);


        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.OpenVpnGroup);

        // Assert
        Assert.That(result.Errors, Is.Empty);
        await groupServiceMock.DidNotReceive().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
        await groupServiceMock.DidNotReceive().AddGroupMemberAsync(Arg.Is(groupId), Arg.Is(memberId));
    }


    [Test]
    public async Task SyncGroupsAsync_Returns_Success_WhenUserGroup_Members_Synced()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [
              new() { DisplayName = "user-group1" , Type = GroupType.UserGroup, Members = ["user@test.com"] }
            ]
        };

        var groupId = "usergroupId";
        var memberId = "memberId";
        var exstingMemberToberemoved = fixture.Build<AadGroupMember>().CreateMany(1).ToList();
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
        groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns(exstingMemberToberemoved);
        groupServiceMock.GetUserIdAsync(Arg.Is(groupsRoot.Groups[0].Members[0].ToString())).Returns(memberId);
        groupServiceMock.GetGroupMemberShipsAsync(Arg.Is(groupId)).Returns([]);

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup);

        // Assert
        Assert.That(result.Errors, Is.Empty);
        await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Is(exstingMemberToberemoved[0].Id));
        await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Is(memberId));
    }


    [Test]
    public async Task SyncGroupsAsync_Returns_Success_WhenUserGroup_Memberships_Synced()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [ new() {
                DisplayName = "user-group-memberships" , Type = GroupType.UserGroup,
                  GroupMemberships = ["member-ship-group"] }
            ]
        };

        var groupId = "userGroupMembershipsId";
        var exstingMembershipsToberemoved = fixture.Build<AadGroup>().CreateMany(1).ToList();

        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
        groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);
        groupServiceMock.GetGroupMemberShipsAsync(Arg.Is(groupId)).Returns(exstingMembershipsToberemoved);

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup);

        // Assert
        Assert.That(result.Errors, Is.Empty);
        await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Any<string>(), Arg.Is(groupId));
        await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
    }

    [Test]
    public async Task SyncGroupsAsync_Returns_Success_WhenAccessGroup_GroupMembers_Synced()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [ new() {
                DisplayName = "access-group-memberships" , Type = GroupType.AccessGroup,
                Members = ["group-member"]  }
            ]
        };

        var groupId = "accessGroupMemberId";
        var exstingMembersToberemoved = fixture.Build<AadGroupMember>().CreateMany(1).ToList();

        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
        groupServiceMock.GetGroupTypeGroupMembersAsync(Arg.Any<string>()).Returns(exstingMembersToberemoved);


        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup);

        // Assert
        Assert.That(result.Errors, Is.Empty);
        await groupServiceMock.Received().RemoveGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
        await groupServiceMock.Received().AddGroupMemberAsync(Arg.Is(groupId), Arg.Any<string>());
    }

    [Test]
    public async Task SyncGroupsAsync_ErrorOccursWhileCreating_UserGroup_ReturnsErrorResult()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [
               new() { DisplayName = "group1" , Type= GroupType.UserGroup }
           ]
        };

        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns("");

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }



    [Test]
    public async Task SyncGroupsAsync_ErrorOccursWhileAdding_UserTypeMembers_ReturnsErrorResult()
    {
        // Arrange

        var groupsRoot = new GroupsRoot
        {
            Groups = [
               new Group() { DisplayName = "group1", Type = GroupType.UserGroup, Members = ["test@test"]  }
           ]
        };
        var groupId = "groupId";
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns(groupId);
        groupServiceMock.GetUserTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);
        groupServiceMock.GetUserIdAsync(Arg.Any<string>()).Returns((string?)null);
        groupServiceMock.GetGroupMemberShipsAsync(Arg.Any<string>()).Returns([]);

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.UserGroup);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0], Is.EqualTo($"User '{groupsRoot.Groups[0].Members[0]}' not found for the group:{groupsRoot.Groups[0].DisplayName}."));
    }


    [Test]
    public async Task SyncGroupsAsync_ErrorOccursWhileCreating_AccessGroup_ReturnsErrorResult()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [
               new() { DisplayName = "group1", Type = GroupType.AccessGroup }
           ]
        };

        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Any<string>()).Returns("");
        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public async Task SyncGroupsAsync_ErrorOccursWhileAdding_GroupTypeMembers_ReturnsErrorResult()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [
               new Group() { DisplayName = "group1", Type = GroupType.AccessGroup, Members = ["test-group"]  }
           ]
        };

        var groupId = "groupId";

        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);
        groupServiceMock.GetGroupIdAsync(Arg.Is(groupsRoot.Groups[0].DisplayName)).Returns(groupId);
        groupServiceMock.GetGroupTypeGroupMembersAsync(Arg.Any<string>()).Returns([]);
        groupServiceMock.GetUserIdAsync(Arg.Any<string>()).Returns((string?)null);
        groupServiceMock.GetGroupIdAsync(Arg.Is(groupsRoot.Groups[0].Members[0].ToString())).Returns((string?)null);

        // Act
        var result = await gitOpsConfigService.SyncGroupsAsync("tenantName", "teamName", "ownerId", GroupType.AccessGroup);

        // Assert
        Assert.That(result.Errors, Is.Not.Empty);

        Assert.That(result.Errors[0], Is.EqualTo($"Group '{groupsRoot.Groups[0].Members[0]}' not found."));
    }

    [Test]
    public async Task GetGroupsConfigAsync_Returns_Groups()
    {
        // Arrange
        var groupsRoot = new GroupsRoot
        {
            Groups = [new Group { DisplayName = "group1", Type = GroupType.AccessGroup, Members = ["test-group"] }]
        };

        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(groupsRoot);

        // Act
        var response = await gitOpsConfigService.GetGroupsConfigAsync("tenantName", "teamName");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetGroupsConfigAsync_NotFound_Returns_NoGroups()
    {
        // Arrange
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(new GroupsRoot() { Groups = [] });

        // Act
        var response = await gitOpsConfigService.GetGroupsConfigAsync("tenantName", "teamName");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CreateGroupsConfigAsync_ConfigCreated()
    {
        // Arrange
        gitOpsConfigRepositoryMock.CreateFileAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns("sha");
        var members = fixture.Build<string>().CreateMany(2);

        // Act
        var response = await gitOpsConfigService.CreateGroupsConfigAsync("defra", "teamName", members, members, members);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task CreateGroupsConfigAsync_Returns_Error()
    {
        // Arrange
        gitOpsConfigRepositoryMock.CreateFileAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        var members = fixture.Build<string>().CreateMany(2);

        // Act
        var response = await gitOpsConfigService.CreateGroupsConfigAsync("defradev", "teamName", members, members, members);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Errors.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task SetGroupMembersAsync_ConfigCreated()
    {
        // Arrange
        var groups = fixture.Build<Group>().CreateMany(3);
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(new GroupsRoot { Groups = groups.ToList() });
        gitOpsConfigRepositoryMock.UpdateFileAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns("response");
        var members = fixture.Build<string>().CreateMany(2);

        // Act
        var response = await gitOpsConfigService.SetGroupMembersAsync("defradev", "teamName", members, members, members);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Errors, Is.Empty);
    }

    [Test]
    public async Task SetGroupMembersAsync_NoExistingGroups_ReturnsError()
    {
        // Arrange
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).ReturnsNull();
        var members = fixture.Build<string>().CreateMany(2);

        // Act
        var response = await gitOpsConfigService.SetGroupMembersAsync("defradev", "teamName", members, members, members);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Errors, Has.Exactly(1).Items);
    }

    [Test]
    public async Task SetGroupMembersAsync_UpdateError_ReturnsError()
    {
        // Arrange
        var groups = fixture.Build<Group>().CreateMany(3);
        gitOpsConfigRepositoryMock.GetFileContentAsync<GroupsRoot>(Arg.Any<GitRepo>(),Arg.Any<string>()).Returns(new GroupsRoot { Groups = groups.ToList() });
        gitOpsConfigRepositoryMock.UpdateFileAsync(Arg.Any<GitRepo>(), Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
        var members = fixture.Build<string>().CreateMany(2);

        // Act
        var response = await gitOpsConfigService.SetGroupMembersAsync("defradev", "teamName", members, members, members);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Errors, Has.Exactly(1).Items);
    }
}
