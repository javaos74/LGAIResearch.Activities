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
        [JsonProperty("bbox")]
        public int[] BBox {get; set; }

        [JsonProperty("layout_preserving_mol")]
        public string LayoutPreservingMol { get; set; }

        [JsonProperty("pred_mol")]
        public string PredMol { get; set; }

        [JsonProperty("smiles")]
        public string Smiles { get; set; }

        //RDkit을 이용해서 이미지로 변경해서 처리하는게 필요 이미지 파일로 줘야 할지? 
        //public System.Drawing.Imaging.Image PngPicture { get; set; }
        public string SvgImagePath { get; set; }

        public int Page { get; set; }
    }

    public class LGAIDDUResponse
    {
        public HttpStatusCode status { get; set; }
        public string body { get; set; }
    }
}
