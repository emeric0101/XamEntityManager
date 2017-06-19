using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace XamEntityManager.Service
{
    public class WebServiceFalseResultException : Exception {
        string errorMsg;
        public WebServiceFalseResultException(string errorMsg)
        {
            this.errorMsg = errorMsg;
        }
        public string ErrorMsg
        {
            get
            {
                return errorMsg;
            }

            set
            {
                errorMsg = value;
            }
        }
    }
    public class WebServiceBadResultException : Exception { }
    public class WebService
    {
        public static Type[] inject = {
            typeof(UrlService)
        };
        UrlService urlService;
		[PreserveAttribute]
        public WebService(UrlService url)
        {
            urlService = url;
        }
    

        /// <summary>
        /// Determine if the response is success by json check
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private JObject parseResponse(JObject data)
        {
            if (data["success"] == null)
            {
                throw new WebServiceBadResultException();
            }
            if (data.Value<Boolean>("success") == false)
            {
                var errorMsg = "";
                if (data["errMsg"] != null)
                {
                    errorMsg = data.Value<string>("errMsg");
                }
                throw new WebServiceFalseResultException(errorMsg);
            }
            return data;
        }

        async public Task<JObject> getAsync(string module, string action = null, string id = null, Dictionary<string, dynamic> args = null)
        {
            string url = urlService.makeApi(module, action, id, args);
            return await getAsync(url);

        }
        async public Task<JObject> getAsync(string url)
        {
            var id = postId++;
            Debug.WriteLine("getAsync : " + id  + " " + url);
            string content = "";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "doggiappli");
                try
                {
                    HttpResponseMessage msg = await httpClient.GetAsync(url);
                    if (msg == null)
                    {
                        Debug.WriteLine("getAsync : msg is null");
                        throw new WebServiceBadResultException();
                    }
                    content = await msg.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("getAsync Error" + e.Message);
                    throw new WebServiceBadResultException();
                }
                Debug.WriteLine("getAsync " + id + " Done ");
            }

            //Debug.WriteLine("getAsync response : " + content);
            JObject data = JObject.Parse(content);
            return parseResponse(data); ;
        }

        async public Task<JObject> postJpgAsync(string module, string action, Stream file)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
                MultipartFormDataContent content = new MultipartFormDataContent();
                var streamContent = new StreamContent(file);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(streamContent, "file", "file.jpg");
                try
                {
                    HttpResponseMessage msg = await httpClient.PostAsync(urlService.makeApi(module, action), content);
                    if (msg == null)
                    {
                        throw new Exception("Unable to send image");
                    }
                    var responseStr = await msg.Content.ReadAsStringAsync();
                    //Debug.WriteLine("postAsync response : " + responseStr);
                    JObject data = JObject.Parse(responseStr);
                    return parseResponse(data);
                }
                catch (Exception e)
                {
                    throw new WebServiceBadResultException();
                }
            }
        }


        async public Task<JObject> postAsync(string module, string action = null, string id = null, object argsPost = null)
        {
            string url = urlService.makeApi(module, action, id);
            return await postAsync(url, argsPost);

        }
		static int postId = 0;
        public async Task<JObject> postAsync(string url, object args)
        {
            int id = postId++;
            string responseStr = "";
            string json = JsonConvert.SerializeObject(args);
            
           	Debug.WriteLine("postAsync " + id + " " + url + " : " + " HTTP " + ": " + json);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "doggiappli");
                try
                {

                    HttpResponseMessage msg = await httpClient.PostAsync(url, content);
                    if (msg == null)
                    {
                        Debug.WriteLine("postAsync : msg is null");
                        throw new WebServiceBadResultException();
                    }
                    responseStr = await msg.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("postAsync Error" + e.Message);
                    throw new WebServiceBadResultException();
                }
            }


			//Debug.WriteLine("postAsync " + id + "response : " + responseStr);
			//Debug.WriteLine("postAsync " + id + " done");
                   JObject data = JObject.Parse(responseStr);
            return parseResponse(data);
        }

		public async Task<byte[]> DownloadFileAsync(string url)
		{
			using (HttpClient httpClient = new HttpClient())
			{
				var response = await httpClient.GetAsync(url);
				HttpContent content = response.Content;
				return await content.ReadAsByteArrayAsync();
			}
		}
    }
}
