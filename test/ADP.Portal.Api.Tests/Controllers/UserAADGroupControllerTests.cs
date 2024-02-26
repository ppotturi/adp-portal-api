using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Azure.Services;
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
    public class UserAADGroupControllerTests
    {
        private readonly UserAadGroupController controller;
        private readonly ILogger<UserAadGroupController> loggerMock;
        private readonly IOptions<AadGroupConfig> configMock;
        private readonly IUserGroupService serviceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public UserAADGroupControllerTests()
        {
            loggerMock = Substitute.For<ILogger<UserAadGroupController>> ();
            configMock = Substitute.For<IOptions<AadGroupConfig>>();
            serviceMock = Substitute.For<IUserGroupService>();
            controller = new UserAadGroupController(loggerMock, serviceMock, configMock);
        } 

        [Test]
        public async Task AddUserToOpenVpnGroup_ReturnSuccess_When_AddUserToGroup_IsTrue()
        {
            // Arrange
            string userPrincipalName = "testUser";
            var fixture = new Fixture();
            configMock.Value.Returns(fixture.Create<AadGroupConfig>());

            var expectedUserId = Guid.NewGuid().ToString();
            serviceMock.GetUserIdAsync(userPrincipalName).Returns(expectedUserId);
            serviceMock.AddUserToGroupAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

            // Act
            var result = await controller.AddUserToOpenVpnGroup(userPrincipalName);
            var noContentResult = result as NoContentResult;

            // Assert
            Assert.That(noContentResult, Is.Not.Null);
            if (noContentResult != null)
            {
                Assert.That(noContentResult.StatusCode, Is.EqualTo(204));
            }
        }
        
        [Test]
        public async Task AddUserToOpenVpnGroup_ReturnNotFound_When_UserId_Null()
        {
            // Arrange
            string userPrincipalName = "testUser";
            var fixture = new Fixture();
            configMock.Value.Returns(fixture.Create<AadGroupConfig>());
            string? expectedUserId = null;
            serviceMock.GetUserIdAsync(userPrincipalName).Returns(expectedUserId);

            // Act
            var result = await controller.AddUserToOpenVpnGroup(userPrincipalName);
            var notFoundResults = result as NotFoundObjectResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
                Assert.That(notFoundResults.Value, Is.EqualTo("User not found"));
            }
        }

        [Test]
        public async Task AddUserToOpenVpnGroup_ReturnBadRequest_When_AddUserToGroup_Is_False()
        {
            // Arrange
            string userPrincipalName = "testUser";
            var fixture = new Fixture();
            configMock.Value.Returns(fixture.Create<AadGroupConfig>());

            var expectedUserId = Guid.NewGuid().ToString();
            serviceMock.GetUserIdAsync(userPrincipalName).Returns(expectedUserId);
            serviceMock.AddUserToGroupAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

            // Act
            var result = await controller.AddUserToOpenVpnGroup(userPrincipalName);
            var badRequestResult = result as BadRequestResult;

            // Assert
            Assert.That(badRequestResult, Is.Not.Null);
            if (badRequestResult != null)
            {
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
        }
    }
}
