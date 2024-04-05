using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsGroupsConfigService
    {
        Task<IEnumerable<Group>> GetGroupsConfigAsync(string tenantName, string teamName, GitRepo gitRepo);

        Task<GroupConfigResult> CreateGroupsConfigAsync(string tenantName, string teamName, GitRepo gitRepo, IEnumerable<string> groupMembers);

        Task<GroupSyncResult> SyncGroupsAsync(string tenantName, string teamName, string ownerId, GroupType? groupType, GitRepo gitRepo);
    }
}