using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using UiPath.LGAIResearch.Activities.Properties;
using UiPath.LGAIResearch.Activities.Utils;
using UiPath.LGAIResearch.Models;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

namespace UiPath.LGAIResearch.Activities
{
    [LocalizedDisplayName(nameof(Resources.GenerateContent_DisplayName))]
    [LocalizedDescription(nameof(Resources.GenerateContent_Description))]
    public class GenerateContent : ContinuableAsyncCodeActivity
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

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.GenerateContent_Endpoint_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_Endpoint_Description))]
        [LocalizedCategory(nameof(Resources.Authentication_Category))]
        public InArgument<string> Endpoint { get; set; }

        [LocalizedDisplayName(nameof(Resources.GenerateContent_ApiKey_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_ApiKey_Description))]
        [LocalizedCategory(nameof(Resources.Authentication_Category))]
        public InArgument<string> ApiKey { get; set; }

        [LocalizedDisplayName(nameof(Resources.GenerateContent_SystemPrompt_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_SystemPrompt_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> SystemPrompt { get; set; } = "You are EXAONE model from LG AI Research, a helpful assistant.";

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.GenerateContent_UserPrompt_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_UserPrompt_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> UserPrompt { get; set; }

        [LocalizedDisplayName(nameof(Resources.GenerateContent_Temperature_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_Temperature_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<double> Temperature { get; set; } = 0.1d;

        [LocalizedDisplayName(nameof(Resources.GenerateContent_TopP_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_TopP_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> TopP { get; set; } = 1;

        [LocalizedDisplayName(nameof(Resources.GenerateContent_TopK_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_TopK_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> TopK { get; set; } = 50;

        [LocalizedDisplayName(nameof(Resources.GenerateContent_MaxNewToken_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_MaxNewToken_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> MaxNewToken { get; set; } = 1024;

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.GenerateContent_Response_DisplayName))]
        [LocalizedDescription(nameof(Resources.GenerateContent_Response_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<List<ExaoneMessage>> Response { get; set; }


        private UiPathExaoneClient _client;
        private LGAIExaoneResponse _resp;
        #endregion


        #region Constructors

        public GenerateContent()
        {
            this._client = new UiPathExaoneClient();
            this._resp = new LGAIExaoneResponse();
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (Endpoint == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Endpoint)));
            if (UserPrompt == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(UserPrompt)));

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
                if (this._resp.status == System.Net.HttpStatusCode.OK)
                {
                    Response.Set(ctx, _client.convertToMessageList(this._resp));
                }
            };
        }

        private async Task ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {
            ///////////////////////////
            // Add execution logic HERE
            ///////////////////////////
            var endpoint = Endpoint.Get(context);
            var apikey = ApiKey.Get(context);
            var systemprompt = SystemPrompt.Get(context);
            var userprompt = UserPrompt.Get(context);
            var temperature = Temperature.Get(context);
            var topp = TopP.Get(context);
            var topk = TopK.Get(context);
            var maxnewtoken = MaxNewToken.Get(context);

            this._client.setTemperature(temperature);
            this._client.setTopP(topp);
            this._client.setTopK(topk);
            this._client.setEndpoint(endpoint);
            this._client.setMaxNewToken(maxnewtoken);
            this._client.setSystemPrompt(systemprompt);
            this._client.setUserPrompt(userprompt);

            this._resp = await _client.SendRequestAsync();
        }

        #endregion
    }
}

