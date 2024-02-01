namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoEnvironment(string name, string? description)
    {
        public string Name { get; set; } = name;

        public string? Description { get; set; } = description;
    }
}
