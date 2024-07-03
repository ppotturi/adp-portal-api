using ADP.Portal.Core.Ado.Entities;
using ADP.Portal.Core.Ado.Infrastructure;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using DistributedTask = Microsoft.TeamFoundation.DistributedTask.WebApi;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ProjectReference = Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ProjectReference;


namespace ADP.Portal.Core.Tests.Ado.Infrastructure
{
    [TestFixture]
    public class AdoServiceTests
    {
        private readonly IVssConnection vssConnectionMock;
        private readonly ServiceEndpointHttpClient serviceEndpointClientMock;
        private readonly DistributedTask.TaskAgentHttpClient taskAgentClientMock;
        private readonly IAdoRestApiService adoRestApiServiceMock;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig<AdoVariableGroup, DistributedTask.VariableGroupParameters>.NewConfig()
                .Map(dest => dest.VariableGroupProjectReferences, src => new List<DistributedTask.VariableGroupProjectReference>() { new() { Name = src.Name, Description = src.Description } })
                .Map(dest => dest.Variables, src => src.Variables.ToDictionary(v => v.Name, v => new DistributedTask.VariableValue(v.Value, v.IsSecret)));

            vssConnectionMock.ClearReceivedCalls();
            serviceEndpointClientMock.ClearReceivedCalls();
            taskAgentClientMock.ClearReceivedCalls();
        }

        public AdoServiceTests()
        {
            vssConnectionMock = Substitute.For<IVssConnection>();
            adoRestApiServiceMock = Substitute.For<IAdoRestApiService>();
            serviceEndpointClientMock = Substitute.For<ServiceEndpointHttpClient>(new Uri("https://mock"), new VssCredentials());
            taskAgentClientMock = Substitute.For<DistributedTask.TaskAgentHttpClient>(new Uri("https://mock"), new VssCredentials());
        }

        [Test]
        public void Constructor_WithValidParameters_SetsAdoService()
        {
            // Arrange
            var logger = Substitute.For<ILogger<AdoService>>();


            // Act
            var projectService = new AdoService(logger, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Assert
            Assert.That(projectService, Is.Not.Null);
        }

        [Test]
        public async Task GetTeamProjectAsync_ReturnsTeamProject()
        {
            // Arrange
            var expectedProject = new TeamProject { Name = "TestProject" };
            var mockProjectClient = Substitute.For<ProjectHttpClient>(new Uri("https://mock"), new VssCredentials());
            mockProjectClient.GetProject(Arg.Any<string>(), null, false, null).Returns(expectedProject);
            vssConnectionMock.GetClientAsync<ProjectHttpClient>(Arg.Any<CancellationToken>()).Returns(mockProjectClient);
            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            var result = await adoService.GetTeamProjectAsync("TestProject");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(expectedProject.Name, Is.EqualTo(result.Name));
        }

        [Test]
        public void GetTeamProjectAsync_WithNonexistentProject_ReturnsNull()
        {
            // Arrange
            var mockProjectClient = Substitute.For<ProjectHttpClient>(new Uri("https://mock"), new VssCredentials());
            mockProjectClient.GetProject(Arg.Any<string>(), null, false, null)
                .ThrowsAsync<ProjectDoesNotExistWithNameException>();
            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            vssConnectionMock.GetClientAsync<ProjectHttpClient>(Arg.Any<CancellationToken>()).Returns(mockProjectClient);

            // Assert
            Assert.ThrowsAsync<ProjectDoesNotExistWithNameException>(async () => await adoService.GetTeamProjectAsync("NonexistentProject"));
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_CallsShareServiceEndpointAsync()
        {
            // Arrange
            var fixture = new Fixture();
            var serviceEndpointProjectReferences = fixture.Build<ServiceEndpointProjectReference>()
                .With(reference => reference.Name, "TestProject")
                .With(reference => reference.ProjectReference, new ProjectReference { Id = Guid.NewGuid() })
                .OmitAutoProperties()
                .CreateMany(1).ToList();
            var serviceEndpoint = fixture.Build<ServiceEndpoint>()
                .With(endpoint => endpoint.Id, Guid.NewGuid())
                .With(endpoint => endpoint.Name, "TestServiceEndpoint")
                .With(endpoint => endpoint.Type, "git")
                .With(endpoint => endpoint.Url, new Uri("https://example.com"))
                .With(endpoint => endpoint.ServiceEndpointProjectReferences, serviceEndpointProjectReferences)
                .OmitAutoProperties()
                .CreateMany(1).ToList();

            serviceEndpointClientMock.GetServiceEndpointsAsync("TestProject", null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
                .Returns(serviceEndpoint);

            serviceEndpointClientMock.ShareServiceEndpointAsync(Arg.Any<Guid>(), Arg.Any<List<ServiceEndpointProjectReference>>(), null, Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            vssConnectionMock.GetClientAsync<ServiceEndpointHttpClient>(Arg.Any<CancellationToken>())
                .Returns(serviceEndpointClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.ShareServiceEndpointsAsync("TestProject", new List<string> { "TestServiceEndpoint" }, new TeamProjectReference { Id = Guid.NewGuid() });

            // Assert
            await serviceEndpointClientMock.Received(1).ShareServiceEndpointAsync(Arg.Any<Guid>(), Arg.Any<List<ServiceEndpointProjectReference>>(), null, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_LogsInformationMessage_WhenIsAlreadySharedIsTrue()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var serviceConnections = new List<string> { "TestServiceConnection" };
            var onBoardProject = new TeamProjectReference { Name = "TestOnBoardProject", Id = Guid.NewGuid() };
            var serviceEndpoint = new ServiceEndpoint
            {
                Name = "TestServiceConnection",
                ServiceEndpointProjectReferences = new List<ServiceEndpointProjectReference>
                    {
                        new() { ProjectReference = new ProjectReference { Id = onBoardProject.Id } }
                    }
            };

            serviceEndpointClientMock.GetServiceEndpointsAsync(adpProjectName, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
                .Returns(new List<ServiceEndpoint> { serviceEndpoint });

            vssConnectionMock.GetClientAsync<ServiceEndpointHttpClient>(Arg.Any<CancellationToken>())
                .Returns(serviceEndpointClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.ShareServiceEndpointsAsync(adpProjectName, serviceConnections, onBoardProject);

            // Assert
            loggerMock.Received(1).Log(
                Arg.Is<LogLevel>(l => l == LogLevel.Information),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == $"Service endpoint {serviceEndpoint.Name} already shared with project {onBoardProject.Name}"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_LogsWarningMessage_WhenEndpointIsNull()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var serviceConnections = new List<string> { "NonExistentServiceConnection" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid() };
            var serviceEndpoint = new ServiceEndpoint
            {
                Name = "TestServiceConnection",
                ServiceEndpointProjectReferences = new List<ServiceEndpointProjectReference>
                   {
                        new() { ProjectReference = new ProjectReference { Id = onBoardProject.Id } }
                   }
            };
            serviceEndpointClientMock.GetServiceEndpointsAsync(adpProjectName, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
                .Returns(new List<ServiceEndpoint> { serviceEndpoint });

            vssConnectionMock.GetClientAsync<ServiceEndpointHttpClient>(Arg.Any<CancellationToken>())
               .Returns(serviceEndpointClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.ShareServiceEndpointsAsync(adpProjectName, serviceConnections, onBoardProject);

            // Assert
            loggerMock.Received(1).Log(
                Arg.Is<LogLevel>(l => l == LogLevel.Warning),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == $"Service endpoint {serviceConnections[0]} not found"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_ReturnsExistingEndpointIds()
        {
            // Arrange - given the ADO project to onboard and the service connections to add...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid() };
            var serviceConnections = new List<string> { "TestServiceConnection1", "TestServiceConnection2" };

            // Arrange - ...and given an existing reference project and service connections...
            var adpProjectName = "TestProject";
            var adpProjectId = Guid.NewGuid();
            var serviceEndpoints = new List<ServiceEndpoint>
            {
                new()
                {
                    Name = "TestServiceConnection1",
                    Id = Guid.NewGuid(),
                    ServiceEndpointProjectReferences = [ new() { ProjectReference = new ProjectReference { Id = adpProjectId } } ]
                },
                new()
                {
                    Name = "TestServiceConnection2",
                    Id = Guid.NewGuid(),
                    ServiceEndpointProjectReferences = [ new() { ProjectReference = new ProjectReference { Id = adpProjectId } } ]
                },
                new()
                {
                    Name = "TestServiceConnection3",
                    Id = Guid.NewGuid(),
                    ServiceEndpointProjectReferences = [ new() { ProjectReference = new ProjectReference { Id = adpProjectId } } ]
                },
            };

            serviceEndpointClientMock.GetServiceEndpointsAsync(adpProjectName, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
                .Returns(serviceEndpoints);
            vssConnectionMock.GetClientAsync<ServiceEndpointHttpClient>(Arg.Any<CancellationToken>())
               .Returns(serviceEndpointClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when sharing service endpoints from onBoardProject to adpProjectName...
            var endpontIds = await adoService.ShareServiceEndpointsAsync(adpProjectName, serviceConnections, onBoardProject);

            // Assert - ...the service connection IDs required for onboarding should be returned.
            var expectedEndpointIds = serviceEndpoints.Where(e => serviceConnections.Exists(c => c == e.Name)).Select(e => e.Id);
            Assert.That(endpontIds, Is.EquivalentTo(expectedEndpointIds));
        }

        [Test]
        public async Task ShareServiceEndpointsAsync_LogsWarningMessage_WhenEndpointAlreadyExists()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var serviceConnections = new List<string> { "ExistingServiceConnection" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "Dummy" };
            var serviceEndpoint = new ServiceEndpoint
            {
                Name = "ExistingServiceConnection",
                Id = Guid.NewGuid(),
                ServiceEndpointProjectReferences = []
            };
            serviceEndpointClientMock.GetServiceEndpointsAsync(Arg.Any<string>(), null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
                .Returns([serviceEndpoint]);

            vssConnectionMock.GetClientAsync<ServiceEndpointHttpClient>(Arg.Any<CancellationToken>())
               .Returns(serviceEndpointClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            var endpontIds = await adoService.ShareServiceEndpointsAsync(adpProjectName, serviceConnections, onBoardProject);

            // Assert
            loggerMock.Received(1).Log(
                Arg.Is<LogLevel>(l => l == LogLevel.Information),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == $"Service endpoint {serviceConnections[0]} already exists in the {onBoardProject.Name} project"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());

            Assert.That(endpontIds.First(), Is.EqualTo(serviceEndpoint.Id));
        }

        [Test]
        public async Task AddEnvironmentsAsync_LogsInformationMessage_WhenEnvironmentExists()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoEnvironments = new List<AdoEnvironment> { new("TestEnvironment", "") };

            var environments = new List<DistributedTask.EnvironmentInstance> { new DistributedTask.EnvironmentInstance { Name = "TestEnvironment" } };

            taskAgentClientMock.GetEnvironmentsAsync(onBoardProject.Id, null, null, null, null, Arg.Any<CancellationToken>()).Returns(environments);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
               .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.AddEnvironmentsAsync(adoEnvironments, onBoardProject);

            // Assert
            loggerMock.Received(1).Log(
                Arg.Is<LogLevel>(l => l == LogLevel.Information),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == $"Environment {adoEnvironments[0].Name} already exists"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Test]
        public async Task AddEnvironmentsAsync_CreatesEnvironment_WhenEnvironmentDoesNotExist()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoEnvironments = new List<AdoEnvironment> { new("TestEnvironment", "") };
            var environments = new List<DistributedTask.EnvironmentInstance>();

            taskAgentClientMock.GetEnvironmentsAsync(onBoardProject.Id, null, null, null, null, Arg.Any<CancellationToken>()).Returns(environments);
            taskAgentClientMock.AddEnvironmentAsync(onBoardProject.Id, Arg.Any<DistributedTask.EnvironmentCreateParameter>(), null, Arg.Any<CancellationToken>()).Returns(x => new DistributedTask.EnvironmentInstance { Name = x.Arg<DistributedTask.EnvironmentCreateParameter>().Name, Id = 123 });
            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
               .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.AddEnvironmentsAsync(adoEnvironments, onBoardProject);

            // Assert
            await taskAgentClientMock.Received(1).AddEnvironmentAsync(onBoardProject.Id, Arg.Any<DistributedTask.EnvironmentCreateParameter>(), null, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task AddEnvironmentsAsync_ReturnsExistingEnvironmentIds()
        {
            // Arrange - given the ADO project to onboard and the environments to add...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoEnvironments = new List<AdoEnvironment> { new("TestEnvironment1", ""), new("TestEnvironment2", ""), new("TestEnvironment3", "") };

            // Arrange - ...and given the environments have already been added to the project...
            var environments = new List<DistributedTask.EnvironmentInstance>
            {
                new DistributedTask.EnvironmentInstance { Name = "TestEnvironment1", Id = 111 },
                new DistributedTask.EnvironmentInstance { Name = "TestEnvironment2", Id = 222 },
                new DistributedTask.EnvironmentInstance { Name = "TestEnvironment3", Id = 333 }
            };

            taskAgentClientMock.GetEnvironmentsAsync(onBoardProject.Id, null, null, null, null, Arg.Any<CancellationToken>()).Returns(environments);
            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
               .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when we attempt to add environments to the project...
            var environmentIds = await adoService.AddEnvironmentsAsync(adoEnvironments, onBoardProject);

            // Assert - ...the environment IDs are returned.
            var expectedEnvironmentIds = environments.Select(e => e.Id);
            Assert.That(environmentIds, Is.EquivalentTo(expectedEnvironmentIds));
        }

        [Test]
        public async Task AddEnvironmentsAsync_ReturnsNewEnvironmentIds()
        {
            // Arrange - given the ADO project to onboard and the environments to add...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoEnvironments = new List<AdoEnvironment> { new("TestEnvironment1", ""), new("TestEnvironment2", ""), new("TestEnvironment3", "") };

            // Arrange - ...and given the project does not already have environments configured...
            var environments = new List<DistributedTask.EnvironmentInstance>();

            taskAgentClientMock.GetEnvironmentsAsync(onBoardProject.Id, null, null, null, null, Arg.Any<CancellationToken>()).Returns(environments);
            taskAgentClientMock.AddEnvironmentAsync(onBoardProject.Id, Arg.Any<DistributedTask.EnvironmentCreateParameter>(), null, Arg.Any<CancellationToken>()).Returns(x => new DistributedTask.EnvironmentInstance { Name = x.Arg<DistributedTask.EnvironmentCreateParameter>().Name, Id = 123 });
            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
               .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when we attempt to add environments to the project...
            var environmentIds = await adoService.AddEnvironmentsAsync(adoEnvironments, onBoardProject);

            // Assert - ...the environment IDs are returned.
            Assert.That(environmentIds, Has.Exactly(adoEnvironments.Count).Items);
        }

        [Test]
        public async Task ShareAgentPoolsAsync_LogsInformationMessage_WhenAgentPoolExists()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var adoAgentPoolsToShare = new List<string> { "TestAgentPool" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adpAgentQueues = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool" } };
            var agentPools = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool" } };

            taskAgentClientMock.GetAgentQueuesAsync(adpProjectName, string.Empty, null, null, Arg.Any<CancellationToken>()).Returns(adpAgentQueues);
            taskAgentClientMock.GetAgentQueuesAsync(onBoardProject.Id, null, null, null, Arg.Any<CancellationToken>()).Returns(agentPools);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.ShareAgentPoolsAsync(adpProjectName, adoAgentPoolsToShare, onBoardProject);

            // Assert
            loggerMock.Received(1).Log(
                Arg.Is<LogLevel>(l => l == LogLevel.Information),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == $"Agent pool {adoAgentPoolsToShare[0]} already exists in the {onBoardProject.Name} project"),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Test]
        public async Task ShareAgentPoolsAsync_CreatesAgentPool_WhenAgentPoolDoesNotExist()
        {
            // Arrange
            var adpProjectName = "TestProject";
            var adoAgentPoolsToShare = new List<string> { "TestAgentPool" };
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adpAgentQueues = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool" } };
            var agentPools = new List<DistributedTask.TaskAgentQueue>();

            taskAgentClientMock.GetAgentQueuesAsync(adpProjectName, string.Empty, null, null, Arg.Any<CancellationToken>()).Returns(adpAgentQueues);
            taskAgentClientMock.GetAgentQueuesAsync(onBoardProject.Id, null, null, null, Arg.Any<CancellationToken>()).Returns(agentPools);
            taskAgentClientMock.AddAgentQueueAsync(onBoardProject.Id, Arg.Any<DistributedTask.TaskAgentQueue>()).Returns(new DistributedTask.TaskAgentQueue { Id = 111 });

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.ShareAgentPoolsAsync(adpProjectName, adoAgentPoolsToShare, onBoardProject);

            // Assert
            await taskAgentClientMock.Received(1).AddAgentQueueAsync(onBoardProject.Id, Arg.Any<DistributedTask.TaskAgentQueue>(), null, null, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ShareAgentPoolsAsync_ReturnsExistingAgentQueueIds_WhenAgentPoolAlreadySharedWithProject()
        {
            // Arrange - given the ADO project to onboard and agent pools to share...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoAgentPoolsToShare = new List<string> { "TestAgentPool1" };

            // Arrange - ...and the agent pools have already been shared with the project...
            var adpProjectName = "TestProject";
            var adpAgentQueues = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool1", Id = 111 } };
            var onboardProjectAgentPools = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool1", Id = 111 } };

            taskAgentClientMock.GetAgentQueuesAsync(adpProjectName, string.Empty, null, null, Arg.Any<CancellationToken>()).Returns(adpAgentQueues);
            taskAgentClientMock.GetAgentQueuesAsync(onBoardProject.Id, null, null, null, Arg.Any<CancellationToken>()).Returns(onboardProjectAgentPools);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when sharing agent pools from adpProject to onboardProject...
            var agentPoolIds = await adoService.ShareAgentPoolsAsync(adpProjectName, adoAgentPoolsToShare, onBoardProject);

            // Assert - ...the existing agent pool IDs are returned.
            var expectedAgentPoolIds = onboardProjectAgentPools.Select(p => p.Id);
            Assert.That(agentPoolIds, Is.EquivalentTo(expectedAgentPoolIds));
        }

        public async Task ShareAgentPoolsAsync_AgentQueueIds_WhenAgentPoolHasNotBeenShared()
        {
            // Arrange - given the ADO project to onboard and agent pools to share...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var adoAgentPoolsToShare = new List<string> { "TestAgentPool1" };

            var expectedAgentQueue = new DistributedTask.TaskAgentQueue
            {
                Id = 222,
                Name = "TestProject1"
            };

            // Arrange - ...and the agent pools have already been shared with the project...
            var adpProjectName = "TestProject";
            var adpAgentQueues = new List<DistributedTask.TaskAgentQueue> { new() { Name = "TestAgentPool1", Id = 111 } };
            var onboardProjectAgentPools = new List<DistributedTask.TaskAgentQueue>();

            taskAgentClientMock.GetAgentQueuesAsync(adpProjectName, string.Empty, null, null, Arg.Any<CancellationToken>()).Returns(adpAgentQueues);
            taskAgentClientMock.GetAgentQueuesAsync(onBoardProject.Id, null, null, null, Arg.Any<CancellationToken>()).Returns(onboardProjectAgentPools);
            taskAgentClientMock.AddAgentQueueAsync(onBoardProject.Id, Arg.Any<DistributedTask.TaskAgentQueue>()).Returns(expectedAgentQueue);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when sharing agent pools from adpProject to onboardProject...
            var agentPoolIds = await adoService.ShareAgentPoolsAsync(adpProjectName, adoAgentPoolsToShare, onBoardProject);

            // Assert - ...the new agent queue ID is returned.
            Assert.That(agentPoolIds, Contains.Item(expectedAgentQueue.Id));
        }

        [Test]
        public async Task AddOrUpdateVariableGroupsAsync_CreatesVariableGroup_WhenVariableGroupDoesNotExist()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var fixture = new Fixture();

            var adoVariables = fixture.Build<AdoVariable>().CreateMany(2).ToList();
            var adoVariableGroup = new AdoVariableGroup("TestVariableGroup", adoVariables, "TestVariableGroup Description");
            var adoVariableGroups = new List<AdoVariableGroup> { adoVariableGroup };
            var variableGroups = new List<DistributedTask.VariableGroup>();

            taskAgentClientMock.GetVariableGroupsAsync(onBoardProject.Id, null, null, null, null, null, null, Arg.Any<CancellationToken>()).Returns(variableGroups);
            taskAgentClientMock.AddVariableGroupAsync(Arg.Any<DistributedTask.VariableGroupParameters>()).Returns(new DistributedTask.VariableGroup { Id = 111, Name = "TestVariableGroup" });

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.AddOrUpdateVariableGroupsAsync(adoVariableGroups, onBoardProject);

            // Assert
            await taskAgentClientMock.Received(1).AddVariableGroupAsync(Arg.Any<DistributedTask.VariableGroupParameters>(), null, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task AddOrUpdateVariableGroupsAsync_UpdatesVariableGroup_WhenVariableGroupExists()
        {
            // Arrange
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var fixture = new Fixture();
            var adoVariables = fixture.Build<AdoVariable>().CreateMany(2).ToList();
            var adoVariableGroup = new AdoVariableGroup("TestVariableGroup", adoVariables, "TestVariableGroup Description");
            var adoVariableGroups = new List<AdoVariableGroup> { adoVariableGroup };
            var variableGroups = new List<DistributedTask.VariableGroup> { new DistributedTask.VariableGroup { Name = "TestVariableGroup", Id = 1 } };

            taskAgentClientMock.GetVariableGroupsAsync(onBoardProject.Id, null, null, null, null, null, null, Arg.Any<CancellationToken>()).Returns(variableGroups);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act
            await adoService.AddOrUpdateVariableGroupsAsync(adoVariableGroups, onBoardProject);

            // Assert
            await taskAgentClientMock.Received(1).UpdateVariableGroupAsync(Arg.Any<int>(), Arg.Any<DistributedTask.VariableGroupParameters>(), null, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task AddOrUpdateVariableGroupsAsync_ReturnsVariableGroupIds_ForExistingVariableGroup()
        {
            // Arrange - given the ADO project to onboard and variable groups to add...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var fixture = new Fixture();
            var adoVariables = fixture.Build<AdoVariable>().CreateMany(2).ToList();
            var adoVariableGroup = new AdoVariableGroup("TestVariableGroup", adoVariables, "TestVariableGroup Description");
            var adoVariableGroups = new List<AdoVariableGroup> { adoVariableGroup };

            // Arrange - ...and given the variable groups have already been added to the project...
            var existingVariableGroup = new DistributedTask.VariableGroup { Name = "TestVariableGroup", Id = 1 };

            taskAgentClientMock.GetVariableGroupsAsync(onBoardProject.Id, null, null, null, null, null, null, Arg.Any<CancellationToken>()).Returns(new List<DistributedTask.VariableGroup> { existingVariableGroup });
            taskAgentClientMock.UpdateVariableGroupAsync(existingVariableGroup.Id, Arg.Any<DistributedTask.VariableGroupParameters>()).Returns(existingVariableGroup);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when we attempt to update the variable groups...
            var variableGroupIds = await adoService.AddOrUpdateVariableGroupsAsync(adoVariableGroups, onBoardProject);

            // Assert - ...the existing variable group IDs are returned.
            Assert.That(variableGroupIds, Contains.Item(existingVariableGroup.Id));
        }

        [Test]
        public async Task AddOrUpdateVariableGroupsAsync_ReturnsVariableGroupIds_WhenVariableGroupsCreated()
        {
            // Arrange - given the ADO project to onboard and variable groups to add...
            var onBoardProject = new TeamProjectReference { Id = Guid.NewGuid(), Name = "TestProject" };
            var fixture = new Fixture();
            var adoVariables = fixture.Build<AdoVariable>().CreateMany(2).ToList();
            var adoVariableGroup = new AdoVariableGroup("TestVariableGroup", adoVariables, "TestVariableGroup Description");
            var adoVariableGroups = new List<AdoVariableGroup> { adoVariableGroup };

            // Arrange - ...and given the variable groups have not yet been added to the project...
            var expectedVariableGroup = new DistributedTask.VariableGroup { Name = "TestVariableGroup", Id = 1 };

            taskAgentClientMock.GetVariableGroupsAsync(onBoardProject.Id, null, null, null, null, null, null, Arg.Any<CancellationToken>()).Returns(new List<DistributedTask.VariableGroup>());
            taskAgentClientMock.AddVariableGroupAsync(Arg.Any<DistributedTask.VariableGroupParameters>()).Returns(expectedVariableGroup);

            vssConnectionMock.GetClientAsync<DistributedTask.TaskAgentHttpClient>(Arg.Any<CancellationToken>())
                .Returns(taskAgentClientMock);

            var loggerMock = Substitute.For<ILogger<AdoService>>();
            var adoService = new AdoService(loggerMock, Task.FromResult(vssConnectionMock), adoRestApiServiceMock);

            // Act - ...when we attempt to add the variable groups...
            var variableGroupIds = await adoService.AddOrUpdateVariableGroupsAsync(adoVariableGroups, onBoardProject);

            // Assert - ...the new variable group IDs are returned.
            Assert.That(variableGroupIds, Contains.Item(expectedVariableGroup.Id));
        }
    }
}