﻿using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Core.Ado.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
namespace ADP.Portal.Core.Tests.Ado.Services
{

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(MockSend(request, cancellationToken));
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return MockSend(request, cancellationToken);
        }

        public virtual HttpResponseMessage MockSend(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class AdoRestApiServiceTests
    {
        private readonly ILogger<AdoRestApiService> loggerMock;
        private readonly MockHttpMessageHandler httpMessageHandlerMock;
        private readonly AdoRestApiService adoRestApiService;
        private readonly string organizationUrl;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        }
        public AdoRestApiServiceTests()
        {
            httpMessageHandlerMock = Substitute.ForPartsOf<MockHttpMessageHandler>();
            loggerMock = Substitute.For<ILogger<AdoRestApiService>>();
            organizationUrl = "https://dev.azure.com/defragovuk";
            adoRestApiService = new AdoRestApiService(loggerMock, organizationUrl, new HttpClient(httpMessageHandlerMock));

        }

        [Test]
        public void Constructor_WithValidParameters_SetsAdoRestApiService()
        {
            // Arrange

            // Act
            var restAPIService = new AdoRestApiService(loggerMock, organizationUrl, new HttpClient(httpMessageHandlerMock));

            // Assert
            Assert.That(restAPIService, Is.Not.Null);
        }
        [Test]
        public void Validate_AdoGroup_Model()
        {
            // Arrange
            const string data = @"{""count"" : 1 , ""value"" : [ { ""id"" : ""454353"", ""providerDisplayName"" : ""testName"", ""ExtensionData"" : ""testvalue"" } ] } ";
            // Act
            var jsonAdoGroupWrapper = JsonConvert.DeserializeObject<JsonAdoGroupWrapper>(data);
            var adoGroup = (jsonAdoGroupWrapper != null && jsonAdoGroupWrapper.value != null) ? jsonAdoGroupWrapper.value[0] : null;

            // Assert
            Assert.That(jsonAdoGroupWrapper, Is.Not.Null);
            Assert.That(adoGroup, Is.Not.Null);

        }

        [Test]
        public async Task GetUserIdAsync_ReturnsUserId_WhenExists()
        {
            // Arrange
            var projectName = "DEFRA-TRADE-PUBLIC";
            var userName = "Project Administrators";
            const string data = @"{""count"" : 1 , ""value"" : [ { ""id"" : ""454353"", ""providerDisplayName"" : ""testName"", ""ExtensionData"" : ""testvalue"" } ] } ";
            var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);

            // Act
            var userid = await adoRestApiService.GetUserIdAsync(projectName, userName);

            // Assert
            Assert.That(userid, Is.EqualTo("454353"));
        }

        [Test]
        public async Task GetUserIdAsync_ReturnsUserId_WhenNotExists()
        {
            // Arrange
            var projectName = "DEFRA-TRADE-PUBLIC";
            var userName = "Project Administrators";
            const string data = @"{""count"" : 0 , ""value"" : """" } ";
            var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);

            // Act
            var userid = await adoRestApiService.GetUserIdAsync(projectName, userName);

            // Assert
            Assert.That(userid, Is.EqualTo(""));
        }

        [Test]
        public async Task postRoleAssignmentAsync_ReturnsSuccess()
        {
            // Arrange
            string projectId = Guid.NewGuid().ToString();
            string envId = Guid.NewGuid().ToString();
            var message = new HttpResponseMessage(HttpStatusCode.OK);
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);

            // Act
            var result = await adoRestApiService.updateRoleAssignmentAsync(projectId, envId);

            // Assert
            Assert.That(result, Is.True);
        }


    }
}