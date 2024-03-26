namespace ADP.Portal.Api.Models.Ado
{
    public class OnBoardAdoProjectRequest
    {
        public required List<AdoEnvironment> Environments { get; set; }

        public required List<string> ServiceConnections { get; set; }

        public required List<string> AgentPools { get; set; }

        public List<AdoVariableGroup>? VariableGroups { get; set; }
    }
}
