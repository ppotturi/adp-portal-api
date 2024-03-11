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
    public class FluxConfigController(IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService, ILogger<FluxConfigController> logger,
        IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig) : Controller
    {
        private readonly IGitOpsFluxTeamConfigService gitOpsFluxTeamConfigService = gitOpsFluxTeamConfigService;
        private readonly ILogger<FluxConfigController> logger = logger;
        private readonly IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig = adpTeamGitRepoConfig;

        [HttpPost("generateteamconfig/{teamName}/{serviceName?}", Name = "GenerateTeamConfig")]
        public async Task<ActionResult> GenerateTeamConfigAsync(string teamName, string? serviceName)
        {
            var teamRepo = adpTeamGitRepoConfig.Value.Adapt<GitRepo>();

            logger.LogInformation("Sync Flux Services for the Team:{TeamName}", teamName);
            await gitOpsFluxTeamConfigService.GenerateFluxTeamConfig(teamRepo, teamName, serviceName);

            return Ok();
        }
    }
}
