using ADP.Portal.Core.Azure.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.TeamFoundation.Core.WebApi;

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
                return await azureAADGroupService.GetUserIdAsync(userPrincipalName);
            }
            catch (ODataError odataException)
            {
                if (odataException.ResponseStatusCode == 404)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<bool> AddUserToGroupAsync(Guid groupId, string userPrincipalName, string userId)
        {
            var isExistingMember = await azureAADGroupService.ExistingMemberAsync(groupId, userPrincipalName);
            if (isExistingMember)
            {
                logger.LogInformation("User:'{userPrincipalName}' already a member of the group:'{groupId}'", userPrincipalName, groupId);
            }
            else
            {
                await azureAADGroupService.AddToAADGroupAsync(groupId, userId);
                logger.LogInformation("User:'{userPrincipalName}' added to the group:{groupId}", userPrincipalName, groupId);
            }
            return true;
        }
    }
}
