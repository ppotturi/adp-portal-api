using System.Text.RegularExpressions;
using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Octokit;

namespace ADP.Portal.Core.Git.Services
{
    public partial class GitOpsConfigService : IGitOpsConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsConfigService> logger;
        private readonly IGroupService groupService;

        [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])")]
        private static partial Regex KebabCaseRegex();

        public GitOpsConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsConfigService> logger, IGroupService groupService)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
            this.groupService = groupService;
        }

        public async Task<bool> IsConfigExistsAsync(string name, ConfigType configType, string tenantName, GitRepo gitRepo)
        {
            var fileName = GetFileName(name, configType);
            try
            {
                var result = await gitOpsConfigRepository.GetConfigAsync<string>(fileName, gitRepo);
                return !string.IsNullOrEmpty(result);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        public async Task<GroupSyncResult> SyncGroupsAsync(string tenantName, string teamName, string ownerId, GroupType? groupType, GitRepo gitRepo)
        {
            var result = new GroupSyncResult();

            var groups = await GetGroupsConfigAsync(tenantName, teamName, groupType, gitRepo);

            if (groups == null)
            {
                result.Errors.Add("Groups not found in the config.");
                return result;
            }

            //logger.LogInformation("Getting config({ConfigType}) for the Team({TeamName})'", configType.ToString(), teamName);

            foreach (var group in groups)
            {
                await ProcessGroupAsync(group, ownerId, result);
            }

            return result;
        }

        private async Task ProcessGroupAsync(Entities.Group group, string ownerId, GroupSyncResult result)
        {
            logger.LogInformation("Getting groupId for the group({DisplayName})", group.DisplayName);
            var groupId = await groupService.GetGroupIdAsync(group.DisplayName);

            if (string.IsNullOrEmpty(groupId) && CanCreateGroup(group))
            {
                groupId = await CreateNewGroupAsync(group, ownerId);
            }

            if (string.IsNullOrEmpty(groupId))
            {
                result.Errors.Add($"Group '{group.DisplayName}' does not exists.");
            }
            else
            {
                await SyncGroupMembersAsync(group, groupId, result);
            }
        }

        private async Task<List<Entities.Group>?> GetGroupsConfigAsync(string tenantName, string teamName, GroupType? groupType, GitRepo gitRepo)
        {
            try
            {
                var fileName = $"{tenantName}/{teamName}.yaml";
                var result = await gitOpsConfigRepository.GetConfigAsync<GroupsRoot>(fileName, gitRepo);

                return result?.Groups.Where(g => groupType == null || g.Type == groupType).ToList() ?? [];

            }
            catch (NotFoundException)
            {
                return [];
            }
        }

        private async Task<string?> CreateNewGroupAsync(Entities.Group group, string ownerId)
        {
            logger.LogInformation("Creating a new Group({DisplayName})", group.DisplayName);
            var aadGroup = group.Adapt<AadGroup>();
            aadGroup.OwnerId = ownerId;

            return await groupService.AddGroupAsync(aadGroup);
        }

        public async Task GenerateFluxTeamConfig(string teamName, GitRepo gitRepo)
        {
            var fileName = GetFileName(teamName, ConfigType.FluxServices);
            var teamConfig = await gitOpsConfigRepository.GetConfigAsync<FluxTeamConfig>(fileName, gitRepo);

            await Task.CompletedTask;
        }

        private async Task SyncGroupMembersAsync(Entities.Group group, string groupId, GroupSyncResult result)
        {
            logger.LogInformation("Syncing group members for the group({DisplayName})", group.DisplayName);

            if (CanSyncUserTypeMembers(group))
            {
                await SyncUserTypeMembersAsync(result, group, groupId, false);
            }

            if (CanSyncMemberships(group))
            {
                logger.LogInformation("Syncing group memberships for the group({DisplayName})", group.DisplayName);
                await SyncMembershipsAsync(result, group, groupId, false);
            }

            if (CanSyncGroupTypeMembers(group))
            {
                await SyncGroupTypeMembersAsync(result, group, groupId, false);
            }
        }

        private async Task SyncUserTypeMembersAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool isNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

            var existingMembers = isNewGroup ? [] : await groupService.GetUserTypeGroupMembersAsync(groupId);

            foreach (var member in existingMembers)
            {
                if (!group.Members.Contains(member.UserPrincipalName, StringComparer.OrdinalIgnoreCase))
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
                        result.Errors.Add($"User '{member}' not found.");
                    }
                    else
                    {
                        await groupService.AddGroupMemberAsync(groupId, memberId);
                    }
                }
            }
        }

        private async Task SyncGroupTypeMembersAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool isNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

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

        private async Task SyncMembershipsAsync(GroupSyncResult result, Entities.Group group, string? groupId, bool IsNewGroup)
        {
            if (groupId == null)
            {
                return;
            }

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
                        result.Errors.Add($"Membership Group '{groupMembership}' not found.");
                    }
                    else
                    {
                        await groupService.AddGroupMemberAsync(groupMembershipId, groupId);
                    }
                }
            }
        }

        private static string GetFileName(string teamName, ConfigType configType)
        {
            return $"{teamName}/{ToKebabCase(configType.ToString())}.yaml";
        }
        private static string ToKebabCase(string name)
        {
            return KebabCaseRegex().Replace(name, "-$1").ToLower();
        }

        private static bool CanCreateGroup(Entities.Group group)
        {
            return (group.Type == GroupType.UserGroup || group.Type == GroupType.AccessGroup);
        }

        private static bool CanSyncUserTypeMembers(Entities.Group group)
        {
            return group.Type == GroupType.OpenVpnGroup || group.Type == GroupType.UserGroup;
        }

        private static bool CanSyncGroupTypeMembers(Entities.Group group)
        {
            return group.Type == GroupType.AccessGroup;
        }

        private static bool CanSyncMemberships(Entities.Group group)
        {
            return group.Type == GroupType.UserGroup;
        }


    }
}