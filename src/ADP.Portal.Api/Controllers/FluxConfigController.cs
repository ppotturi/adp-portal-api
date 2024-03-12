using ADP.Portal.Api.Config;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FluxConfigController : Controller
    {
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService;
        private readonly ILogger<FluxConfigController> logger;
        private readonly IOptions<TeamGitRepoConfig> teamGitRepoConfig;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig;

        public FluxConfigController(IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService, ILogger<FluxConfigController> logger,
        IOptions<TeamGitRepoConfig> teamGitRepoConfig, IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig)
        {
            this.gitOpsFluxTeamConfigService = gitOpsFluxTeamConfigService;
            this.logger = logger;
            this.teamGitRepoConfig = teamGitRepoConfig;
            this.fluxServicesGitRepoConfig = fluxServicesGitRepoConfig;
        }

        [HttpPost("generateteamconfig/{teamName}/{serviceName?}", Name = "GenerateTeamConfig")]
        public async Task<ActionResult> GenerateTeamConfigAsync(string teamName, string? serviceName)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var fluxServicesRepo = fluxServicesGitRepoConfig.Value.Adapt<GitRepo>();

            logger.LogInformation("Sync Flux Services for the Team:{TeamName}", teamName);
            await gitOpsFluxTeamConfigService.GenerateFluxTeamConfig(teamRepo, fluxServicesRepo, teamName, serviceName);

            return Ok();
        }
    }
}
