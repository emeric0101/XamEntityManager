using XamEntityManager.Entity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Forms.Internals;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(XamEntityManager.Service.LoginService))]
namespace XamEntityManager.Service
{
	public enum UserLogged
	{
		Init, Logged, NotLogged, Error
	};
    public class LoginServiceEventArgs : EventArgs
    {
        public IUser User { get; set; }
        public LoginServiceEventArgs(IUser u) { User = u; }
    }
    public class LoginService
    {
        private UrlService url = DependencyService.Get<UrlService>();
        private WebService web = DependencyService.Get<WebService>();
        private EntityManager em = DependencyService.Get<EntityManager>();
        private DbService db = DependencyService.Get<DbService>();
        private string sid = "";
        private IUser user = null;
		public UserLogged logged { get; protected set; } = UserLogged.Init;

		private IUser User
		{
			get { 
				return user;
			}
			set {
				user = value;
			}
		}

        public event EventHandler<LoginServiceEventArgs> loginUpdate;

		System.Threading.SemaphoreSlim loginSemaphone = new System.Threading.SemaphoreSlim(1, 1);
		async private Task<T> getLoginInfo<T>() where T :  IUser
        {
			await loginSemaphone.WaitAsync();
            string ss = db.getVariable("usersid");
            if (ss != null)
            {
                sid = ss;
            }



            url.Sid = sid; // for logged request
            JObject response = null;
			try
			{
                string urls = url.makeApi("login", "getLoginInfo");

                response = await web.getAsync(urls);
				RepositoryService repo = em.getRepository();
				T userTmp = repo.entityFromJson<T>(response["user"]);
				User = userTmp;

				logged = User == null ? UserLogged.NotLogged : UserLogged.Logged;
				loginUpdate?.Invoke(this, new LoginServiceEventArgs(User));

				User.Sid = sid;
				db.setVariable("usersid", sid);
				return (T)user;
			}
			catch (Exception e)
			{
				if (e is WebServiceBadResultException)
				{
					logged = UserLogged.Error;
					return (T)user;
				}
				if (e is WebServiceFalseResultException && ((WebServiceFalseResultException)e).ErrorMsg == "NOT_LOGGED")
				{
					if (logged == UserLogged.Logged)
					{
						// change state
						logged = UserLogged.NotLogged;
						user = null;
						loginUpdate?.Invoke(this, new LoginServiceEventArgs(null));
					}
					logged = UserLogged.NotLogged; // attention quand on démarre l'app 
					return default(T);
				}
				else
				{
					throw new Exception("LoginService::getLoginInfo : not catched exception " + e.Message);
				}
			}
			finally
			{
				loginSemaphone.Release();
			} 
        }

        public async Task<T> loginFacebook<T>(Account account) where T : IUser
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["token"] = account.Properties["access_token"];
            var request = await web.getAsync(url.makeApi("user", "registerFacebook", null, args));
            var sid = request.Value<string>("sid");
            db.setVariable("usersid", sid);
            return await getLoginInfo<T>();
        }

        async public Task<bool> logout<T>() where T : IUser
        {
            db.setVariable("usersid", "");
            url.Sid = "";
			try
			{
				await web.getAsync(url.makeApi("login", "logout"));
			}
			catch (WebServiceFalseResultException e)
			{
				if (e.ErrorMsg != "NOT_LOGGED")
				{
					throw e;
				}
			}
			user = null;
			await getLoginInfo<T>();
            return true;
        }

        async public Task<bool> login<T>(string mail, string password, bool stay) where T : IUser
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            values["mail"] = mail;
            values["password"] = password;
            values["stay"] = stay ? "true" : "false";
            JObject response = null;
            try
            {
                response = await web.postAsync(url.makeApi("login", "login"), values);

            }
            catch (WebServiceFalseResultException e)
            {
                if (e.ErrorMsg == "BAD_PASSWORD")
                {
                    return false;
                }
                throw e;
            } 
            if (response == null || response["success"] == null)
            {
                return false;
            }
            if (response.Value<bool>("success") == false)
            {
                return false;
            }
            sid = response.Value<string>("sessionid");
            url.Sid = sid;
            db.setVariable("usersid", sid);
            await getLoginInfo<T>();
            return true;
        }

		public void onUpdateUser()
		{
			loginUpdate?.Invoke(this,new LoginServiceEventArgs(User));
		}

		async public Task<T> getUser<T>(bool refresh = false) where T : IUser
        {
			
			if (logged == UserLogged.NotLogged)
			{
				return default(T);
			}

			if (User == null || refresh)
            {
                User = await getLoginInfo<T>();
            }
            return (T)User;
        }

    }
}
