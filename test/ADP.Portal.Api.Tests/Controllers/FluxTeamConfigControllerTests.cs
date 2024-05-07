using System.Reflection;
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Api.Mapster;
using ADP.Portal.Api.Models.Flux;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class FluxTeamConfigControllerTests
    {
        private readonly FluxTeamConfigController controller;
        private readonly ILogger<FluxTeamConfigController> loggerMock;
        private readonly IOptions<AzureAdConfig> azureAdConfigMock;
        private readonly IFluxTeamConfigService fluxTeamConfigServiceMock;
        private readonly Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
            MapsterConfig.Configure(Substitute.For<IServiceCollection>());
        }

        public FluxTeamConfigControllerTests()
        {
            azureAdConfigMock = Substitute.For<IOptions<AzureAdConfig>>();
            loggerMock = Substitute.For<ILogger<FluxTeamConfigController>>();
            fluxTeamConfigServiceMock = Substitute.For<IFluxTeamConfigService>();
            controller = new FluxTeamConfigController(fluxTeamConfigServiceMock, loggerMock, azureAdConfigMock);
            fixture = new Fixture();
        }

        [Test]
        public async Task Generate_Returns_Ok()
        {
            // Arrange
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.GenerateManifestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateManifestResult());

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }


        [Test]
        public async Task Generate_Returns_BadRequest_When_ConfigNotExists()
        {
            // Arrange
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.GenerateManifestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateManifestResult() { IsConfigExists = false, Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo($"Flux generator config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task GetServiceEnvironmentAsync_Returns_Ok()
        {
            // Arrange
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.GetServiceEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ServiceEnvironmentResult { FluxTemplatesVersion = "1.0.0" });

            // Act
            var result = await controller.GetServiceEnvironmentAsync("teamName", "service1", "environment1");

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task GenerateAsync_Returns_NotFound()
        {
            // Arrange
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.GetServiceEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new ServiceEnvironmentResult { IsConfigExists = false, FluxTemplatesVersion = "1.0.0" });

            // Act
            var result = await controller.GetServiceEnvironmentAsync("teamName", "service1", "environment1");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task SetEnvironmentManifestAsync_Returns_NoContent()
        {
            // Arrange
            var manifest = fixture.Build<ManifestConfigRequest>().Create();
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.UpdateServiceEnvironmentManifestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
                .Returns(new FluxConfigResult { IsConfigExists = true });

            // Act
            var result = await controller.SetEnvironmentManifestAsync("teamName", "service1", "environment1", manifest);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task SetEnvironmentManifestAsync_Returns_BadRequest_When_Config_NotFound()
        {
            // Arrange
            var manifest = fixture.Build<ManifestConfigRequest>().Create();
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.UpdateServiceEnvironmentManifestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
                .Returns(new FluxConfigResult { IsConfigExists = false, Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.SetEnvironmentManifestAsync("teamName", "service1", "environment1", manifest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo("Flux team config not found"));
            }
        }

        [Test]
        public async Task SetEnvironmentManifestAsync_Returns_BadRequest()
        {
            // Arrange
            var manifest = fixture.Build<ManifestConfigRequest>().Create();
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.UpdateServiceEnvironmentManifestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
                .Returns(new FluxConfigResult { IsConfigExists = true, Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.SetEnvironmentManifestAsync("teamName", "service1", "environment1", manifest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo(new List<string>() { "Flux team config not found" }));
            }
        }

        [Test]
        public async Task GenerateAsync_Returns_BadRequest_When_CompletedWithErros()
        {
            // Arrange
            azureAdConfigMock.Value.Returns(fixture.Build<AzureAdConfig>().Create());
            fluxTeamConfigServiceMock.GenerateManifestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new GenerateManifestResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.GenerateAsync("teamName", "service1");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreateConfigAsync_Returns_BadRequest()
        {
            // Arrange
            var request = fixture.Build<TeamConfigRequest>().Create();
            fluxTeamConfigServiceMock.CreateConfigAsync(Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.CreateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreateConfigAsync_Returns_Created()
        {
            // Arrange
            var request = fixture.Build<TeamConfigRequest>().Create();
            fluxTeamConfigServiceMock.CreateConfigAsync(Arg.Any<string>(), Arg.Any<FluxTeamConfig>())
                .Returns(new FluxConfigResult());

            // Act
            var result = await controller.CreateConfigAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            if (result != null)
            {
                var results = (CreatedResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task GetConfigAsync_Returns_Ok()
        {
            // Arrange
            var fluxTeam = fixture.Build<FluxTeamConfig>().Create();
            fluxTeamConfigServiceMock.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<string>())
                .Returns(fluxTeam);

            // Act
            var result = await controller.GetConfigAsync("teamName");

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            if (result != null)
            {
                var okResults = (OkObjectResult)result;
                var fluxTeamConfig = okResults.Value as FluxTeamConfig;

                Assert.That(okResults, Is.Not.Null);
                Assert.That(fluxTeamConfig, Is.Not.Null);
                Assert.That(fluxTeamConfig?.Services.Count, Is.EqualTo(fluxTeam.Services.Count));
            }
        }

        [Test]
        public async Task GetConfigAsync_Returns_NotFound_WhenFluxConfigNotFound()
        {
            // Arrange
            fluxTeamConfigServiceMock.GetConfigAsync<FluxTeamConfig>(Arg.Any<string>(), Arg.Any<string>())
                .ReturnsNull();

            // Act
            var result = await controller.GetConfigAsync("teamName");

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }


        [Test]
        public async Task CreateServiceAsync_Returns_BadRequest_When_Errors()
        {
            // Arrange
            var request = fixture.Build<ServiceConfigRequest>().Create();
            fluxTeamConfigServiceMock.AddServiceAsync(Arg.Any<string>(), Arg.Any<Core.Git.Entities.FluxService>())
                .Returns(new FluxConfigResult() { Errors = ["Flux team config not found"] });

            // Act
            var result = await controller.CreateServiceAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        public async Task CreateServiceAsync_Returns_Created(bool isFrontend, bool isHelmOnly)
        {
            // Arrange
            var request = fixture.Build<ServiceConfigRequest>().Create();
            request.IsFrontend = isFrontend;
            request.IsHelmOnly = isHelmOnly;
            fluxTeamConfigServiceMock.AddServiceAsync(Arg.Any<string>(), Arg.Any<Core.Git.Entities.FluxService>())
                .Returns(new FluxConfigResult());

            // Act
            var result = await controller.CreateServiceAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            if (result != null)
            {
                var results = (CreatedResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task CreateServiceAsync_Returns_BadRequest_When_TeamFlux_Not_Exists()
        {
            // Arrange
            var request = fixture.Build<ServiceConfigRequest>().Create();
            fluxTeamConfigServiceMock.AddServiceAsync(Arg.Any<string>(), Arg.Any<Core.Git.Entities.FluxService>())
                .Returns(new FluxConfigResult() { IsConfigExists = false });

            // Act
            var result = await controller.CreateServiceAsync("teamName", request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo("Flux config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task AddServiceEnvironment_Returns_Created_When_Successfully_Add_Config()
        {
            // Arrange
            fluxTeamConfigServiceMock.AddServiceEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new FluxConfigResult() { IsConfigExists = true });

            // Act
            var result = await controller.AddServiceEnvironmentAsync("teamName", "serviceName", "snd");

            // Assert
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            if (result != null)
            {
                var results = (CreatedResult)result;
                Assert.That(results, Is.Not.Null);
            }
        }

        [Test]
        public async Task AddServiceEnvironment_Returns_BadRequest_When_Config_Not_Exists()
        {
            // Arrange
            fluxTeamConfigServiceMock.AddServiceEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new FluxConfigResult() { IsConfigExists = false, Errors = ["Flux config not found for the team:teamName"] });

            // Act
            var result = await controller.AddServiceEnvironmentAsync("teamName", "serviceName", "snd");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo("Flux config not found for the team:teamName"));
            }
        }

        [Test]
        public async Task AddServiceEnvironment_Returns_BadRequest_When_Failed_To_Save()
        {
            // Arrange
            fluxTeamConfigServiceMock.AddServiceEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new FluxConfigResult() { IsConfigExists = true, Errors = ["Failed to save the config for the team: teamName"] });

            // Act
            var result = await controller.AddServiceEnvironmentAsync("teamName", "serviceName", "snd");

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            if (result != null)
            {
                var badResults = (BadRequestObjectResult)result;
                Assert.That(badResults, Is.Not.Null);
                Assert.That(badResults.StatusCode, Is.EqualTo(400));
                Assert.That(badResults.Value, Is.EqualTo(new List<string>() { "Failed to save the config for the team: teamName" }));
            }
        }
    }
}
