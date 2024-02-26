using ADP.Portal.Core.Azure.Infrastructure;
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

        public AzureAadGroupServiceTests()
        {
            var requestAdapter = Substitute.For<IRequestAdapter>();
            graphServiceClientMock = new GraphServiceClient(requestAdapter);
            azureAadGroupService = new AzureAadGroupService(graphServiceClientMock);
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsExpectedUserId()
        {
            // Arrange
            var userPrincipalName = "test@domain.com";
            var user = new User { Id = Guid.NewGuid().ToString() };
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
            var groupId = Guid.NewGuid();
            var userPrincipalName = "test@domain.com";
            var user = new User { UserPrincipalName = userPrincipalName };
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
            var groupId = Guid.NewGuid();
            var userPrincipalName = "test@domain.com";
            graphServiceClientMock.Groups[groupId.ToString()].Members.GraphUser.GetAsync()
               .ReturnsForAnyArgs(new UserCollectionResponse() { Value = [] });

            // Act
            var isExistingMember = await azureAadGroupService.ExistingMemberAsync(groupId, userPrincipalName);

            // Assert
            Assert.That(isExistingMember, Is.False);
        }

        [Test]
        public async Task AddToAADGroupAsync_AddsUserToGroup()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var userId = "12345";
            var requestBody = new ReferenceCreate
            {
                OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{userId}",
            };

            await graphServiceClientMock.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);

            // Act
            var result = await azureAadGroupService.AddToAADGroupAsync(groupId, userId);

            // Assert
            
            Assert.That(result, Is.True);
        }
    }
}
