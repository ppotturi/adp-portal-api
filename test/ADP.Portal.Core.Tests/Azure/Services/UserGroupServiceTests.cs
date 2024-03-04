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
    public class UserGroupServiceTests
    {
        private readonly IAzureAadGroupService azureAADGroupServicMock;
        private readonly ILogger<UserGroupService> loggerMock;
        private readonly UserGroupService userGroupService;
        private readonly Fixture fixture;

        public UserGroupServiceTests()
        {
            azureAADGroupServicMock = Substitute.For<IAzureAadGroupService>();
            loggerMock = Substitute.For<ILogger<UserGroupService>>();
            userGroupService = new UserGroupService(azureAADGroupServicMock, loggerMock);
            fixture= new Fixture();
        }

        [Test]
        public void Constructor_WithValidParameters_SetsUserGroupService()
        {
            // Act
            var userGroupService2 = new UserGroupService(azureAADGroupServicMock, loggerMock);

            // Assert
            Assert.That(userGroupService2, Is.Not.Null);
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsExpectedUserId()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            var userId = "12345";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).Returns(userId);

            // Act
            var result = await userGroupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.Not.Null);
        }


        [Test]
        public async Task GetUserIdAsync_Returns_Null_UserId_When_NotExists()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).ThrowsAsync(new ODataError() { ResponseStatusCode = 404 });

            // Act
            var result = await userGroupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetUserIdAsync_Throw_Unhandled_Exception()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            azureAADGroupServicMock.GetUserIdAsync(userPrincipalName).ThrowsAsync<ODataError>();


            // Assert
            Assert.ThrowsAsync<ODataError>(async () => await userGroupService.GetUserIdAsync(userPrincipalName));
        }

        [Test]
        public async Task AddGroupMemberAsync_Return_True_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            azureAADGroupServicMock.AddGroupMemberAsync(groupId, userId).Returns(true);

            // Act
            var result = await userGroupService.AddGroupMemberAsync(groupId, userId);

            // Assert
            await azureAADGroupServicMock.Received().AddGroupMemberAsync(groupId, userId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task AddGroupMemberAsync_Return_False_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            azureAADGroupServicMock.AddGroupMemberAsync(groupId, userId).Returns(false);

            // Act
            var result = await userGroupService.AddGroupMemberAsync(groupId, userId);

            // Assert
            await azureAADGroupServicMock.Received().AddGroupMemberAsync(groupId, userId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task RemoveGroupMemberAsync_GivenValidInputs_ShouldReturnTrueAndLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            var memberId = "memberId";

            azureAADGroupServicMock.RemoveGroupMemberAsync(groupId, memberId).Returns(true);

            // Act
            var result = await userGroupService.RemoveGroupMemberAsync(groupId, memberId);

            // Assert
            Assert.That(result,Is.True);
        }

        [Test]
        public async Task RemoveGroupMemberAsync_GivenValidInputsButRemoveFails_ShouldReturnFalseAndNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            var memberId = "memberId";

            azureAADGroupServicMock.RemoveGroupMemberAsync(groupId, memberId).Returns(false);

            // Act
            var result = await userGroupService.RemoveGroupMemberAsync(groupId, memberId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetGroupIdAsync_GroupExists_ReturnsGroupId()
        {
            // Arrange
            var groupName = "testGroup";
            var groupId = "testId";
            azureAADGroupServicMock.GetGroupIdAsync(groupName).Returns(groupId);

            // Act
            var result = await userGroupService.GetGroupIdAsync(groupName);

            // Assert
            Assert.That(result, Is.EqualTo(groupId));
        }

        [Test]
        public async Task GetGroupIdAsync_GroupDoesNotExist_ReturnsNull()
        {
            // Arrange
            var groupName = "testGroup";
            azureAADGroupServicMock.GetGroupIdAsync(groupName).Returns((string?)null);

            // Act
            var result = await userGroupService.GetGroupIdAsync(groupName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupId_ReturnsGroupMembersAndLogsInformation()
        {
            // Arrange
            var groupId = "groupId";
            var groupMembers = new List<User> { new User { Id = "memberId", UserPrincipalName = "user@domain.com" } };

            azureAADGroupServicMock.GetGroupMembersAsync(groupId).Returns(groupMembers);

            // Act
            var result = await userGroupService.GetGroupMembersAsync(groupId);

            // Assert
            
            Assert.That(groupMembers.Count, Is.EqualTo(result.Count));
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupIdButNoMembers_ReturnsEmptyListAndDoesNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            List<User>? listUsers = null;
            azureAADGroupServicMock.GetGroupMembersAsync(groupId).Returns(listUsers);

            // Act
            var result = await userGroupService.GetGroupMembersAsync(groupId);

            // Assert
            Assert.That(result,Is.Empty);
        }

        [Test]
        public async Task GetGroupMemberShipsAsync_GivenGroupId_ReturnsGroupMembershipsAndLogsInformation()
        {
            // Arrange
            var groupId = "groupId";
            var groupMemberships = new List<Group> { new Group { Id = "groupId2", DisplayName = "group2" } };
            
            azureAADGroupServicMock.GetGroupMemberShipsAsync(groupId).Returns(groupMemberships);

            // Act
            var result = await userGroupService.GetGroupMemberShipsAsync(groupId);

            // Assert
            Assert.That(groupMemberships.Count, Is.EqualTo(result.Count));
        }

        [Test]
        public async Task GetGroupMemberShipsAsync_GivenGroupIdButNoMemberships_ReturnsEmptyListAndDoesNotLogInformation()
        {
            // Arrange
            var groupId = "groupId";
            List<Group>? listGroups = null;
            azureAADGroupServicMock.GetGroupMemberShipsAsync(groupId).Returns(listGroups);

            // Act
            var result = await userGroupService.GetGroupMemberShipsAsync(groupId);

            // Assert
            Assert.That(result, Is.Empty);
        }
        [Test]
        public async Task AddGroupAsync_GivenAadGroup_ReturnsGroupIdAndLogsInformation()
        {
            // Arrange
            var aadGroup = fixture.Build<AadGroup>().With(i=>i.DisplayName, "group").Create();
            var group = aadGroup.Adapt<Group>();
            group.Id = "groupId";

            azureAADGroupServicMock.AddGroupAsync(Arg.Any<Group>()).Returns(group);

            // Act
            var result = await userGroupService.AddGroupAsync(aadGroup);

            // Assert
            Assert.That(result, Is.EqualTo(group.Id));
            
        }

        [Test]
        public async Task AddGroupAsync_GivenAadGroupButAddFails_ReturnsNullAndDoesNotLogInformation()
        {
            // Arrange
            var aadGroup = fixture.Build<AadGroup>().With(i => i.DisplayName, "group").Create();
            
            
            azureAADGroupServicMock.AddGroupAsync(Arg.Any<Group>()).Returns((Group?)null);

            // Act
            var result = await userGroupService.AddGroupAsync(aadGroup);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}