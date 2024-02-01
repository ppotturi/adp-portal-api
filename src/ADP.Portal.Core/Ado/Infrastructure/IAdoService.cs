using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoService
    {

        Task<TeamProject> GetTeamProjectAsync(string projectName);

        Task ShareServiceEndpointsAsync(string adpProjectName, List<string> serviceConnections, TeamProjectReference onBoardProject);

        Task AddEnvironmentsAsync(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject);

        Task ShareAgentPoolsAsync(string adpPrjectName, List<string> adoAgentPoolsToShare, TeamProjectReference onBoardProject);

        Task AddOrUpdateVariableGroupsAsync(List<AdoVariableGroup> adoVariableGroups, TeamProjectReference onBoardProject);
    }
}