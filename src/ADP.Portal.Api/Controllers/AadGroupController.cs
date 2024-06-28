using ADP.Portal.Api.Config;
using ADP.Portal.Api.Models.Group;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api.Controllers;

[Route("api/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class AadGroupController : ControllerBase
{
    private readonly IGroupsConfigService groupsConfigService;
    private readonly ILogger<AadGroupController> logger;
    public readonly IOptions<AzureAdConfig> azureAdConfig;


    public AadGroupController(IGroupsConfigService groupsConfigService, ILogger<AadGroupController> logger,
        IOptions<AzureAdConfig> azureAdConfig)
    {
        this.groupsConfigService = groupsConfigService;
        this.logger = logger;
        this.azureAdConfig = azureAdConfig;
    }

    /// <summary>
    /// Reads the Groups Configuration for the Team from the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <returns></returns>
    [HttpGet("{teamName}/groups-config", Name = "GetGroupsConfigForTeam")]
    [Authorize(AuthenticationSchemes = "")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetGroupsConfigAsync(string teamName)
    {
        var tenantName = azureAdConfig.Value.TenantName;

        logger.LogInformation("Reading Groups Config for the Team:'{TeamName}'", teamName);
        var groups = await groupsConfigService.GetGroupsConfigAsync(tenantName, teamName);

        return Ok(groups);
    }

    /// <summary>
    /// Create a new Groups configuration for the specified Team in the GitOps repository.
    /// </summary>
    /// <param name="teamName">Required: Name of the Team, like ffc-demo</param>
    /// <param name="createGroupsConfigRequest">Required: Collection of the users to set up as members in the Admin Group</param>
    /// <returns></returns>
    [HttpPost("{teamName}/groups-config", Name = "CreateGroupsConfigForTeam")]
    [Authorize(AuthenticationSchemes = "backstage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateGroupsConfigAsync(string teamName, [FromBody] CreateGroupsConfigRequest createGroupsConfigRequest)
    {

        var tenantName = azureAdConfig.Value.TenantName;
        var ownerId = azureAdConfig.Value.SpObjectId;
        teamName = teamName.ToLower();

        logger.LogInformation("Creating Groups Config for the Team:'{TeamName}'", teamName);
        var result = await groupsConfigService.CreateGroupsConfigAsync(tenantName, teamName,
                                                                       createGroupsConfigRequest.AdminMembers,
                                                                       createGroupsConfigRequest.TechUserMembers,
                                                                       createGroupsConfigRequest.NonTechUserMembers);
        if (result.Errors.Count != 0)
        {
            logger.LogError("Error while creating groups config for the Team:'{TeamName}' with errors: {Errors}", teamName, result.Errors);
            return BadRequest(result.Errors);
        }

        logger.LogInformation("Sync Groups for the Team:'{TeamName}'", teamName);
        var syncResult = await groupsConfigService.SyncGroupsAsync(tenantName, teamName, ownerId, null);
        if (syncResult.Errors.Count != 0)
        {
            logger.LogError("Error while syncing groups for the Team:'{TeamName}' with errors: {Errors}", teamName, syncResult.Errors);
            return BadRequest(syncResult.Errors);
        }

        return NoContent();
    }

    [HttpPatch("{teamName}/members", Name = "SetMembersForTeam")]
    [Authorize(AuthenticationSchemes = "backstage")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SetGroupMembersAsync(string teamName, [FromBody] SetGroupMembersRequest setGroupMembersRequest)
    {
        var tenantName = azureAdConfig.Value.TenantName;
        var ownerId = azureAdConfig.Value.SpObjectId;
        teamName = teamName.ToLower();

        logger.LogInformation("Setting group members for team: {TeamName}", teamName);
        var result = await groupsConfigService.SetGroupMembersAsync(tenantName, teamName,
                                                                    setGroupMembersRequest.AdminMembers,
                                                                    setGroupMembersRequest.TechUserMembers,
                                                                    setGroupMembersRequest.NonTechUserMembers);
        if (result.Errors.Count != 0)
        {
            logger.LogError("Error while creating groups config for the Team:'{TeamName}'", teamName);
            return BadRequest(result.Errors);
        }

        logger.LogInformation("Sync Groups for the Team:'{TeamName}'", teamName);
        var syncResult = await groupsConfigService.SyncGroupsAsync(tenantName, teamName, ownerId, GroupType.UserGroup);
        if (syncResult.Errors.Count != 0)
        {
            logger.LogError("Error while syncing groups for the Team:'{TeamName}' with errors: {Errors}", teamName, syncResult.Errors);
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
    [Authorize(AuthenticationSchemes = "")]
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

        var tenantName = azureAdConfig.Value.TenantName;
        var ownerId = azureAdConfig.Value.SpObjectId;

        logger.LogInformation("Sync Groups for the Team:'{TeamName}' and Group Type:'{GroupType}'", teamName, groupType);
        var result = await groupsConfigService.SyncGroupsAsync(tenantName, teamName, ownerId, groupType != null ? (GroupType)syncGroupTypeEnum : null);

        if (result.Errors.Count > 0)
        {
            if (!result.IsConfigExists)
            {
                logger.LogError("Config not found for the Team:'{TeamName}' with errors: {Errors}", teamName, result.Errors);
                return BadRequest(result.Errors);
            }

            logger.LogError("Error while syncing groups for the Team:'{TeamName}' with errors: {Errors}", teamName, result.Errors);
            return BadRequest(result.Errors);
        }

        return NoContent();
    }
}