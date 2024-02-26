

namespace ADP.Portal.Core.Azure.Services
{
    public interface IUserGroupService
    {
        Task<string?> GetUserIdAsync(string userPrincipalName);
        public Task<bool> AddUserToGroupAsync(Guid groupId, string userPrincipalName, string userId);
    }
}
