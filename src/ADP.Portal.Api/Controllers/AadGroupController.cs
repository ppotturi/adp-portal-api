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

        [HttpPut("sync/{teamName}/{groupType?}")]
        public async Task<ActionResult> SyncGroupsAsync(string teamName, string? groupType = null)
        {
            var isValidtype = Enum.TryParse<SyncGroupType>(groupType, true, out var syncGroupTypeEnum); 
            if(groupType!= null && !isValidtype)
            {
                logger.LogWarning("Invalid Group Type:{GroupType}", groupType);
                return BadRequest("Invalid Group Type.");
            }

            var teamRepo = adpTeamGitRepoConfig.Value.Adapt<GitRepo>();
            var tenantName = azureAdConfig.Value.TenantName;

            var ownerId = azureAdConfig.Value.SpObjectId;
            
            //logger.LogInformation("Sync Groups for the Team:{TeamName} and configType:{ConfigType}", teamName, configType);
            var result = await gitOpsConfigService.SyncGroupsAsync(tenantName,teamName, ownerId, groupType==null? null: (GroupType)syncGroupTypeEnum, teamRepo);

            if (result.Errors.Count > 0)
            {
                return BadRequest(result.Errors);
            }

            return NoContent();
        }
    }
}
