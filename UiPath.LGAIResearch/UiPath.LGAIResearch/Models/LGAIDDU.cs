using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UiPath.LGAIResearch.Models
{
    [JsonObject("molecule")]
    public class LGAIDDU
    {
        [JsonProperty("hbox")]
        public int[] BBox {get; set; }
        [JsonProperty("layout_preserving_mol")]
        public string LayoutPreservingMol { get; set; }
        [JsonProperty("pred_mol")]
        public string PredMol { get; set; }
        [JsonProperty("smiles")]
        public string Smiles { get; set; }
    }

    public class LGAIDDUResponse
    {
        public HttpStatusCode status { get; set; }
        public string body { get; set; }
    }
}
