using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Infrastructure;
using ADP.Portal.Core.Azure.Services;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
    public class GroupServiceTests
    {
        private readonly IAzureAadGroupService azureAADGroupServiceMock;
        private readonly ILogger<GroupService> loggerMock;
        private readonly GroupService groupService;
        private readonly Fixture fixture;

        public GroupServiceTests()
        {
            azureAADGroupServiceMock = Substitute.For<IAzureAadGroupService>();
            loggerMock = Substitute.For<ILogger<GroupService>>();
            groupService = new GroupService(azureAADGroupServiceMock, loggerMock);
            fixture = new Fixture();
        }

        [Test]
        public void Constructor_WithValidParameters_SetsUserGroupService()
        {
            // Act
            var groupService2 = new GroupService(azureAADGroupServiceMock, loggerMock);

            // Assert
            Assert.That(groupService2, Is.Not.Null);
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsExpectedUserId()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            var userId = "12345";
            azureAADGroupServiceMock.GetUserIdAsync(userPrincipalName).Returns(userId);

            // Act
            var result = await groupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.Not.Null);
        }


        [Test]
        public async Task GetUserIdAsync_Returns_Null_UserId_When_NotExists()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            azureAADGroupServiceMock.GetUserIdAsync(userPrincipalName).ThrowsAsync(new ODataError() { ResponseStatusCode = 404 });

            // Act
            var result = await groupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetUserIdAsync_Throw_Unhandled_Exception()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            azureAADGroupServiceMock.GetUserIdAsync(userPrincipalName).ThrowsAsync<ODataError>();


            // Assert
            Assert.ThrowsAsync<ODataError>(async () => await groupService.GetUserIdAsync(userPrincipalName));
        }

        [Test]
        public async Task AddGroupMemberAsync_Return_True_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            azureAADGroupServiceMock.AddGroupMemberAsync(groupId, userId).Returns(true);

            // Act
            var result = await groupService.AddGroupMemberAsync(groupId, userId);

            // Assert
            await azureAADGroupServiceMock.Received().AddGroupMemberAsync(groupId, userId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task AddGroupMemberAsync_Return_False_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            azureAADGroupServiceMock.AddGroupMemberAsync(groupId, userId).Returns(false);

            // Act
            var result = await groupService.AddGroupMemberAsync(groupId, userId);

            // Assert
            await azureAADGroupServiceMock.Received().AddGroupMemberAsync(groupId, userId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task RemoveGroupMemberAsync_GivenValidInputs_ShouldReturnTrueAndLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            var memberId = "memberId";

            azureAADGroupServiceMock.RemoveGroupMemberAsync(groupId, memberId).Returns(true);

            // Act
            var result = await groupService.RemoveGroupMemberAsync(groupId, memberId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task RemoveGroupMemberAsync_GivenValidInputsButRemoveFails_ShouldReturnFalseAndNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            var memberId = "memberId";

            azureAADGroupServiceMock.RemoveGroupMemberAsync(groupId, memberId).Returns(false);

            // Act
            var result = await groupService.RemoveGroupMemberAsync(groupId, memberId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetGroupIdAsync_GroupExists_ReturnsGroupId()
        {
            // Arrange
            var groupName = "testGroup";
            var groupId = "testId";
            azureAADGroupServiceMock.GetGroupIdAsync(groupName).Returns(groupId);

            // Act
            var result = await groupService.GetGroupIdAsync(groupName);

            // Assert
            Assert.That(result, Is.EqualTo(groupId));
        }

        [Test]
        public async Task GetGroupIdAsync_GroupDoesNotExist_ReturnsNull()
        {
            // Arrange
            var groupName = "testGroup";
            azureAADGroupServiceMock.GetGroupIdAsync(groupName).Returns((string?)null);

            // Act
            var result = await groupService.GetGroupIdAsync(groupName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupId_Returns_UserTypeGroupMembersAndLogsInformation()
        {
            // Arrange
            var groupId = "groupId";
            var groupMembers = new List<User> { new User { Id = "memberId", UserPrincipalName = "user@domain.com" } };

            azureAADGroupServiceMock.GetGroupMembersAsync<User>(groupId).Returns(groupMembers);

            // Act
            var result = await groupService.GetUserTypeGroupMembersAsync(groupId);

            // Assert

            Assert.That(groupMembers.Count, Is.EqualTo(result.Count));
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupIdButNo_UserTypeMembers_ReturnsEmpty_ListAndDoesNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            List<User>? listUsers = null;
            azureAADGroupServiceMock.GetGroupMembersAsync<User>(groupId).Returns(listUsers);

            // Act
            var result = await groupService.GetUserTypeGroupMembersAsync(groupId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupId_Returns_GroupType_GroupMembersAndLogsInformation()
        {
            // Arrange
            var groupId = "groupId";
            var groupMembers = new List<Group> { new Group { Id = "memberId", DisplayName = "Test group" } };

            azureAADGroupServiceMock.GetGroupMembersAsync<Group>(groupId).Returns(groupMembers);

            // Act
            var result = await groupService.GetGroupTypeGroupMembersAsync(groupId);

            // Assert

            Assert.That(groupMembers.Count, Is.EqualTo(result.Count));
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupIdButNoMembers_ReturnsEmpty_GroupType_ListAndDoesNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            List<Group>? listUsers = null;
            azureAADGroupServiceMock.GetGroupMembersAsync<Group>(groupId).Returns(listUsers);

            // Act
            var result = await groupService.GetUserTypeGroupMembersAsync(groupId);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetGroupMemberShipsAsync_GivenGroupId_ReturnsGroupMembershipsAndLogsInformation()
        {
            // Arrange
            var groupId = "groupId";
            var groupMemberships = new List<Group> { new Group { Id = "groupId2", DisplayName = "group2" } };

            azureAADGroupServiceMock.GetGroupMemberShipsAsync(groupId).Returns(groupMemberships);

            // Act
            var result = await groupService.GetGroupMemberShipsAsync(groupId);

            // Assert
            Assert.That(groupMemberships.Count, Is.EqualTo(result.Count));
        }

        [Test]
        public async Task GetGroupMemberShipsAsync_GivenGroupIdButNoMemberships_ReturnsEmptyListAndDoesNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            List<Group>? listGroups = null;
            azureAADGroupServiceMock.GetGroupMemberShipsAsync(groupId).Returns(listGroups);

            // Act
            var result = await groupService.GetGroupMemberShipsAsync(groupId);

            // Assert
            Assert.That(result, Is.Empty);
        }
        [Test]
        public async Task AddGroupAsync_GivenAadGroup_ReturnsGroupIdAndLogsInformation()
        {
            // Arrange
            var aadGroup = fixture.Build<AadGroup>().With(i => i.DisplayName, "group").Create();
            var group = aadGroup.Adapt<Group>();
            group.Id = "groupId";

            azureAADGroupServiceMock.AddGroupAsync(Arg.Any<Group>()).Returns(group);

            // Act
            var result = await groupService.AddGroupAsync(aadGroup);

            // Assert
            Assert.That(result, Is.EqualTo(group.Id));

        }

        [Test]
        public async Task AddGroupAsync_GivenAadGroupButAddFails_ReturnsNullAndDoesNotLogInformation()
        {
            // Arrange
            var aadGroup = fixture.Build<AadGroup>().With(i => i.DisplayName, "group").Create();


            azureAADGroupServiceMock.AddGroupAsync(Arg.Any<Group>()).Returns((Group?)null);

            // Act
            var result = await groupService.AddGroupAsync(aadGroup);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}