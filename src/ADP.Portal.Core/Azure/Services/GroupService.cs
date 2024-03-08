using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System.Diagnostics.CodeAnalysis;

namespace ADP.Portal.Core.Azure.Services
{
    public class GroupService : IGroupService
    {
        private readonly ILogger<GroupService> logger;
        private readonly IAzureAadGroupService azureAADGroupService;

        public GroupService(IAzureAadGroupService azureAADGroupService, ILogger<GroupService> logger)
        {
            this.azureAADGroupService = azureAADGroupService;
            this.logger = logger;
        }

        public async Task<string?> GetUserIdAsync(string userPrincipalName)
        {
            try
            {
                var result = await azureAADGroupService.GetUserIdAsync(userPrincipalName);

                if (!string.IsNullOrEmpty(result))
                {
                    logger.LogInformation("User '{UserPrincipalName}' found.", userPrincipalName);
                }

                return result;
            }
            catch (ODataError odataException)
            {
                if (odataException.ResponseStatusCode == 404)
                {
                    logger.LogWarning("User '{UserPrincipalName}' does not exist.", userPrincipalName);
                    return null;
                }
                else
                {
                    logger.LogError(odataException, "Error occurred while getting user '{UserPrincipalName}'", userPrincipalName);
                    throw;
                }
            }
        }

        public async Task<bool> AddGroupMemberAsync(string groupId, string memberId)
        {
            var result = await azureAADGroupService.AddGroupMemberAsync(groupId, memberId);
            if (result)
            {
                logger.LogInformation("Added user({MemberId}) to group({GroupId})", memberId, groupId);
            }
            return result;
        }

        public async Task<bool> RemoveGroupMemberAsync(string groupId, string memberId)
        {
            var result = await azureAADGroupService.RemoveGroupMemberAsync(groupId, memberId);

            if (result)
            {
                logger.LogInformation("Removed user({MemberId}) from the group({GroupId})", memberId, groupId);
            }

            return result;
        }

        public async Task<string?> GetGroupIdAsync(string groupName)
        {
            var result = await azureAADGroupService.GetGroupIdAsync(groupName);

            if (!string.IsNullOrEmpty(result))
            {
                logger.LogInformation("Group '{GroupName}' found.", groupName);
            }

            return result;

        }
        public async Task<List<AadGroupMember>> GetUserTypeGroupMembersAsync(string groupId)
        {
            var result =   await azureAADGroupService.GetGroupMembersAsync<User>(groupId);
            if (result != null)
            {
                logger.LogInformation("Retrieved user type group members({Count}) from group({GroupId}))", result.Count, groupId);
                return result.Adapt<List<AadGroupMember>>();
            }

            return [];
        }

        public async Task<List<AadGroupMember>> GetGroupTypeGroupMembersAsync(string groupId)
        {
            var result = await azureAADGroupService.GetGroupMembersAsync<Group>(groupId);
            if (result != null)
            {
                logger.LogInformation("Retrieved group type group members({Count}) from group({GroupId}))", result.Count, groupId);
                return result.Adapt<List<AadGroupMember>>();
            }

            return [];
        }

        public async Task<List<AadGroup>> GetGroupMemberShipsAsync(string groupId)
        {
            var result = await azureAADGroupService.GetGroupMemberShipsAsync(groupId);

            if (result != null)
            {
                logger.LogInformation("Retrieved group memberships({Count}) from group({GroupId}))", result.Count, groupId);
                return result.Adapt<List<AadGroup>>();
            }

            return [];
        }

        public async Task<string?> AddGroupAsync(AadGroup aadGroup)
        {
            var result = await azureAADGroupService.AddGroupAsync(aadGroup.Adapt<Group>());
            if (result != null)
            {
                logger.LogInformation("Group '{DisplayName}' created", aadGroup.DisplayName);
            }
            return result?.Id;
        }
    }
}
