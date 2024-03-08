


using Microsoft.Graph.Models;

namespace ADP.Portal.Core.Azure.Infrastructure
{
    public interface IAzureAadGroupService
    {
        Task<string?> GetUserIdAsync(string userPrincipalName);

        Task<bool> ExistingMemberAsync(string groupId, string userPrincipalName);

        Task<bool> AddGroupMemberAsync(string groupId, string directoryObjectId);

        Task<bool> RemoveGroupMemberAsync(string groupId, string directoryObjectId);

        Task<string?> GetGroupIdAsync(string groupName);

        Task<List<T>?> GetGroupMembersAsync<T>(string groupId);

        Task<List<Group>?> GetGroupMemberShipsAsync(string groupId);

        Task<Group?> AddGroupAsync(Group group);
    }
}