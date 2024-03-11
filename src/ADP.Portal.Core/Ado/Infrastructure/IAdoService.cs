using ADP.Portal.Core.Ado.Entities;
using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Infrastructure
{
    public interface IAdoService
    {

        Task<TeamProject> GetTeamProjectAsync(string projectName);

        /// <summary>
        /// Shares service endpoints from the ADP project with the onboarded project.
        /// </summary>
        /// <param name="adpProjectName">The name of the ADP project.</param>
        /// <param name="serviceConnections">List of service connection names to share with the onboarded project.</param>
        /// <param name="onBoardProject">The project being onboarded.</param>
        /// <returns>A list of service connection IDs assigned to <paramref name="onBoardProject"/>.</returns>
        Task<IEnumerable<Guid>> ShareServiceEndpointsAsync(string adpProjectName, List<string> serviceConnections, TeamProjectReference onBoardProject);

        /// <summary>
        /// Adds environments to the onboarded project.
        /// </summary>
        /// <param name="adoEnvironments">The environments to add.</param>
        /// <param name="onBoardProject">The project being onboarded.</param>
        /// <returns>A list of environment IDs added to <paramref name="onBoardProject"/>.</returns>
        Task<IEnumerable<int>> AddEnvironmentsAsync(List<AdoEnvironment> adoEnvironments, TeamProjectReference onBoardProject);

        /// <summary>
        /// Shares agent pools from the ADP project with the onboarded project.
        /// </summary>
        /// <param name="adpProjectName">The name of the ADP project.</param>
        /// <param name="adoAgentPoolsToShare">List of agent pool names to share with the onboarded project.</param>
        /// <param name="onBoardProject">The project being onboarded.</param>
        /// <returns>A list of agent queue IDs assigned to <paramref name="onBoardProject"/>.</returns>
        Task<IEnumerable<int>> ShareAgentPoolsAsync(string adpProjectName, List<string> adoAgentPoolsToShare, TeamProjectReference onBoardProject);

        /// <summary>
        /// Adds or updates variable groups in the onboarded project.
        /// </summary>
        /// <param name="adoVariableGroups">The variable groups to configure.</param>
        /// <param name="onBoardProject">The project being onboarded.</param>
        /// <returns>A list of variable groups assigned to <paramref name="onBoardProject"/>.</returns>
        Task<IEnumerable<int>> AddOrUpdateVariableGroupsAsync(List<AdoVariableGroup> adoVariableGroups, TeamProjectReference onBoardProject);
    }
}