using ADP.Portal.Core.Git.Jwt;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Text;

namespace ADP.Portal.Core.Tests.Git.Jwt
{
    [TestFixture]
    public class JwtTokenHelperTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CreateEncodedJwtToken_GivenValidInputs_ShouldReturnValidJwtToken(bool timeSpan)
        {
            // Arrange
            var privateKeyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(GenerateTestPrivateKey()));
            var githubAppId = 123;
            var expirationSeconds = 600;
            TimeSpan? iatOffset = timeSpan ? TimeSpan.Zero : null;

            // Act
            var token = JwtTokenHelper.CreateEncodedJwtToken(privateKeyBase64, githubAppId, expirationSeconds, iatOffset);

            // Assert
            Assert.That(token, Is.Not.Null);
            Assert.That(token, Is.Not.Empty);

        }

        public static string GenerateTestPrivateKey()
        {
            using var rsa = RSA.Create(2048);
            var privateKeyBytes = rsa.ExportRSAPrivateKey();
            var base64PrivateKey = Convert.ToBase64String(privateKeyBytes);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
            stringBuilder.AppendLine(base64PrivateKey);
            stringBuilder.AppendLine("-----END RSA PRIVATE KEY-----");

            return stringBuilder.ToString();
        }
    }
}
