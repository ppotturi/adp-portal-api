namespace ADP.Portal.Api.Models.Ado
{
    public class AdoVariable
    {
        public required string Name { get; set; }

        public required string Value { get; set; }

        public bool IsSecret { get; set; }
    }
}
