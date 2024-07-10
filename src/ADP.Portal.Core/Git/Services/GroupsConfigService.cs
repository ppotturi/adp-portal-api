using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Octokit;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Services;

public class GroupsConfigServiceOptions
{
    public ICollection<string> PostgresDBMembers { get; set; } = [];
}

public partial class GroupsConfigService : IGroupsConfigService
{
    private readonly IGitHubRepository gitHubRepository;
    private readonly IOptionsSnapshot<GroupsConfigServiceOptions> configOptions;
    private readonly GitRepo teamGitRepo;
    private readonly ILogger<GroupsConfigService> logger;
    private readonly IGroupService groupService;
    private readonly ISerializer serializer;
    private readonly IDeserializer deserializer;

    public GroupsConfigService(IGitHubRepository gitHubRepository, IOptionsSnapshot<GitRepo> gitRepoOptions, IOptionsSnapshot<GroupsConfigServiceOptions> configOptions,
        ILogger<GroupsConfigService> logger, IGroupService groupService, ISerializer serializer, IDeserializer deserializer)
    {
        this.gitHubRepository = gitHubRepository;
        this.configOptions = configOptions;
        this.teamGitRepo = gitRepoOptions.Get(Constants.GitRepo.TEAM_REPO_CONFIG);
        this.logger = logger;
        this.groupService = groupService;
        this.serializer = serializer;
        this.deserializer = deserializer;
    }

    public async Task<IEnumerable<Group>> GetGroupsConfigAsync(string tenantName, string teamName)
    {
        return await GetGroupsConfigAsync(tenantName, teamName, null);
    }

    public async Task<GroupConfigResult> CreateGroupsConfigAsync(string tenantName, string teamName, IEnumerable<string> adminGroupMembers, IEnumerable<string> techUserGroupMembers, IEnumerable<string> nonTechUserGroupMembers)
    {
        var result = new GroupConfigResult();

        var fileName = $"{tenantName}/{teamName}.yaml";
        var groups = BuildTeamGroups(tenantName, teamName, adminGroupMembers, techUserGroupMembers, nonTechUserGroupMembers, deserializer);

        logger.LogInformation("Create groups config for the team({TeamName})", teamName);
        var response = await gitHubRepository.CreateFileAsync(teamGitRepo, fileName, serializer.Serialize(groups));
        if (string.IsNullOrEmpty(response))
        {
            result.Errors.Add($"Failed to save the config for the team: {teamName}");
        }
        return result;
    }

    public async Task<GroupConfigResult> SetGroupMembersAsync(string tenantName, string teamName, IEnumerable<string> adminGroupMembers, IEnumerable<string> techUserGroupMembers, IEnumerable<string> nonTechUserGroupMembers)
    {
        var result = new GroupConfigResult();

        var existingGroups = await GetGroupsConfigAsync(tenantName, teamName, GroupType.UserGroup);
        if (existingGroups == null || !existingGroups.Any())
        {
            logger.LogDebug("User groups for team {TeamName} have not been created", teamName);
            result.Errors.Add($"User groups for team {teamName} have not been created");
            return result;
        }

        var fileName = $"{tenantName}/{teamName}.yaml";
        var groups = BuildTeamGroups(tenantName, teamName, adminGroupMembers, techUserGroupMembers, nonTechUserGroupMembers, deserializer);
        logger.LogInformation("Update groups config for the team {TeamName}", teamName);
        var response = await gitHubRepository.UpdateFileAsync(teamGitRepo, fileName, serializer.Serialize(groups));
        if (string.IsNullOrEmpty(response))
        {
            result.Errors.Add($"Failed to save the config for the team: {teamName}");
        }
        return result;
    }

    private GroupsRoot BuildTeamGroups(string tenantName, string teamName, IEnumerable<string> adminGroupMembers, IEnumerable<string> techUserGroupMembers, IEnumerable<string> nonTechUserGroupMembers, IDeserializer deserializer)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"{assemblyName.Name}.Git.template.UserGroupMemberShip.{tenantName.ToLower()}.yml")!;
        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        var data = streamReader.ReadToEnd();
        var userGroupMemberships = deserializer.Deserialize<UserGroupMembership>(data);
        var allUsers = new List<string>();
        allUsers.AddRange(adminGroupMembers);
        allUsers.AddRange(techUserGroupMembers);
        allUsers.AddRange(nonTechUserGroupMembers);
        var environments = new List<string>();
        switch (tenantName)
        {
            case "defradev":
                environments = ["snd1", "snd2", "snd3"];
                break;

            case "defra":
                environments = ["snd4", "dev1", "tst1", "pre1", "prd1"];
                break;
        }

        var root = new GroupsRoot
        {
            Groups = [
                new Group {
                    DisplayName = $"AAG-Users-ADP-{teamName.ToUpper()}_TechUser",
                    Type = GroupType.UserGroup,
                    GroupMemberships = BuildGroupMembership(teamName, userGroupMemberships.TechUser),
                    Members = techUserGroupMembers.ToList()
                },
                new Group {
                    DisplayName = $"AAG-Users-ADP-{teamName.ToUpper()}_NonTechUser",
                    Type = GroupType.UserGroup,
                    GroupMemberships = BuildGroupMembership(teamName, userGroupMemberships.NontechUser),
                    Members = nonTechUserGroupMembers.ToList()
                },
                new Group {
                    DisplayName = $"AAG-Users-ADP-{teamName.ToUpper()}_Admin",
                    Type = GroupType.UserGroup,
                    GroupMemberships = BuildGroupMembership(teamName, userGroupMemberships.Admin),
                    Members = adminGroupMembers.ToList()
                },
                new Group {
                    DisplayName = $"AAG-APP-Defra-Azure-OpenVPN-ADP-Users",
                    Type = GroupType.OpenVpnGroup,
                    GroupMemberships = [],
                    Members = allUsers.Select(x => x.ToLower()).Distinct().ToList()
                }
            ]
        };

        environments.ForEach(item =>
        {
            root.Groups.Add(new Group
            {
                DisplayName = $"AAG-Azure-ADP-{teamName.ToUpper()}-{item.ToUpper()}-PostgresDB_Reader",
                Description = "AD group to grant reader access to postgres DB",
                Type = GroupType.AccessGroup,
                Members = configOptions.Value.PostgresDBMembers.ToList()
            });
            root.Groups.Add(new Group
            {
                DisplayName = $"AAG-Azure-ADP-{teamName.ToUpper()}-{item.ToUpper()}-PostgresDB_Writer",
                Description = "AD group to grant writer access to postgres DB",
                Type = GroupType.AccessGroup,
                Members = configOptions.Value.PostgresDBMembers.ToList()
            });
        });

        root.Groups.Add(new Group
        {
            DisplayName = $"AAG-Azure-ADP-{teamName.ToUpper()}-Resources-Contributor",
            Description = "AD group to grant contributor access to team resources. For e.g. Contributor to Team resource group, DataOwner to team queues and topic.",
            Type = GroupType.AccessGroup
        });

        root.Groups.Add(new Group
        {
            DisplayName = $"AAG-Azure-ADP-{teamName.ToUpper()}-Resources-Reader",
            Description = "AD group to grant reader access to team resources.",
            Type = GroupType.AccessGroup
        });

        return root;
    }

    private static List<string> BuildGroupMembership(string teamName, List<String> userGroupMemberships)
    {
        List<string> groupMemberships = new List<string>();

        groupMemberships.AddRange(userGroupMemberships.Select(groupName => groupName.Replace("{teamName}", teamName.ToUpper())).ToList<string>());
        return groupMemberships;
    }

    public async Task<GroupSyncResult> SyncGroupsAsync(string tenantName, string teamName, string ownerId, GroupType? groupType)
    {
        var result = new GroupSyncResult();

        var groups = await GetGroupsConfigAsync(tenantName, teamName, groupType);

        if (!groups.Any())
        {
            result.IsConfigExists = false;
            result.Errors.Add($"Groups config not found for the team:{teamName} in the tenant:{tenantName}");
            return result;
        }

        logger.LogInformation("Syncing groups for the team({TeamName})", teamName);
        //AccessGroups
        var accessGroupstasks = groups.Where(x => x.Type == GroupType.AccessGroup).Select(group => ProcessGroupAsync(group, ownerId, result));
        await Task.WhenAll(accessGroupstasks);

        //UserGroups and Others
        var UserGroupTasks = groups.Where(x => x.Type == GroupType.UserGroup).Select(group => ProcessGroupAsync(group, ownerId, result));
        await Task.WhenAll(UserGroupTasks);

        //UserGroups and Others
        var OpenVpnGroupTasks = groups.Where(x => x.Type == GroupType.OpenVpnGroup).Select(group => ProcessGroupAsync(group, ownerId, result));
        await Task.WhenAll(OpenVpnGroupTasks);

        return result;
    }

    private async Task ProcessGroupAsync(Group group, string ownerId, GroupSyncResult result)
    {
        logger.LogInformation("Getting groupId for the group({DisplayName})", group.DisplayName);
        var groupId = await groupService.GetGroupIdAsync(group.DisplayName);

        if (string.IsNullOrEmpty(groupId) && CanCreateGroup(group.Type))
        {
            logger.LogInformation("Creating a new Group({DisplayName})", group.DisplayName);
            groupId = await CreateNewGroupAsync(group, ownerId);
        }

        if (string.IsNullOrEmpty(groupId))
        {
            result.Errors.Add($"Group '{group.DisplayName}' does not exists.");
        }
        else
        {
            logger.LogInformation("Syncing group members for the group({DisplayName})", group.DisplayName);
            await SyncGroupMembersAsync(group, groupId, result);
        }
    }

    private async Task<IEnumerable<Group>> GetGroupsConfigAsync(string tenantName, string teamName, GroupType? groupType)
    {
        try
        {
            var fileName = $"{tenantName}/{teamName}.yaml";

            logger.LogInformation("Getting groups config for the team({TeamName})", teamName);
            var result = await gitHubRepository.GetFileContentAsync<GroupsRoot>(teamGitRepo, fileName);

            return result?.Groups.Where(g => groupType == null || g.Type == groupType) ?? [];
        }
        catch (NotFoundException)
        {
            return [];
        }
    }

    private async Task<string?> CreateNewGroupAsync(Group group, string ownerId)
    {
        logger.LogInformation("Creating a new Group({DisplayName})", group.DisplayName);

        var aadGroup = group.Adapt<AadGroup>();
        aadGroup.OwnerId = ownerId;

        return await groupService.AddGroupAsync(aadGroup);
    }

    private async Task SyncGroupMembersAsync(Group group, string groupId, GroupSyncResult result)
    {
        logger.LogInformation("Syncing group members for the group({DisplayName})", group.DisplayName);

        if (CanSyncUserTypeMembers(group.Type))
        {
            await SyncUserTypeMembersAsync(result, group, groupId, false);
        }

        if (CanSyncMemberships(group.Type))
        {
            logger.LogInformation("Syncing group memberships for the group({DisplayName})", group.DisplayName);
            await SyncMembershipsAsync(result, group, groupId, false);
        }

        if (CanSyncGroupTypeMembers(group.Type))
        {
            await SyncGroupTypeMembersAsync(result, group, groupId, false);
        }
    }

    private async Task SyncUserTypeMembersAsync(GroupSyncResult result, Group group, string? groupId, bool isNewGroup)
    {
        if (groupId == null)
        {
            return;
        }

        var existingMembers = isNewGroup ? [] : await groupService.GetUserTypeGroupMembersAsync(groupId);

        foreach (var member in existingMembers)
        {
            if (!group.Members.Contains(member.UserPrincipalName, StringComparer.OrdinalIgnoreCase) && group.Type != GroupType.OpenVpnGroup)
            {
                await groupService.RemoveGroupMemberAsync(groupId, member.Id);
            }
        }

        var existingMemberNames = existingMembers.Select(i => i.UserPrincipalName).ToList();

        foreach (var member in group.Members)
        {
            if (!existingMemberNames.Contains(member, StringComparer.OrdinalIgnoreCase))
            {
                var memberId = await groupService.GetUserIdAsync(member);

                if (memberId == null)
                {
                    result.Errors.Add($"User '{member}' not found for the group:{group.DisplayName}.");
                }
                else
                {
                    await groupService.AddGroupMemberAsync(groupId, memberId);
                }
            }
        }
    }

    private async Task SyncGroupTypeMembersAsync(GroupSyncResult result, Group group, string groupId, bool isNewGroup)
    {
        var existingMembers = isNewGroup ? [] : await groupService.GetGroupTypeGroupMembersAsync(groupId);

        foreach (var member in existingMembers)
        {
            if (!group.Members.Contains(member.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                await groupService.RemoveGroupMemberAsync(groupId, member.Id);
            }
        }

        var existingMemberNames = existingMembers.Select(i => i.DisplayName).ToList();

        foreach (var member in group.Members)
        {
            if (!existingMemberNames.Contains(member, StringComparer.OrdinalIgnoreCase))
            {
                var memberId = await groupService.GetGroupIdAsync(member);

                if (memberId == null)
                {
                    result.Errors.Add($"Group '{member}' not found.");
                }
                else
                {
                    await groupService.AddGroupMemberAsync(groupId, memberId);
                }
            }
        }
    }

    private async Task SyncMembershipsAsync(GroupSyncResult result, Group group, string groupId, bool IsNewGroup)
    {
        var existingMemberShips = IsNewGroup ? [] : await groupService.GetGroupMemberShipsAsync(groupId);

        foreach (var memberShip in existingMemberShips)
        {
            if (memberShip.Id != null && !group.GroupMemberships.Contains(memberShip.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                await groupService.RemoveGroupMemberAsync(memberShip.Id, groupId);
            }
        }

        var existingMembershipNames = existingMemberShips.Select(i => i.DisplayName).ToList();

        foreach (var groupMembership in group.GroupMemberships)
        {
            if (!existingMembershipNames.Contains(groupMembership, StringComparer.OrdinalIgnoreCase))
            {
                var groupMembershipId = await groupService.GetGroupIdAsync(groupMembership);
                if (groupMembershipId == null)
                {
                    result.Errors.Add($"Membership Group '{groupMembership}' not found for the group:{group.DisplayName}.");
                }
                else
                {
                    await groupService.AddGroupMemberAsync(groupMembershipId, groupId);
                }
            }
        }
    }

    private static bool CanCreateGroup(GroupType groupType)
    {
        return (groupType == GroupType.UserGroup || groupType == GroupType.AccessGroup);
    }

    private static bool CanSyncUserTypeMembers(GroupType groupType)
    {
        return groupType == GroupType.OpenVpnGroup || groupType == GroupType.UserGroup;
    }

    private static bool CanSyncGroupTypeMembers(GroupType groupType)
    {
        return groupType == GroupType.AccessGroup;
    }

    private static bool CanSyncMemberships(GroupType groupType)
    {
        return groupType == GroupType.UserGroup;
    }
}