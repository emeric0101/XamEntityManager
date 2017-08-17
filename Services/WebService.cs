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
using Xamarin.Forms;
using Xamarin.Forms.Internals;
[assembly: Xamarin.Forms.Dependency(typeof(XamEntityManager.Service.WebService))]

namespace XamEntityManager.Service
{
    public class WebServiceJsonErrorException : Exception
    {
        public JsonReaderException JsonException { get; set; }
        public WebServiceJsonErrorException(string content, JsonReaderException jsonex) : base(content)
        {
            JsonException = jsonex;
        }
    }

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

        UrlService urlService = DependencyService.Get<UrlService>();


        /// <summary>
        /// Determine if the response is success by json check
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private JObject parseResponse(string json)
        {
            JObject data;
            try
            {
                data = JObject.Parse(json);
            }
            catch (JsonReaderException jsonEx)
            {
                throw new WebServiceJsonErrorException(json, jsonEx);
            }

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

        async public Task<JObject> getAsync(string module, string action = null, string id = null, Dictionary<string, object> args = null)
        {
            string url = urlService.makeApi(module, action, id, args);
            return await getAsync(url);

        }
        /// <summary>
        /// Perform a simple get request (without checking result)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        async public Task<HttpResponseMessage> getOutsideAsync(string url)
        {
            var id = postId++;
            Debug.WriteLine("getAsync : " + id + " " + url);
            HttpResponseMessage msg = null;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "doggiappli");
                try
                {
                    msg = await httpClient.GetAsync(url);
                    if (msg == null)
                    {
                        Debug.WriteLine("getAsync : msg is null");
                        throw new WebServiceBadResultException();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("getAsync Error" + e.Message);
                    throw new WebServiceBadResultException();
                }
                Debug.WriteLine("getAsync " + id + " Done ");
                return msg;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        async public Task<JObject> getAsync(string url)
        {     
            HttpResponseMessage response = await getOutsideAsync(url);
            string content = await response.Content.ReadAsStringAsync();

            //Debug.WriteLine("getAsync response : " + content);
            return parseResponse(content); ;
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
                    return parseResponse(responseStr);
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
                httpClient.DefaultRequestHeaders.Add("User-Agent", "xamentitymanager");
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


            Debug.WriteLine("postAsync " + id + "response : " + responseStr);
            //Debug.WriteLine("postAsync " + id + " done");

            return parseResponse(responseStr);
        }
        /// <summary>
        /// Download and save a file
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static int dlId = 0;
        public async Task<string> DownloadAndSaveFileAsync(string url, string filename, bool replace = false, int IOExceptionCatched = 0)
        {
            //System.Diagnostics.Debug.WriteLine("Downloading image " + dlId + " : " + url);

            PCLStorage.IFile file;
            try
            {
                if (replace)
                {
                    throw new PCLStorage.Exceptions.FileNotFoundException("replace needed");
                }
                file = await PCLStorage.FileSystem.Current.LocalStorage.GetFileAsync(filename);
            }
            catch (PCLStorage.Exceptions.FileNotFoundException)
            {
                // Download
                byte[] fileBytes = await DownloadFileAsync(url);

                file = await PCLStorage.FileSystem.Current.LocalStorage.CreateFileAsync(filename, PCLStorage.CreationCollisionOption.ReplaceExisting);
          
                using (var streamFile = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
                {
                    await streamFile.WriteAsync(fileBytes, 0, fileBytes.Length);
                    await streamFile.FlushAsync();
                }
                //System.Diagnostics.Debug.WriteLine("Downloading image done " + dlId);
            }
            catch (System.IO.IOException e)
            {
                IOExceptionCatched++;
                if (IOExceptionCatched > 2)
                {
                    throw e;
                }
                await Task.Delay(1000);
                return await DownloadAndSaveFileAsync(url, filename, replace, IOExceptionCatched);
            }
            return file.Path;

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
