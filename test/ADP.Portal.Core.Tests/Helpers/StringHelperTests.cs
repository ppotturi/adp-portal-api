using ADP.Portal.Core.Helpers;
using NUnit.Framework;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Tests.Helpers
{
    [TestFixture]
    public class StringHelperTests
    {
        [Test]
        public void On_ReplaceFirst_Test()
        {
            // Arrange
            const string url = "https://dev.azure.com/org";
            const string stringToReplace = "dev.azure.com";
            const string stringToReplaceWith = "vssps.dev.azure.com";
            const string newurl = "https://vssps.dev.azure.com/org";

            StringHelper sh = new();

            // Act
            var actualValue = sh.ReplaceFirst(url, stringToReplace, stringToReplaceWith);

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue, Is.EqualTo(newurl));
        }

        public void On_ReplaceFirstEmpty_Test()
        {
            // Arrange
            const string url = "https://dev.azure.com/org";
            const string stringToReplace = "test";
            const string stringToReplaceWith = "vssps.dev.azure.com";
            const string newurl = "https://dev.azure.com/org";

            StringHelper sh = new();

            // Act
            var actualValue = sh.ReplaceFirst(url, stringToReplace, stringToReplaceWith);

            // Assert
            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue, Is.EqualTo(newurl));
        }

    }
}
