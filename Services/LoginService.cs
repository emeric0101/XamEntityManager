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
        public static Type[] inject ={
            typeof(UrlService),
            typeof(WebService),
            typeof(EntityManager),
            typeof(DbService)
        };

        private UrlService url;
        private WebService web;
        private EntityManager em;
        private DbService db;
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
		async private Task<IUser> getLoginInfo()
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
				response = await web.getAsync(url.makeApi("login", "getLoginInfo"));
				RepositoryService repo = em.getRepository();
				var userTmp = repo.entityFromJson(UserType, response["user"]) as IUser;
				User = userTmp;

				logged = User == null ? UserLogged.NotLogged : UserLogged.Logged;
				loginUpdate?.Invoke(this, new LoginServiceEventArgs(User));

				User.Sid = sid;
				db.setVariable("usersid", sid);
				return user;
			}
			catch (Exception e)
			{
				if (e is WebServiceBadResultException)
				{
					logged = UserLogged.Error;
					return user;
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
					return null;
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

        public async Task<IUser> loginFacebook(Account account) 
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["token"] = account.Properties["access_token"];
            var request = await web.getAsync(url.makeApi("user", "registerFacebook", null, args));
            var sid = request.Value<string>("sid");
            db.setVariable("usersid", sid);
            return await this.getLoginInfo();
        }

 

 
		[Preserve]
        public LoginService(UrlService u, WebService w, EntityManager e, DbService d)
        {
            url = u;
            web = w;
            em = e;
            db = d; 
        }

        public Type UserType { get; internal set; }

        async public Task<bool> logout()
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
			await getLoginInfo();
            return true;
        }

        async public Task<bool> login<T>(string mail, string password, bool stay) where T : IUser, IEntity
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
            await getLoginInfo();
            return true;
        }

		public void onUpdateUser()
		{
			loginUpdate?.Invoke(this,new LoginServiceEventArgs(User));
		}

		async public Task<IUser> getUser(bool refresh = false) 
        {
			
			if (logged == UserLogged.NotLogged)
			{
				return null;
			}

			if (User == null || refresh)
            {
                User = await getLoginInfo();
            }
            return User;
        }

    }
}
