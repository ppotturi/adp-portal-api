
namespace ADP.Portal.Core.Azure.Infrastructure
{
    public interface IAzureAadGroupService
    {
        Task<string?> GetUserIdAsync(string userPrincipalName);

        Task<bool> ExistingMemberAsync(Guid groupId, string userPrincipalName);

        Task<bool> AddToAADGroupAsync(Guid groupId, string userId);
        
    }
}