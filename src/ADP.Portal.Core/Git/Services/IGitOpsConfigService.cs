using ADP.Portal.Core.Git.Entities;

namespace ADP.Portal.Core.Git.Services
{
    public interface IGitOpsConfigService
    {
        
        Task<bool> IsConfigExistsAsync(string teamName, ConfigType configType, string tenantName, GitRepo gitRepo);
        Task<GroupSyncResult> SyncGroupsAsync(string tenantName, string teamName, string ownerId, GroupType? groupType, GitRepo gitRepo);
        Task GenerateFluxTeamConfig(string teamName, GitRepo gitRepo);
    }
}