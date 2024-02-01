using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests
{
    [TestFixture]
    public class GlobalExceptionHandlerTests
    {
        private readonly ILogger<GlobalExceptionHandler> loggerMock;
        private readonly GlobalExceptionHandler handler;
        public GlobalExceptionHandlerTests()
        {
            loggerMock = Substitute.For<ILogger<GlobalExceptionHandler>>();
            handler = new GlobalExceptionHandler(loggerMock);
        }

        [Test]
        public async Task TryHandleAsync_LogsErrorAndWritesProblemDetails()
        {
            // Arrange
            var exception = new Exception("Test exception");
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Act
            var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);

            loggerMock.Received().Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                exception,
                Arg.Any<Func<object, Exception?, string>>());

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(httpContext.Response.Body).ReadToEnd();
            Assert.That(responseBody, Is.EquivalentTo("{\"title\":\"Server error\",\"status\":500}"));
        }
    }
}