using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AadGroupController : ControllerBase
    {
        private readonly IGitOpsConfigService gitOpsConfigService;
        private readonly ILogger<AadGroupController> logger;
        public readonly IOptions<AzureAdConfig> azureAdConfig;
        private readonly IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig;

        public AadGroupController(IGitOpsConfigService gitOpsConfigService, ILogger<AadGroupController> logger,
            IOptions<AzureAdConfig> azureAdConfig, IOptions<AdpTeamGitRepoConfig> adpTeamGitRepoConfig)
        {
            this.gitOpsConfigService = gitOpsConfigService;
            this.logger = logger;
            this.azureAdConfig = azureAdConfig;
            this.adpTeamGitRepoConfig = adpTeamGitRepoConfig;
        }

        [HttpPut("sync/{teamName}/{syncConfigType}")]
        public async Task<ActionResult> SyncGroupsAsync(string teamName, string syncConfigType)
        {
            if (!Enum.TryParse<SyncConfigType>(syncConfigType, true, out var syncConfigTypeEnum))
            {
                logger.LogWarning("Invalid syncConfigType:{SyncConfigType}", syncConfigType);
                return BadRequest("Invalid syncConfigType.");
            }

            var configType = (ConfigType)syncConfigTypeEnum;
            var teamRepo = adpTeamGitRepoConfig.Value.Adapt<GitRepo>();

            logger.LogInformation("Check if config exists for team:{TeamName} and configType:{ConfigType}", teamName, configType);
            var isConfigExists = await gitOpsConfigService.IsConfigExistsAsync(teamName, configType, teamRepo);
            if (!isConfigExists)
            {
                logger.LogWarning("Config not found for the Team:{TeamName} and configType:{ConfigType}", teamName, configType);
                return BadRequest($"Team '{teamName}' config not found.");
            }

            var ownerId = azureAdConfig.Value.SpObjectId;
            logger.LogInformation("Sync Groups for the Team:{TeamName} and configType:{ConfigType}", teamName, configType);
            var result = await gitOpsConfigService.SyncGroupsAsync(teamName, ownerId, configType, teamRepo);

            if (result.Error.Count > 0)
            {
                return Ok(result.Error);
            }

            return NoContent();
        }
    }
}
