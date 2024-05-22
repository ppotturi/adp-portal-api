using ADP.Portal.Api.Config;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.ConfigTests;
public class AppInsightsConfigTests
{
    [TestFixture]
    public class AadGroupControllerTests
    {
        [Test]
        public void AppInsightsConfig_Should_BeDefined()
        {
            // Act
            var config = new AppInsightsConfig
            {
                ConnectionString = "your_connection_string",
                CloudRole = "your_cloud_role"
            };

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.ConnectionString, Is.Not.Empty);
            Assert.That(config.CloudRole, Is.Not.Empty);
        }
    }
}