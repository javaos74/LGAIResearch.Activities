using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Http;
using System.Globalization;
using System.IO;
using System.Activities;
using System.Net.Sockets;
using UiPath.LGAIResearch.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace UiPath.LGAIResearch.Activities.Utils
{


    public class UiPathExaoneClient
    {
        public UiPathExaoneClient() :
            this("http://exaone.myrobots.co.kr:5000/exaone/chat/completion")
        {
        }
        public UiPathExaoneClient(string endpoint)
        {
            this.endpoint = endpoint.EndsWith("/exaone/chat/completion") ? endpoint : endpoint + "/exaone/chat/completion";
            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public void setEndpoint(string endpoint)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                this.endpoint = endpoint.EndsWith("/exaone/chat/completion") ? endpoint : endpoint + "/exaone/chat/completion";
            }
        }
        public void setApiKey(string apikey)
        {
            this.client.DefaultRequestHeaders.Add("X-EXAONE-KEY", apikey);
        }

        public void setAuthorizationToken(string token)
        {
            this.client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            this.client.DefaultRequestHeaders.Add("Accept", "*/*");
            //this.client.DefaultRequestHeaders.Add("User-Agent", "dotnet/1.0.0");
        }

        public void setTopK(int topK)
        {
            this.payload.Add("top_k", topK);
        }
        public void setTopP(int topP)
        {
            this.payload.Add("top_p", topP);
        }
        public void setTemperature(double temperature)
        {
            this.payload.Add("temperature", temperature);
        }
        public void setUserPrompt(string userPrompt)
        {
            this.payload["user_prompt"] = userPrompt;
        }
        public void setSystemPrompt(string systemPrompt)
        {
            this.payload["system_prompt"] = systemPrompt;
        }
        public void setMaxNewToken(int maxNewToken)
        {
            this.payload.Add("max_new_tokens", maxNewToken);
        }

        public void Clear()
        {
            this.client.DefaultRequestHeaders.Add("Accept", "application/json");
            this.payload.Clear();
        }


        public async Task<LGAIExaoneResponse> SendRequestAsync()
        {
            var url = this.endpoint;
            LGAIExaoneResponse resp = new LGAIExaoneResponse();
            resp.status = HttpStatusCode.InternalServerError;
            try
            {
                using (var message = this.client.PostAsJsonAsync(url, this.payload))
                {
                    resp.status = message.Result.StatusCode;
                    resp.body = await message.Result.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Console.WriteLine(ex.Message);
#endif
                resp.status = HttpStatusCode.InternalServerError;
                resp.body = ex.Message;
            }
            return resp;
        }

        public List<ExaoneMessage> convertToMessageList(LGAIExaoneResponse resp)
        {
#if DEBUG
            System.Console.WriteLine($"response: {resp.body}"); 
#endif
            var tmp = resp.body.Replace("\\", string.Empty).Trim("\"".ToCharArray());
#if DEBUG
            System.Console.WriteLine($"ajusted: {tmp}");
#endif
            List<ExaoneMessage> messages = JsonConvert.DeserializeObject<List<ExaoneMessage>>(tmp); 
            return messages;
        }


        private Dictionary<string,object> payload = new Dictionary<string,object>();
        private HttpClient client;
        private string endpoint;
    }
}
