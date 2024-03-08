using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Ado.Services
{
    [TestFixture]
    public class AdoProjectServiceTests
    {
        private readonly IAdoService adoServiceMock;
        private readonly ILogger<AdoProjectService> loggerMock;
        private readonly AdoProjectService adoProjectService;

        public AdoProjectServiceTests()
        {
            adoServiceMock = Substitute.For<IAdoService>();
            loggerMock = Substitute.For<ILogger<AdoProjectService>>();
            adoProjectService = new AdoProjectService(adoServiceMock, loggerMock);
        }

        [Test]
        public void Constructor_WithValidParameters_SetsAdoService()
        {
            // Act
            var projectService = new AdoProjectService(adoServiceMock, loggerMock);

            // Assert
            Assert.That(projectService, Is.Not.Null);
        }

        [Test]
        public async Task GetProjectAsync_ReturnsProject_WhenProjectExists()
        {
            // Arrange
            var projectName = "TestProject";
            var project = new TeamProject();
            adoServiceMock.GetTeamProjectAsync(projectName).Returns(project);

            // Act
            var result = await adoProjectService.GetProjectAsync(projectName);

            // Assert
            Assert.That(result, Is.EqualTo(project));
        }

        [Test]
        public async Task GetProjectAsync_ReturnsNull_WhenProjectDoesNotExist()
        {
            // Arrange
            var projectName = "TestProject";

            adoServiceMock.GetTeamProjectAsync(projectName).ThrowsAsync<ProjectDoesNotExistWithNameException>();

            // Act
            var result = await adoProjectService.GetProjectAsync(projectName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task OnBoardAsync_CallsAdoServiceMethods()
        {
            var adpProjectName = "TestProject";
            var onboardProject = new AdoProject(Substitute.For<TeamProjectReference>(),
                Substitute.For<List<string>>(), Substitute.For<List<string>>(), Substitute.For<List<AdoEnvironment>>(), Substitute.For<List<AdoVariableGroup>>()
                );

            var fixture = new Fixture();
            onboardProject.VariableGroups = fixture.Build<AdoVariableGroup>()
                .CreateMany(2).ToList();

            // Act
            await adoProjectService.OnBoardAsync(adpProjectName, onboardProject);

            // Assert
            await adoServiceMock.Received(1).ShareServiceEndpointsAsync(adpProjectName, onboardProject.ServiceConnections, onboardProject.ProjectReference);
            await adoServiceMock.Received(1).AddEnvironmentsAsync(onboardProject.Environments, onboardProject.ProjectReference);
            await adoServiceMock.Received(1).ShareAgentPoolsAsync(adpProjectName, onboardProject.AgentPools, onboardProject.ProjectReference);
            if (onboardProject.VariableGroups != null)
            {
                await adoServiceMock.Received(1).AddOrUpdateVariableGroupsAsync(onboardProject.VariableGroups, onboardProject.ProjectReference);
            }
        }
    }
}