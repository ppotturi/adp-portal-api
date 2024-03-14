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
        public readonly IOptions<AzureAdConfig> azureAdConfig;
        private readonly IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig;

        public FluxConfigController(IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService, ILogger<FluxConfigController> logger,
            IOptions<TeamGitRepoConfig> teamGitRepoConfig, IOptions<AzureAdConfig> azureAdConfig, IOptions<FluxServicesGitRepoConfig> fluxServicesGitRepoConfig)
        {
            this.gitOpsFluxTeamConfigService = gitOpsFluxTeamConfigService;
            this.logger = logger;
            this.teamGitRepoConfig = teamGitRepoConfig;
            this.azureAdConfig = azureAdConfig;
            this.fluxServicesGitRepoConfig = fluxServicesGitRepoConfig;
        }

        [HttpPost("generate/{teamName}/{serviceName?}", Name = "Generate")]
        public async Task<ActionResult> GenerateAsync(string teamName, string? serviceName)
        {
            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();

            var fluxServicesRepo = fluxServicesGitRepoConfig.Value.Adapt<GitRepo>();
            var tenantName = azureAdConfig.Value.TenantName;

            logger.LogInformation("Sync Flux Services for the Team:{TeamName}", teamName);
            var result = await gitOpsFluxTeamConfigService.GenerateFluxTeamConfig(teamRepo, fluxServicesRepo, tenantName, teamName, serviceName);

            if (result.Errors.Count > 0)
            {
                if (!result.IsConfigExists)
                {
                    return BadRequest($"Flux generator config not for the team:{teamName}");
                }

                return BadRequest(result.Errors);
            }

            return Ok();
        }
    }
}
