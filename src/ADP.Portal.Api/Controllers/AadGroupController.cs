using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Group;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers;

[Route("api/[controller]")]
[ApiVersion("1.0")]
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

    /// <summary>
    /// Reads the Groups Configuration for the Team from the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <returns></returns>
    [HttpGet("{teamName}/groups-config", Name = "GetGroupsConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetGroupsConfigAsync(string teamName)
    {
        var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();
        var tenantName = azureAdConfig.Value.TenantName;

        logger.LogInformation("Reading Groups Config for the Team:'{TeamName}'", teamName);
        var groups = await gitOpsConfigService.GetGroupsConfigAsync(tenantName, teamName, teamRepo);

        return Ok(groups);
    }

    /// <summary>
    /// Create a new Groups configuration for the specified Team in the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="createGroupsConfigRequest">Required: Collection of the users to set up as members in the Admin Group</param>
    /// <returns></returns>
    [HttpPost("{teamName}/groups-config", Name = "CreateGroupsConfigForTeam")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateGroupsConfigAsync(string teamName, [FromBody] CreateGroupsConfigRequest createGroupsConfigRequest)
    {
        var teamRepo = teamGitRepoConfig.Value.Adapt<GitRepo>();
        var tenantName = azureAdConfig.Value.TenantName;
        var ownerId = azureAdConfig.Value.SpObjectId;

        logger.LogInformation("Creating Groups Config for the Team:'{TeamName}'", teamName);
        var result = await gitOpsConfigService.CreateGroupsConfigAsync(tenantName, teamName, teamRepo, createGroupsConfigRequest.Members);
        if (result.Errors.Count != 0)
        {
            logger.LogError("Error while creating groups config for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        logger.LogInformation("Sync Groups for the Team:'{TeamName}'", teamName);
        var syncResult = await gitOpsConfigService.SyncGroupsAsync(tenantName, teamName, ownerId, null, teamRepo);
        if (syncResult.Errors.Count != 0)
        {
            logger.LogError("Error while syncing groups for the Team:'{TeamName}'", teamName);
            return BadRequest(syncResult.Errors);
        }

        return Created();
    }

    /// <summary>
    /// Synchronise the Groups defined in the Team specific configuration file in GitOps repository with the Azure Active Directory
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="groupType">Optional: Type of groups to sync i.e. UserGroup/AccessGroup/OpenVpnGroup</param>
    /// <returns></returns>
    [HttpPut("{teamName}/sync", Name = "SyncGroupsForTeam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SyncGroupsAsync(string teamName, [FromQuery] string? groupType = null)
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
        var result = await gitOpsConfigService.SyncGroupsAsync(tenantName, teamName, ownerId, groupType != null ? (GroupType)syncGroupTypeEnum : null, teamRepo);

        if (result.Errors.Count > 0)
        {
            if (!result.IsConfigExists)
            {
                logger.LogError("Config not found for the Team:'{TeamName}'", teamName);
                return BadRequest(result.Errors);
            }

            logger.LogError("Error while syncing groups for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        return NoContent();
    }
}