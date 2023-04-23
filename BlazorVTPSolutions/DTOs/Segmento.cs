using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace BlazorVTPSolutions.DTOs
{
    public class Segmento 
    {
        [JsonProperty("$oid")]
        public string oid { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }

        [JsonProperty("_id")]
        public JObject _id { get; set; }

        public void SetOidFromIdJObject()
        {
            oid = _id.GetValue("$oid").ToString();
        }
    }
}
