using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
[assembly: Xamarin.Forms.Dependency(typeof(XamEntityManager.Service.UrlService))]

namespace XamEntityManager.Service
{
    public class UrlService
    {
		[Preserve]
		public UrlService()
		{
		}
        // For the secretid
        private string sid = "";

        public string Sid
        {
            get
            {
                return sid;
            }

            set
            {
                sid = value;
            }
        }

        public string Servername { get; internal set; }

        public string makeApi(string module, string action = null, string id = null, IDictionary<string, object> args = null)
        {
            module = char.ToUpper(module[0]) + module.Substring(1); // uc first
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }
            if (Sid != "")
            {
                // Login token
                args["sessionid"] = Sid;
            }

            string  url = module;
            if (action != null)
            {
                url += "/" + action;
                if (id != null)
                {
                    url += '/' + id;
                }
            }
            url += ".json";
            var first = true;
            foreach (var value in args)
            {
                if (first) { url += "/?"; first = false; }
                else { url += "&"; }
                url += value.Key + "=" + value.Value;
            }
            
            return Servername + url;
        }
    }
}
