using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UiPath.LGAIResearch.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;
using UiPath.LGAIResearch.Models;
using UiPath.LGAIResearch.Activities.Utils;
using Newtonsoft.Json.Linq;

namespace UiPath.LGAIResearch.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetResult_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetResult_Description))]
    public class GetResult : ContinuableAsyncCodeActivity
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

        [LocalizedDisplayName(nameof(Resources.GetResult_Endpoint_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetResult_Endpoint_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<string> Endpoint { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetResult_ApiKey_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetResult_ApiKey_Description))]
        [LocalizedCategory(nameof(Resources.Authentication_Category))]
        [RequiredArgument]
        public InArgument<string> ApiKey { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetResult_RequestId_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetResult_RequestId_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<string> RequestId { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetResult_DDUData_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetResult_DDUData_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<LGAIDDU[]> Moleculars{ get; set; }

        [LocalizedDisplayName(nameof(Resources.GetResult_ErrorMessage_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetResult_ErrorMessage_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> ErrorMessage { get; set; }

        #endregion

        private List<LGAIDDU> _moleculars;
        private string _requestId;
        private string _errorMessage;
        private UiPathHttpClient _client;

        #region Constructors

        public GetResult()
        {
            _requestId = string.Empty;
            _moleculars = new List<LGAIDDU>();
            _errorMessage = "OK";
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (Endpoint == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Endpoint)));
            if (ApiKey == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(ApiKey)));
            if (RequestId == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(RequestId)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);


            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);

            // Outputs
            return (ctx) => {
                Moleculars.Set(ctx, _moleculars.ToArray());
                ErrorMessage.Set(ctx, _errorMessage);
            };
        }

        private async Task ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {
            var endpoint = Endpoint.Get(context);
            var apikey = ApiKey.Get(context);
            var requestid = RequestId.Get(context);

            _client = new UiPathHttpClient(endpoint);
            _client.setApiKey(apikey);

            var respGet = await _client.Get(requestid);
            if (respGet.status == System.Net.HttpStatusCode.OK)
            {
                _moleculars.Clear();
#if DEBUG
                Console.WriteLine(respGet.body.Substring(0, Math.Min(100, respGet.body.Length)));
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
                _moleculars.Clear();
                _errorMessage = respGet.body;
            }

        }

        #endregion
    }
}

