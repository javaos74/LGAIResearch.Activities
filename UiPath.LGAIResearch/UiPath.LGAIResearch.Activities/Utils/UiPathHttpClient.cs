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

namespace UiPath.LGAIResearch.Activities.Utils
{


    public class UiPathHttpClient
    {
        public UiPathHttpClient() :
            this("https://gw.lgair.net")
        {
        }
        public UiPathHttpClient( string endpoint)
        {
            this.endpoint = endpoint;
            this.client = new HttpClient();
            this.content = new MultipartFormDataContent("lgaiddu----" + DateTime.Now.Ticks.ToString());
        }

        public void setEndpoint( string endpoint)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                this.endpoint = endpoint;
            }
        }
        public void setApiKey( string apikey)
        {
            this.client.DefaultRequestHeaders.Add("X-API-Key", apikey);
        }

        public void setAuthorizationToken(string token)
        {
            this.client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            this.client.DefaultRequestHeaders.Add("Accept", "*/*");
            //this.client.DefaultRequestHeaders.Add("User-Agent", "dotnet/1.0.0");
        }

        public void AddFile(string fileName, string fieldName = "inputs")
        {
            var fstream = System.IO.File.OpenRead(fileName);
#if DEBUG
            Console.WriteLine($"file size: {fstream.Length}");
#endif
            byte[] buf = new byte[fstream.Length];
            int read_bytes = 0;
            int offset = 0;
            int remains = (int)fstream.Length;
            do
            {
                read_bytes += fstream.Read(buf, offset, remains);
                offset += read_bytes;
                remains -= read_bytes;
            } while (remains != 0);
            fstream.Close();

            this.content.Add(new StreamContent(new MemoryStream(buf)), fieldName, System.IO.Path.GetFileName(fileName));
        }
 
        public void AddField( string name, string value)
        {
            this.content.Add(new StringContent(value), name);
        }

        public void Clear()
        {
            this.content.Dispose();
            this.content = new MultipartFormDataContent("lgapddu----" + DateTime.Now.Ticks.ToString());
            this.client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<LGAIDDUResponse> Upload()
        {
            var url = this.endpoint.EndsWith("/") ? this.endpoint + "api/vision/ddu/manager" : this.endpoint + "/api/vision/ddu/manager";
#if DEBUG
            Console.WriteLine("http content count :" + this.content.Count());
#endif
            using (var message = this.client.PostAsync(url, this.content))
            {
                LGAIDDUResponse resp = new LGAIDDUResponse();
                resp.status = message.Result.StatusCode;
                resp.body = await message.Result.Content.ReadAsStringAsync();
                return resp;
            }
        }

        public async Task<LGAIDDUResponse> Get( string reqId)
        {
            var url = this.endpoint.EndsWith("/") ? this.endpoint + $"api/vision/ddu/manager?id={reqId}" : this.endpoint + $"/api/vision/ddu/manager?id={reqId}";
#if DEBUG
            Console.WriteLine($"get endpoint: {url}");
#endif
            using (var message = this.client.GetAsync(url))
            {
                LGAIDDUResponse resp = new LGAIDDUResponse();
                resp.status = message.Result.StatusCode;
                resp.body = await message.Result.Content.ReadAsStringAsync();
                return resp;
            }
        }


        private HttpClient client;
        private string endpoint;
        private MultipartFormDataContent content;
    }
}
