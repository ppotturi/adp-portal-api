namespace ADP.Portal.Core.Ado.Dtos;
public class OnboardProjectResult
{
    public IEnumerable<Guid> ServiceConnectionIds { get; set; } = Enumerable.Empty<Guid>();
    public IEnumerable<int> VariableGroupIds { get; set; } = Enumerable.Empty<int>();
    public IEnumerable<int> AgentQueueIds { get; set; } = Enumerable.Empty<int>();
    public IEnumerable<int> EnvironmentIds { get; set; } = Enumerable.Empty<int>();
}
