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
    public class ScaffolderController(IGitOpsConfigService gitOpsConfigService, ILogger<ScaffolderController> logger, IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig) : Controller
    {
        private readonly IGitOpsConfigService gitOpsConfigService = gitOpsConfigService;
        private readonly ILogger<ScaffolderController> logger = logger;
        private readonly IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig = adpTeamGitRepoConfig;

        [HttpPost("onboardfluxservices/{teamName}/{serviceName?}", Name = "OnBoardFluxServices")]
        public async Task<ActionResult> OnBoardFluxServicesAsync(string teamName, string? serviceName)
        {
            var teamRepo = adpTeamGitRepoConfig.Value.Adapt<GitRepo>();

            logger.LogInformation("Check if Flux Services config exists for team:{TeamName}", teamName);
            var isConfigExists = await gitOpsConfigService.IsConfigExistsAsync(teamName, ConfigType.FluxServices, teamRepo);
            if (!isConfigExists)
            {
                logger.LogWarning("Config not found for the Team:{TeamName} and configType:{ConfigType}", teamName, ConfigType.FluxServices);
                return BadRequest($"Team '{teamName}' config not found.");
            }

            logger.LogInformation("Sync Flux Services for the Team:{TeamName}", teamName);
            await gitOpsConfigService.SyncFluxServices(teamName, teamRepo);

            return Ok();
        }
    }
}
