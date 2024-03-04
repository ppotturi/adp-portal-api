


using ADP.Portal.Core.Azure.Entities;
using Microsoft.Graph.Models;

namespace ADP.Portal.Core.Azure.Services
{
    public interface IUserGroupService
    {
        Task<string?> GetUserIdAsync(string userPrincipalName);

        public Task<bool> AddGroupMemberAsync(string groupId, string memberId);

        Task<bool> RemoveGroupMemberAsync(string groupId, string memberId);

        Task<string?> GetGroupIdAsync(string groupName);

        Task<List<AadGroupMember>> GetGroupMembersAsync(string groupId);

        Task<List<AadGroup>> GetGroupMemberShipsAsync(string groupId);

        Task<string?> AddGroupAsync(AadGroup aadGroup);
    }
}
