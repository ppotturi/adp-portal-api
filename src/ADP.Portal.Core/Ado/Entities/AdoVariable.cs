namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoVariable(string name, string value, bool isSecret)
    {
        public string Name { get; set; } = name;
        public string Value { get; set; } = value;
        public bool IsSecret { get; set; } = isSecret;
    }
}
