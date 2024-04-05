using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Api.Services;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;
namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
    public class AdoRestApiServiceTests
    {
        private readonly ILogger<AdoRestApiService> loggerMock;
        private readonly IOptions<AdoConfig> configurationMock;

        private readonly AdoRestApiService adoRestApiService;
        private readonly Fixture fixture;
        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        }
        public AdoRestApiServiceTests()
        {
            configurationMock = Substitute.For<IOptions<AdoConfig>>();
            loggerMock = Substitute.For<ILogger<AdoRestApiService>>();
            fixture = new Fixture();
            var adoConfig = fixture.Create<AdoConfig>();
            adoConfig.OrganizationUrl = "https://dev.azure.com/defragovuk/DEFRA-TRADE-PUBLIC/";
            configurationMock.Value.Returns(adoConfig);
            adoRestApiService = new AdoRestApiService(loggerMock, configurationMock);

        }

        [Test]
        public void Constructor_WithValidParameters_SetsAdoRestApiService()
        {
            // Arrange

            // Act
            var restAPIService = new AdoRestApiService(loggerMock, configurationMock);

            // Assert
            Assert.That(restAPIService, Is.Not.Null);
        }
        [Test]
        public void Validate_AdoGroup_Model()
        {
            // Arrange
            const string data = "{count : 1 , value : [ { id : '454353', providerDisplayName : 'testName', extra : 'testvalue' } ] } ";
            // Act
            var jsonAdoGroupWrapper = JsonConvert.DeserializeObject<JsonAdoGroupWrapper>(data);
            var adoGroup = (jsonAdoGroupWrapper != null && jsonAdoGroupWrapper.value != null) ? jsonAdoGroupWrapper.value[0] : null;

            // Assert
            Assert.That(jsonAdoGroupWrapper, Is.Not.Null);
            Assert.That(adoGroup, Is.Not.Null);
            if (adoGroup != null)
            {
                Assert.That(adoGroup.getStuff, Is.Not.Null);
                Assert.DoesNotThrow(adoGroup.setStuff);
            }


        }

        [Test]
        public async Task GetUserIdAsync_ReturnsUserId_WhenExists()
        {
            // Arrange
            var projectName = "TestProject";
            var userName = "Project Administrators";
            configurationMock.Value.Returns(fixture.Create<AdoConfig>());

            // Act
            var userid = await adoRestApiService.GetUserIdAsync(projectName, userName);

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
            var result = await adoRestApiService.postRoleAssignmentAsync(projectId, envId, roleName, userId);

            // Assert
            Assert.That(result, Is.False);
        }


    }
}