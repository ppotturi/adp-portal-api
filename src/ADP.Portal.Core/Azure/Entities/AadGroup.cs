namespace ADP.Portal.Core.Azure.Entities
{
    public class AadGroup
    {
        public string? Id { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }
        public required string OwnerId { get; set; }
    }
}
