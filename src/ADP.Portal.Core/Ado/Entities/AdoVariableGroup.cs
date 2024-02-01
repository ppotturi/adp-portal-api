namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoVariableGroup(string name, List<AdoVariable> variables, string? description)
    {
        public string Name { get; set; } = name;

        public List<AdoVariable> Variables { get; set; } = variables;
        public string? Description { get; set; } = description;
    }
}
