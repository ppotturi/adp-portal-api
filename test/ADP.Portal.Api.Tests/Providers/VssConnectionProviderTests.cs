using ADP.Portal.Api.Config;
using ADP.Portal.Api.Providers;
using ADP.Portal.Api.Wrappers;
using AutoFixture;
using Azure.Core;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace ADP.Portal.Api.Tests.Providers
{
    [TestFixture]
    public class VssConnectionProviderTests
    {
        private readonly IAzureCredential azureCredentialMock;
        private readonly string organizationUrl = "http://localhost";

        public VssConnectionProviderTests()
        {
            azureCredentialMock = Substitute.For<IAzureCredential>();
        }

        [Test]
        public async Task GetConnectionAsync_UsesPatToken_WhenUsePatTokenIsTrue()
        {
            //Arrange
            var fixture = new Fixture();
            var adoConfig = fixture.Build<AdoConfig>()
                .With(c => c.OrganizationUrl, organizationUrl).Create();
            var provider = new VssConnectionProvider(azureCredentialMock, adoConfig);

            // Act
            var result = await provider.GetConnectionAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetConnectionAsync_UsesAccessToken_WhenUsePatTokenIsFalse()
        {
            //Arrange
            var fixture = new Fixture();
            var adoConfig = fixture.Build<AdoConfig>()
                .With(c => c.OrganizationUrl, organizationUrl)
                .With(c => c.UsePatToken, false)
                .Create();
            var accessToken = fixture.Build<AccessToken>().Create();
            azureCredentialMock.GetTokenAsync(Arg.Any<TokenRequestContext>()).Returns(Task.FromResult(accessToken));

            var provider = new VssConnectionProvider(azureCredentialMock, adoConfig);

            // Act
            var result = await provider.GetConnectionAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
        }
    }
}