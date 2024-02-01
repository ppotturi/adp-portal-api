using System.ComponentModel.DataAnnotations;

namespace ADP.Portal.Api.Models
{
    public class AdoVariable
    {        
        public required string Name { get; set; }

        public required string Value { get; set; }

        public bool IsSecret { get; set; }
    }
}
