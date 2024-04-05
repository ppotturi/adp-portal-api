using ADP.Portal.Api.Config;
using ADP.Portal.Api.Services;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Octokit;
using System.Reflection;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
    public class AdoRestAPIServiceTests
    {
        private readonly ILogger<AdoRestAPIService> loggerMock;
        private readonly IOptions<AdoConfig> configurationMock;

        private readonly AdoRestAPIService adoRestAPIService;
        private readonly Fixture fixture;
        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        }
        public AdoRestAPIServiceTests()
        {
            configurationMock = Substitute.For<IOptions<AdoConfig>>();            
            loggerMock = Substitute.For<ILogger<AdoRestAPIService>>();                        
            fixture = new Fixture();
            configurationMock.Value.Returns(fixture.Create<AdoConfig>());
            adoRestAPIService = new AdoRestAPIService(loggerMock, configurationMock);

        }

        [Test]
        public void Constructor_WithValidParameters_SetsAdoRestAPIService()
        {
            // Arrange
            configurationMock.Value.Returns(fixture.Create<AdoConfig>());
            // Act
            var restAPIService = new AdoRestAPIService(loggerMock, configurationMock);

            // Assert
            Assert.That(restAPIService, Is.Not.Null);
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsUserId_WhenExists()
        {
            // Arrange
            var projectName = "TestProject";
            var userName = "Project Administrators";
            configurationMock.Value.Returns(fixture.Create<AdoConfig>());

            // Act
            var userid = await adoRestAPIService.GetUserIdAsync(projectName, userName);

            // Assert
            Assert.That(userid, Is.EqualTo(""));
        }
        [Test]
        public async Task postRoleAssignmentAsync_ReturnsSuccess()
        {
            // Arrange
            string roleName = "Project Administrators";
            string projectId = Guid.NewGuid().ToString();
            string envId = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();
            configurationMock.Value.Returns(fixture.Create<AdoConfig>());

            // Act
            var result=await adoRestAPIService.postRoleAssignmentAsync(projectId, envId, roleName, userId);

            // Assert
            Assert.That(result, Is.True);
        }


    }
}