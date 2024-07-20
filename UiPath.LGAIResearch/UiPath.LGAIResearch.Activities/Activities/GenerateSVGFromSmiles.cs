using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Activities;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UiPath.LGAIResearch.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

namespace UiPath.LGAIResearch.Activities
{
    [LocalizedDisplayName(nameof(Resources.GenerateSVGFromSmiles_DisplayName))]
    [LocalizedDescription(nameof(Resources.GenerateSVGFromSmiles_Description))]
    public class GenerateSVGFromSmiles : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.GenerateSVGFromSmiles_Smiles_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateSVGFromSmiles_Smiles_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Smiles { get; set; }

        [LocalizedDisplayName(nameof(Resources.GenerateSVGFromSmiles_Endpoint_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateSVGFromSmiles_Endpoint_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Endpoint { get; set; }

        [LocalizedDisplayName(nameof(Resources.GenerateSVGFromSmiles_SvgPath_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateSVGFromSmiles_SvgPath_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> SvgPath { get; set; }


        HttpClient _client; 
        #endregion


        #region Constructors

        public GenerateSVGFromSmiles()
        {
            _client = new HttpClient();
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (Smiles == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Smiles)));
            if (Endpoint == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Endpoint)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var smiles = Smiles.Get(context);
            var endpoint = Endpoint.Get(context);
            var jObj = new JObject();

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(smiles);
            jObj.Add("smiles", System.Convert.ToBase64String(plainTextBytes));
            HttpContent content = new StringContent( JsonConvert.SerializeObject(jObj) );
            var resp = await _client.PostAsync(endpoint, content);
            String targetFilePath= Path.ChangeExtension(Path.GetTempFileName(), "svg");
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using (var streamToReadFrom = await resp.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(targetFilePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(fileStream);
                }
            }

            // Outputs
            return (ctx) => {
                SvgPath.Set(ctx, targetFilePath);
            };
        }

        #endregion
    }
}

