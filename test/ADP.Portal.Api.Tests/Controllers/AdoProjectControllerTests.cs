using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Services;
using AutoFixture;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;


namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class AdoProjectControllerTests
    {
        private readonly AdoProjectController controller;
        private readonly ILogger<AdoProjectController> loggerMock;
        private readonly IOptions<AdpAdoProjectConfig> configMock;
        private readonly IAdoProjectService serviceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public AdoProjectControllerTests()
        {
            loggerMock = Substitute.For<ILogger<AdoProjectController>>();
            configMock = Substitute.For<IOptions<AdpAdoProjectConfig>>();
            serviceMock = Substitute.For<IAdoProjectService>();
            controller = new AdoProjectController(loggerMock, configMock, serviceMock);
        }

        [Test]
        public async Task GetAdoProject_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            string projectName = "test";
            TeamProjectReference? project = null;
            serviceMock.GetProjectAsync(projectName).Returns(project);

            // Act
            var result = await controller.GetAdoProject(projectName);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }

        }

        [Test]
        public async Task GetAdoProject_ReturnsOk_WhenProjectExists()
        {
            // Arrange
            string projectName = "test";
            var fixture = new Fixture();
            var project = fixture.Build<TeamProjectReference>().Create();
            serviceMock.GetProjectAsync(projectName).Returns(project);

            // Act
            var result = await controller.GetAdoProject(projectName);
            var okFoundResults = result as OkObjectResult;

            // Assert
            Assert.That(okFoundResults, Is.Not.Null);
            if (okFoundResults != null)
            {
                Assert.That(okFoundResults.StatusCode, Is.EqualTo(200));
            }
        }

        [Test]
        public async Task OnBoardAsync_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            string projectName = "test";
            var fixture = new Fixture();
            var onBoardRequest = fixture.Build<OnBoardAdoProjectRequest>().Create();
            TeamProjectReference? project = null;
            serviceMock.GetProjectAsync(projectName).Returns(project);

            // Act
            var result = await controller.OnBoardAsync(projectName, onBoardRequest);
            var notFoundResults = result as NotFoundResult;

            // Assert
            Assert.That(notFoundResults, Is.Not.Null);
            if (notFoundResults != null)
            {
                Assert.That(notFoundResults.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task OnBoardAsync_Update_WhenProjectExist()
        {
            // Arrange
            string projectName = "test";
            var fixture = new Fixture();
            var onBoardRequest = fixture.Build<OnBoardAdoProjectRequest>().Create();
            var project = fixture.Build<TeamProjectReference>().Create();
            var adpAdoProjectConfig = fixture.Build<AdpAdoProjectConfig>().Create();
            serviceMock.GetProjectAsync(projectName).Returns(project);
            serviceMock.OnBoardAsync(projectName, Arg.Any<AdoProject>()).Returns(Task.CompletedTask);
            configMock.Value.Returns(adpAdoProjectConfig);

            // Act
            var result = await controller.OnBoardAsync(projectName, onBoardRequest);
            var noContentResult = result as NoContentResult;

            // Assert
            Assert.That(noContentResult, Is.Not.Null);
            await serviceMock.Received().OnBoardAsync(Arg.Any<string>(), Arg.Any<AdoProject>());
            if (noContentResult != null)
            {
                Assert.That(noContentResult.StatusCode, Is.EqualTo(204));
            }
        }
    }
}
