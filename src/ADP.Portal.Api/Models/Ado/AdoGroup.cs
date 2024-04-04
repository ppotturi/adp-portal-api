using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ADP.Portal.Api.Models.Ado
{

    public class AdoGroup
    {
        public string id { get; set; }

        public string providerDisplayName { get; set; }

        // extra fields
        [JsonExtensionData]
        private IDictionary<string, JToken> _extraStuff;

    }

    public class JsonAdoGroupWrapper
    {
        public int count { get; set; }

        public List<AdoGroup> value { get; set; }

    }


}
