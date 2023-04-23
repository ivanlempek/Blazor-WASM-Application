using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlazorVTPSolutions.DTOs
{
    public class Empresa
    {
        public Empresa()
        {
            SegmentoEmpresa = new Segmento();
        }

        [JsonProperty("_id")]
        private JObject _id { get; set; }

        [JsonIgnore]
        public string oid { get; set; }
        public string Nome { get; set; }
        public string Site { get; set; }

        [JsonIgnore]
        public string SegmentoId { get; set; }

        [JsonIgnore]
        public Segmento SegmentoEmpresa { get; set; }

        [JsonProperty("Segmento")]
        public JObject Segmento { get; set; }

        public void SetOidFromIdJObject()
        {
            oid = _id.GetValue("$oid").ToString();
            SegmentoId = Segmento.GetValue("$oid").ToString();
        }
    }
}
