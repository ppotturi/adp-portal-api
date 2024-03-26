using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Group;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    public class AadGroupController : ControllerBase
    {
        private readonly IGitOpsGroupsConfigService gitOpsConfigService;
        private readonly ILogger<AadGroupController> logger;
        public readonly IOptions<AzureAdConfig> azureAdConfig;
        private readonly IOptions<TeamGitRepoConfig> teamGitRepoConfig;

        public AadGroupController(IGitOpsGroupsConfigService gitOpsConfigService, ILogger<AadGroupController> logger,
            IOptions<AzureAdConfig> azureAdConfig, IOptions<TeamGitRepoConfig> teamGitRepoConfig)
        {
            this.gitOpsConfigService = gitOpsConfigService;
            this.logger = logger;
            this.azureAdConfig = azureAdConfig;
            this.teamGitRepoConfig = teamGitRepoConfig;
        }

        [HttpPut("sync/{teamName}/{groupType?}")]
        public async Task<ActionResult> SyncGroupsAsync(string teamName, string? groupType = null)
        {
            var isValidType = Enum.TryParse<SyncGroupType>(groupType, true, out var syncGroupTypeEnum);
            if (groupType != null && !isValidType)
            {
                logger.LogWarning("Invalid Group Type:{GroupType}", groupType);
                return BadRequest("Invalid Group Type.");
            }

            var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();
            var tenantName = azureAdConfig.Value.TenantName;
            var ownerId = azureAdConfig.Value.SpObjectId;

            logger.LogInformation("Sync Groups for the Team:'{TeamName}' and Group Type:'{GroupType}'", teamName, groupType);
            var result = await gitOpsConfigService.SyncGroupsAsync(tenantName, teamName, ownerId, groupType != null ? (GroupType?)syncGroupTypeEnum : null, teamRepo);

            if (result.Errors.Count > 0)
            {
                if (!result.IsConfigExists)
                {
                    logger.LogWarning("Config not found for the Team:'{TeamName}'", teamName);
                    return BadRequest(result.Errors);
                }

                logger.LogError("Error while syncing groups for the Team:'{TeamName}'", teamName);
                return Ok(result.Errors);
            }

            return NoContent();
        }
    }
}
