namespace ADP.Portal.Api.Models.Ado
{

    public class AdoGroup
    {
        public required string id { get; set; }

        public required string providerDisplayName { get; set; }

    }

    public class JsonAdoGroupWrapper
    {
        public int count { get; set; }

        public List<AdoGroup>? value { get; set; }

    }


}
