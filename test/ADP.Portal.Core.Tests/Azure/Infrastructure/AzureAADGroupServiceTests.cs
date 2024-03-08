using ADP.Portal.Core.Azure.Infrastructure;
using AutoFixture;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using NSubstitute;
using NUnit.Framework;


namespace ADP.Portal.Core.Tests.Azure.Infrastructure
{
    [TestFixture]
    public class AzureAadGroupServiceTests
    {
        private readonly AzureAadGroupService azureAadGroupService;

        private readonly GraphServiceClient graphServiceClientMock;

        private readonly Fixture fixture;

        public AzureAadGroupServiceTests()
        {
            var requestAdapter = Substitute.For<IRequestAdapter>();
            graphServiceClientMock = new GraphServiceClient(requestAdapter);
            azureAadGroupService = new AzureAadGroupService(graphServiceClientMock);
            fixture = new Fixture();
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsExpectedUserId()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            var user = new Microsoft.Graph.Models.User { Id = Guid.NewGuid().ToString() };
            graphServiceClientMock.Users[userPrincipalName].GetAsync()
                 .ReturnsForAnyArgs(user);

            // Act
            var result = await azureAadGroupService.GetUserIdAsync(userPrincipalName);

            // Assert
            Assert.That(result, Is.EqualTo(user.Id));
        }

        [Test]
        public async Task ExistingMemberAsync_UserExists_ReturnsTrue()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userPrincipalName = "test@domain.com";
            var user = new Microsoft.Graph.Models.User { UserPrincipalName = userPrincipalName };
            graphServiceClientMock.Groups[groupId.ToString()].Members.GraphUser.GetAsync()
                .ReturnsForAnyArgs(new UserCollectionResponse() { Value = [user] });

            // Act
            var isExistingMember = await azureAadGroupService.ExistingMemberAsync(groupId, userPrincipalName);

            // Assert
            Assert.That(isExistingMember, Is.True);
        }

        [Test]
        public async Task ExistingMemberAsync_UserDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userPrincipalName = "test@domain.com";
            graphServiceClientMock.Groups[groupId.ToString()].Members.GraphUser.GetAsync()
               .ReturnsForAnyArgs(new UserCollectionResponse() { Value = [] });

            // Act
            var isExistingMember = await azureAadGroupService.ExistingMemberAsync(groupId, userPrincipalName);

            // Assert
            Assert.That(isExistingMember, Is.False);
        }

        [Test]
        public async Task AddGroupMemberAsync_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid().ToString();
            var userId = "12345";
            var requestBody = new ReferenceCreate
            {
                OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{userId}",
            };

            await graphServiceClientMock.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);

            // Act
            var result = await azureAadGroupService.AddGroupMemberAsync(groupId, userId);

            // Assert

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task RemoveGroupMemberAsync_GivenValidInputs_ShouldReturnTrue()
        {
            // Arrange
            var groupId = "groupId";
            var directoryObjectId = "directoryObjectId";

            await graphServiceClientMock.Groups[groupId.ToString()].Members[directoryObjectId].Ref.DeleteAsync();
            // Act
            var result = await azureAadGroupService.RemoveGroupMemberAsync(groupId, directoryObjectId);

            // Assert
            Assert.That(result, Is.True);
        }


        [Test]
        public async Task GetGroupIdAsync_GivenGroupName_ReturnsGroupId()
        {
            // Arrange
            var groupName = "testGroup";
            var expectedGroupId = "groupId";

            var group = new Group { Id = expectedGroupId };
            var existingGroup = new GroupCollectionResponse() { Value = [new Group { Id = group.Id }] };


            graphServiceClientMock.Groups.GetAsync().ReturnsForAnyArgs(existingGroup);

            // Act
            var result = await azureAadGroupService.GetGroupIdAsync(groupName);

            // Assert
            Assert.That(result, Is.EqualTo(expectedGroupId));
        }


        [Test]
        public async Task GetGroupIdAsync_GivenGroupName_NotExists_ReturnsDefault()
        {
            // Arrange
            var groupName = "testGroup";

            GroupCollectionResponse? existingGroup = null;

            graphServiceClientMock.Groups.GetAsync().ReturnsForAnyArgs(existingGroup);

            // Act
            var result = await azureAadGroupService.GetGroupIdAsync(groupName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupId_Returns_UserType_GroupMembers()
        {
            // Arrange
            var groupId = "groupId";
            var expectedUser = new Microsoft.Graph.Models.User { Id = "userId", UserPrincipalName = "user@domain.com" };
            var directoryObjectResponse = new DirectoryObjectCollectionResponse { Value = [expectedUser] };


            graphServiceClientMock.Groups[groupId].Members.GetAsync()
                .ReturnsForAnyArgs(directoryObjectResponse);

            // Act
            var result = await azureAadGroupService.GetGroupMembersAsync<Microsoft.Graph.Models.User>(groupId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupIdButNoUserTypeMembers_Returns_EmptyList()
        {
            // Arrange
            var groupId = "groupId";

            var directoryObjectResponse = new DirectoryObjectCollectionResponse { Value = [] };

            graphServiceClientMock.Groups[groupId].Members.GetAsync()
                .ReturnsForAnyArgs(directoryObjectResponse);

            // Act
            var result = await azureAadGroupService.GetGroupMembersAsync<Microsoft.Graph.Models.User>(groupId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupId_Returns_GroupType_GroupMembers()
        {
            // Arrange
            var groupId = "groupId";
            var expectedGroup = new Group { Id = "userId", DisplayName = "Test group" };
            var directoryObjectResponse = new DirectoryObjectCollectionResponse { Value = [expectedGroup] };


            graphServiceClientMock.Groups[groupId].Members.GetAsync()
                .ReturnsForAnyArgs(directoryObjectResponse);

            // Act
            var result = await azureAadGroupService.GetGroupMembersAsync<Group>(groupId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetGroupMembersAsync_GivenGroupIdButNoGroupTypeMembers_Returns_EmptyList()
        {
            // Arrange
            var groupId = "groupId";

            var directoryObjectResponse = new DirectoryObjectCollectionResponse { Value = [] };

            graphServiceClientMock.Groups[groupId].Members.GetAsync()
                .ReturnsForAnyArgs(directoryObjectResponse);

            // Act
            var result = await azureAadGroupService.GetGroupMembersAsync<Group>(groupId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetGroupMemberShipsAsync_GivenGroupId_ReturnsGroupMemberships()
        {
            // Arrange
            var groupId = "groupId";
            var expectedGroup = new Group { Id = "groupId2", DisplayName = "group2" };
            var directoryObjectResponse = new DirectoryObjectCollectionResponse { Value = [expectedGroup] };

            graphServiceClientMock.Groups[groupId].MemberOf.GetAsync()
                .ReturnsForAnyArgs(directoryObjectResponse);

            // Act
            var result = await azureAadGroupService.GetGroupMemberShipsAsync(groupId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetGroupMemberShipsAsync_GivenGroupIdButNoMemberships_ReturnsNull()
        {
            // Arrange
            var groupId = "groupId";


            var directoryObjectResponse = new DirectoryObjectCollectionResponse { Value = [] };

            graphServiceClientMock.Groups[groupId].MemberOf.GetAsync()
               .ReturnsForAnyArgs(directoryObjectResponse);

            // Act
            var result = await azureAadGroupService.GetGroupMemberShipsAsync(groupId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result?.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task AddGroupAsync_GivenGroup_ReturnsAddedGroup()
        {
            // Arrange
            var group = new Group { Id = "groupId", DisplayName = "group" };

            graphServiceClientMock.Groups.PostAsync(group).ReturnsForAnyArgs(group);

            // Act
            var result = await azureAadGroupService.AddGroupAsync(group);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(group));
        }
    }
}
