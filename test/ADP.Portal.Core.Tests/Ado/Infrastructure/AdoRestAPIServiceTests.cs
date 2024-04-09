using ADP.Portal.Api.Models.Ado;
using ADP.Portal.Core.Ado.Client;
using ADP.Portal.Core.Ado.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
namespace ADP.Portal.Core.Tests.Ado.Infrastructure
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
        private readonly IVssConnection vssConnectionMock;
        private readonly AdoRestHttpClient adoRestHttpClientMock;
        private readonly MockHttpMessageHandler httpMessageHandlerMock;
        private readonly AdoRestApiService adoRestApiService;
        private readonly string organizationUrl;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
            vssConnectionMock.ClearReceivedCalls();

        }
        public AdoRestApiServiceTests()
        {
            organizationUrl = "https://dev.azure.com/defragovuk";
            httpMessageHandlerMock = Substitute.ForPartsOf<MockHttpMessageHandler>();
            vssConnectionMock = Substitute.For<IVssConnection>();
            adoRestHttpClientMock = Substitute.For<AdoRestHttpClient>(new Uri(organizationUrl), new VssCredentials());
            adoRestHttpClientMock.GetHttpClient().Returns(new HttpClient(httpMessageHandlerMock));
            vssConnectionMock.GetClient<AdoRestHttpClient>().Returns(adoRestHttpClientMock);
            loggerMock = Substitute.For<ILogger<AdoRestApiService>>();

            adoRestApiService = new AdoRestApiService(loggerMock, Task.FromResult(vssConnectionMock));

        }


        [Test]
        public void Constructor_WithValidParameters_SetsVssConnection()
        {
            // Arrange

            // Act
            var restAPIService = new AdoRestApiService(loggerMock, Task.FromResult(vssConnectionMock));

            // Assert
            Assert.That(restAPIService, Is.Not.Null);
        }

        [Test]
        public void AdoRestHttpClientConstructor_WithValidParameters()
        {
            // Arrange

            // Act
            var restAPIService = new AdoRestHttpClient(new Uri(organizationUrl), new VssCredentials());
            var restAPIService2 = new AdoRestHttpClient(new Uri(organizationUrl), new VssCredentials(), new VssHttpRequestSettings());
            var restAPIService3 = new AdoRestHttpClient(new Uri(organizationUrl), httpMessageHandlerMock, true);

            // Assert
            Assert.That(restAPIService, Is.Not.Null);
            Assert.That(restAPIService.GetHttpClient(), Is.Not.Null);
            Assert.That(restAPIService2, Is.Not.Null);
            Assert.That(restAPIService3, Is.Not.Null);
        }

        [Test]
        public void Validate_AdoSecurityRole_Model()
        {
            // Arrange
            const string data = @"{""count"" : 1 , ""value"" : [ { ""identity"" : { ""id"" : ""454353"", ""displayName"" : ""testName"", ""uniqueName"" : ""testvalue"" }, ""role"" : { ""name"" : ""testName"" }  } ] } ";
            // Act
            var jsonAdoSecurityRoleWrapper = JsonConvert.DeserializeObject<JsonAdoSecurityRoleWrapper>(data);
            var adoSecurityRoleObj = (jsonAdoSecurityRoleWrapper != null && jsonAdoSecurityRoleWrapper.value != null) ? jsonAdoSecurityRoleWrapper.value[0] : null;

            // Assert
            Assert.That(jsonAdoSecurityRoleWrapper, Is.Not.Null);
            Assert.That(adoSecurityRoleObj, Is.Not.Null);

        }

        [Test]
        public async Task GetRoleAssignmentAsync_AdministratorTest()
        {
            // Arrange
            string projectId = Guid.NewGuid().ToString();
            string envId = Guid.NewGuid().ToString();
            const string data = @"{""count"" : 1 , ""value"" : [ { ""identity"" : { ""id"" : ""454353"", ""displayName"" : ""[Test]\\Project Administrators"", ""uniqueName"" : ""admin"" }, ""role"" : { ""name"" : ""User"" }  } ] } ";
            var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);

            // Act
            List<AdoSecurityRole> result = await adoRestApiService.GetRoleAssignmentAsync(projectId, envId);

            // Assert          
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].roleName, Is.EqualTo("Administrator"));
            Assert.That(result[0].userId, Is.EqualTo("454353"));

        }

        [Test]
        public async Task GetRoleAssignmentAsync_ReaderTest()
        {
            // Arrange
            string projectId = Guid.NewGuid().ToString();
            string envId = Guid.NewGuid().ToString();
            const string data = @"{""count"" : 1 , ""value"" : [ { ""identity"" : { ""id"" : ""1234"", ""displayName"" : ""Project Valid Users"", ""uniqueName"" : ""Project user"" }, ""role"" : { ""name"" : ""User"" }  } ] } ";
            var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);

            // Act
            List<AdoSecurityRole> result = await adoRestApiService.GetRoleAssignmentAsync(projectId, envId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].roleName, Is.EqualTo("Reader"));
            Assert.That(result[0].userId, Is.EqualTo("1234"));
        }

        [Test]
        public async Task GetRoleAssignmentAsync_ContributorsTest()
        {
            // Arrange
            string projectId = Guid.NewGuid().ToString();
            string envId = Guid.NewGuid().ToString();
            const string data = @"{""count"" : 1 , ""value"" : [ { ""identity"" : { ""id"" : ""34564"", ""displayName"" : ""Contributors"", ""uniqueName"" : ""Contributors"" }, ""role"" : { ""name"" : ""Reader"" }  } ] } ";
            var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);

            // Act
            List<AdoSecurityRole> result = await adoRestApiService.GetRoleAssignmentAsync(projectId, envId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].roleName, Is.EqualTo("User"));
            Assert.That(result[0].userId, Is.EqualTo("34564"));
        }


        [Test]
        public async Task postRoleAssignmentAsync_ReturnsSuccess()
        {
            // Arrange
            string projectId = Guid.NewGuid().ToString();
            string envId = Guid.NewGuid().ToString();
            var message = new HttpResponseMessage(HttpStatusCode.OK);
            httpMessageHandlerMock.MockSend(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(message);
            List<AdoSecurityRole> adoSecurityRoleList = new();
            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = "Administrator", userId = "1" });
            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = "Reader", userId = "2" });
            adoSecurityRoleList.Add(new AdoSecurityRole { roleName = "User", userId = "3" });
            // Act
            var result = await adoRestApiService.UpdateRoleAssignmentAsync(projectId, envId, adoSecurityRoleList);

            // Assert
            Assert.That(result, Is.True);
        }


    }
}