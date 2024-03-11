using System.Text.RegularExpressions;
using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Services;
using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Octokit;

namespace ADP.Portal.Core.Git.Services
{
    public partial class GitOpsGroupsConfigService : IGitOpsGroupsConfigService
    {
        private readonly IGitOpsConfigRepository gitOpsConfigRepository;
        private readonly ILogger<GitOpsGroupsConfigService> logger;
        private readonly IGroupService groupService;

        [GeneratedRegex("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])")]
        private static partial Regex KebabCaseRegex();

        public GitOpsGroupsConfigService(IGitOpsConfigRepository gitOpsConfigRepository, ILogger<GitOpsGroupsConfigService> logger, IGroupService groupService)
        {
            this.gitOpsConfigRepository = gitOpsConfigRepository;
            this.logger = logger;
            this.groupService = groupService;
        }

        public async Task<GroupSyncResult> SyncGroupsAsync(string tenantName, string teamName, string ownerId, GroupType? groupType, GitRepo gitRepo)
        {
            var result = new GroupSyncResult();

            var groups = await GetGroupsConfigAsync(tenantName, teamName, groupType, gitRepo);

            if (groups == null)
            {
                result.IsConfigExists = false;
                result.Errors.Add($"Groups config not found for the team({teamName}) in the tenant({tenantName}");
                return result;
            }

            logger.LogInformation("Syncing groups for the team({TeamName})", teamName);
            var tasks = groups.Select(group => ProcessGroupAsync(group, ownerId, result));
            await Task.WhenAll(tasks);

            return result;
        }

        private async Task ProcessGroupAsync(Entities.Group group, string ownerId, GroupSyncResult result)
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

        private async Task<List<Entities.Group>?> GetGroupsConfigAsync(string tenantName, string teamName, GroupType? groupType, GitRepo gitRepo)
        {
            try
            {
                var fileName = $"{tenantName}/{teamName}.yaml";

                logger.LogInformation("Getting groups config for the team({TeamName})", teamName);
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

        private async Task SyncGroupMembersAsync(Entities.Group group, string groupId, GroupSyncResult result)
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
                        result.Errors.Add($"User '{member}' not found for the group:{group.DisplayName}.");
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
}