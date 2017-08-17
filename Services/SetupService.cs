using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Xamarin.Forms;
[assembly: Xamarin.Forms.Dependency(typeof(XamEntityManager.Service.SetupService))]

namespace XamEntityManager.Service
{

    public class SetupService
    {
        UrlService url = DependencyService.Get<UrlService>();
        LoginService login = DependencyService.Get<LoginService>();
        DbService db = DependencyService.Get<DbService>();
        RepositoryService repo = DependencyService.Get<RepositoryService>();
        public string Servername
        {
            get
            {
                return url.Servername;
            }
            set
            {
                url.Servername = value;
            }
        }


        public void Init(string servername)
        {
            Servername = servername;

            // Define the user type

            // Init SQLite
            db.init();
        }
        
    }
}
