using ADP.Portal.Core.Azure.Entities;
using ADP.Portal.Core.Azure.Infrastructure;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System.Diagnostics.CodeAnalysis;

namespace ADP.Portal.Core.Azure.Services
{
    public class UserGroupService : IUserGroupService
    {
        private readonly ILogger<UserGroupService> logger;
        private readonly IAzureAadGroupService azureAADGroupService;

        public UserGroupService(IAzureAadGroupService azureAADGroupService, ILogger<UserGroupService> logger)
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
                    logger.LogInformation("User '{userPrincipalName}' found.", userPrincipalName);
                }
               
                return result;
            }
            catch (ODataError odataException)
            {
                if (odataException.ResponseStatusCode == 404)
                {
                    logger.LogWarning("User '{userPrincipalName}' does not exist.", userPrincipalName);
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<bool> AddGroupMemberAsync(string groupId, string memberId)
        {
            var result = await azureAADGroupService.AddGroupMemberAsync(groupId, memberId);
            if (result)
            {
                logger.LogInformation("Added user({memberId}) to group({groupId})", memberId, groupId);
            }
            return result;
        }

        public async Task<bool> RemoveGroupMemberAsync(string groupId, string memberId)
        {
            var result = await azureAADGroupService.RemoveGroupMemberAsync(groupId, memberId);

            if (result)
            {
                logger.LogInformation("Removed user({memberId}) from the group({groupId})", memberId, groupId);
            }

            return result;
        }

        public async Task<string?> GetGroupIdAsync(string groupName)
        {
            var result = await azureAADGroupService.GetGroupIdAsync(groupName);

            if (!string.IsNullOrEmpty(result))
            {
                logger.LogInformation("Group '{groupName}' found.", groupName);
            }

            return result;

        }
        public async Task<List<AadGroupMember>> GetGroupMembersAsync(string groupId)
        {
            var result = await azureAADGroupService.GetGroupMembersAsync(groupId);
            if (result != null)
            {
                logger.LogInformation("Retrieved group members({Count}) from group({groupId}))", result.Count, groupId);
                return result.Adapt<List<AadGroupMember>>();
            }

            return [];
        }

        public async Task<List<AadGroup>> GetGroupMemberShipsAsync(string groupId)
        {
            var result = await azureAADGroupService.GetGroupMemberShipsAsync(groupId);

            if(result != null)
            {
                logger.LogInformation("Retrieved group memberships({Count}) from group({groupId}))", result.Count, groupId);
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
