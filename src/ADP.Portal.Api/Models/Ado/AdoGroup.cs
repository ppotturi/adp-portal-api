using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ADP.Portal.Api.Models.Ado
{

    public class AdoGroup
    {
        public required string id { get; set; }

        public required string providerDisplayName { get; set; }

        // extra fields
        [JsonExtensionData]
        private IDictionary<string, JToken>? _extraStuff;

        public IDictionary<string, JToken>?  getStuff()
        {
            return _extraStuff;
        }

        public void setStuff()
        {
            _extraStuff = new Dictionary<string, JToken>();
        }

    }

    public class JsonAdoGroupWrapper
    {
        public int count { get; set; }

        public List<AdoGroup>? value { get; set; }

    }


}
