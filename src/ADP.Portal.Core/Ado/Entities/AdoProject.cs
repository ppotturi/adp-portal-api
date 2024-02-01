using Microsoft.TeamFoundation.Core.WebApi;

namespace ADP.Portal.Core.Ado.Entities
{

    public class AdoProject(TeamProjectReference projectReference,
        List<string> serviceConnections, List<string> agentPools,
        List<AdoEnvironment> environments, List<AdoVariableGroup>? variableGroups)
    {
        public TeamProjectReference ProjectReference { get; set; } = projectReference;

        public List<string> ServiceConnections { get; set; } = serviceConnections;

        public List<string> AgentPools { get; set; } = agentPools;

        public List<AdoEnvironment> Environments { get; set; } = environments;

        public List<AdoVariableGroup>? VariableGroups { get; set; } = variableGroups;
    }
}
