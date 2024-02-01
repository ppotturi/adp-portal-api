using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models
{
    public class AdoVariableGroup
    {
        
        public required string Name { get; set; }

        public string? Description { get; set; }

        public required List<AdoVariable> Variables { get; set; }
    }
}
