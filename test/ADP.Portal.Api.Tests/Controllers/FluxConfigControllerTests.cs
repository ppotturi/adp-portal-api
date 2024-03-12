using System.Reflection;
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Controllers
{
    [TestFixture]
    public class FluxConfigControllerTests
    {
        private readonly FluxConfigController controller;
        private readonly ILogger<FluxConfigController> loggerMock;
        private readonly IOptions<TeamGitRepoConfig> teamGitRepoConfigMock;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfigMock;
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService;
        private readonly Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        }

        public FluxConfigControllerTests()
        {
            teamGitRepoConfigMock = Substitute.For<IOptions<TeamGitRepoConfig>>();
            fluxServicesGitRepoConfigMock = Substitute.For<IOptions<FluxServicesGitRepoConfig>>();
            loggerMock = Substitute.For<ILogger<FluxConfigController>>();
            gitOpsFluxTeamConfigService = Substitute.For<IGitOpsFluxTeamConfigService>();
            controller = new FluxConfigController(gitOpsFluxTeamConfigService, loggerMock, teamGitRepoConfigMock, fluxServicesGitRepoConfigMock);
            fixture = new Fixture();
        }
    }
}
