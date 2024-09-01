using System;
using System.Activities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UiPath.LGAIResearch.Activities.Properties;
using UiPath.LGAIResearch.Activities.Utils;
using UiPath.LGAIResearch.Models;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;


namespace UiPath.LGAIResearch.Activities
{
    [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_DisplayName))]
    [LocalizedDescription(nameof(Resources.AnalyzeDocument_Description))]
    public class AnalyzeDocument : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.Timeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.Timeout_Description))]
        public InArgument<int> TimeoutMS { get; set; } = 60000;

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_ApiKey_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_ApiKey_Description))]
        [LocalizedCategory(nameof(Resources.Authentication_Category))]
        public InArgument<string> ApiKey { get; set; }

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_Endpoint_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_Endpoint_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<string> Endpoint { get; set; }

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_FilePath_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_FilePath_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_RequestId_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_RequestId_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> RequestId { get; set; }

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_Molecules_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_Molecules_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<LGAIDDU[]> Moleculars { get; set; }

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_ErrorStatus_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_ErrorStatus_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> ErrorStatus { get; set; }

        [LocalizedDisplayName(nameof(Resources.AnalyzeDocument_EstimatedTime_DisplayName))]
        [LocalizedDescription(nameof(Resources.AnalyzeDocument_EstimatedTime_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        [Browsable(false)]
        public OutArgument<int> EstimatedTime { get; set; }
        #endregion

        private List<LGAIDDU> _moleculars;
        private string _requestId;
        private int _estimatedTime;
        private string _errorStatus;
        private UiPathHttpClient _client; 

        #region Constructors

        public AnalyzeDocument()
        {
            _requestId = string.Empty;
            _moleculars = new List<LGAIDDU>();
            _estimatedTime = -1;
            _errorStatus = "OK";
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (ApiKey == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(ApiKey)));
            if (FilePath == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(FilePath)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);
            var filepath = FilePath.Get(context);

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);

#if false  
            int idx = 0;
            foreach (var item in _molecules)
            {
                var svgImageFile = Path.Combine(Path.GetDirectoryName(filepath), 
                    Path.GetFileNameWithoutExtension(filepath) + "_" + idx + ".svg");
                var m = RWMol.MolFromSmiles(item.Smiles);
                RDKFuncs.prepareMolForDrawing(m);
                var view = new MolDraw2DSVG(item.BBox[2], item.BBox[3]);
                view.drawMolecule(m, Path.GetFileNameWithoutExtension(svgImageFile));
                view.finishDrawing();
                using (var w = new StreamWriter(svgImageFile))
                {
                    w.Write(view.getDrawingText());
                    Console.WriteLine($"{svgImageFile} is drawn.");
                }
            }
#endif

            // Outputs
            return (ctx) => {
                RequestId.Set(ctx, this._requestId);
                ErrorStatus.Set(ctx, this._errorStatus);
                Moleculars.Set(ctx, this._moleculars.ToArray());
                EstimatedTime.Set(ctx, this._estimatedTime);    
            };
        }

        private async Task ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {

            var apikey = ApiKey.Get(context);
            var filepath = FilePath.Get(context);
            var endpoint = Endpoint.Get(context);

            if (System.IO.File.Exists(filepath))
            {
                _client = new UiPathHttpClient(endpoint);
                _client.setApiKey(apikey);
                _client.AddFile(filepath);
                _client.AddField("params", "{\"inputs_format\":\"bytes\"}");

                var respUpload = await _client.Upload();
                if( respUpload.status == System.Net.HttpStatusCode.OK)
                {
                    var jobj = JObject.Parse(respUpload.body);
                    _requestId = (string)jobj["id"];
                    JArray exts = (JArray)jobj["outputs"];
                    foreach( var ext in exts)
                    {
                        JToken _t = (JToken)ext["estimated_time"];
                        _estimatedTime = (int)_t;
                    }
#if DEBUG
                    Console.WriteLine($"respons Id: {_requestId} estimatedTime: {_estimatedTime}");
#endif
                    Console.WriteLine($"요청을 처리하는데 약 {_estimatedTime}초가 걸립니다...");
                    await Task.Delay(_estimatedTime * 1000);

                    _client.Clear();
                    var respGet = await _client.Get(_requestId);
                    if( respGet.status == System.Net.HttpStatusCode.OK)
                    {
                        _moleculars.Clear();
#if DEBUG
                        Console.WriteLine($"Response body: {respGet.body.Substring(0,Math.Min(100, respGet.body.Length))}");
#endif
                        var jobj2 = JObject.Parse(respGet.body);
                        int idx = 0;
                        JArray pages = (JArray)jobj2["outputs"];
                        if (pages != null)
                        {
                            foreach (var p in pages)
                            {
                                JArray mols = (JArray)p["elements"]["molecule"];
                                if (mols != null)
                                {
                                    idx = 0;
                                    foreach (var mol in mols)
                                    {
#if DEBUG
                                        Console.WriteLine($"pred_mol: {mol["pred_mol"]}");
#endif
                                        _moleculars.Add(new LGAIDDU
                                        {
                                            LayoutPreservingMol = (string)mol["layout_preserving_mol"],
                                            PredMol = (string)mol["pred_mol"],
                                            Smiles = (string)mol["smiles"],
                                            BBox = new int[] { (int)mol["bbox"][0], (int)mol["bbox"][1], (int)mol["bbox"][2], (int)mol["bbox"][3] },
                                            SvgImagePath = string.Empty,
                                            Page = (int)p["page"],
                                            Seq = idx
                                        });
                                        idx++;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _errorStatus = respUpload.body;
                    }
                    
                }
                else
                {
                    _errorStatus = respUpload.body;
                }
            }

        }


#endregion


    }
}

