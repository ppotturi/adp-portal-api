using ADP.Portal.Core.Azure.Entities;

namespace ADP.Portal.Core.Azure.Services
{
    public interface IGroupService
    {
        Task<string?> GetUserIdAsync(string userPrincipalName);

        Task<bool> AddGroupMemberAsync(string groupId, string memberId);

        Task<bool> RemoveGroupMemberAsync(string groupId, string memberId);

        Task<string?> GetGroupIdAsync(string groupName);

        Task<List<AadGroupMember>> GetUserTypeGroupMembersAsync(string groupId);

        Task<List<AadGroupMember>> GetGroupTypeGroupMembersAsync(string groupId);

        Task<List<AadGroup>> GetGroupMemberShipsAsync(string groupId);

        Task<string?> AddGroupAsync(AadGroup aadGroup);
    }
}
