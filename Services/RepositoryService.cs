using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamEntityManager.Entity;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Xamarin.Forms.Internals;

namespace XamEntityManager.Service
{

    public class RepositoryService
    {
        private IDictionary<Type, IDictionary<int, IEntity>> entities = new Dictionary<Type, IDictionary<int, IEntity>>();
        public static Type[] inject = {
            typeof(WebService),
            typeof(UrlService),
            typeof(DbService)
        };
        WebService web;
        UrlService url;
        DbService db;
        List<Request> requestStack = new List<Request>();
		System.Threading.SemaphoreSlim RequestStackMutex = new System.Threading.SemaphoreSlim(1,1);
		[Preserve]
        public RepositoryService(WebService w, UrlService u, DbService d)
        {
            web = w;
            url = u;
            db = d;
        }

        public void clearCache()
        {
            entities.Clear();
        }





        private void pushIntoCache(Type type, IEntity obj)
        {
            if (!entities.Keys.Contains(type))
            {
                entities[type] = new Dictionary<int, IEntity>();
            }
            var test = entities[type];
            if (entities[type].Keys.Contains(obj.getId()))
            {
                return;
            }


            entities[type][obj.getId()] = obj;
        }
        private IEntity getFromCache(Type type, int id)
        {
            if (!entities.Keys.Contains(type) || !entities[type].Keys.Contains(id))
            {
                return null;
                //return getFromDb(name, id);
            }
            return entities[type][id];
        }

        /// <summary>
        /// Prepare requests for a multiple sending
        /// </summary>
        /// <param name="requests"></param>
        /// <returns></returns>
        private List<Dictionary<string, dynamic>> PrepareRequests(List<Request> requests)
        {
            List<Dictionary<string, dynamic>> ret = new List<Dictionary<string, dynamic>>();
            foreach (var request in requests)
            {
                Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
                values["id"] = request.Id;
                values["method"] = request.Method;
                values["controller"] = request.Controller;
                values["requestid"] = request.Requestid;
                values["params"] = request.Args;
                ret.Add(values);
            }
            return ret;
        }

        async private Task runRequest()
        {
			var arguments = PrepareRequests(requestStack);
			var urlStr = url.makeApi("Multiple", "index");
			JObject results = null;
			await RequestStackMutex.WaitAsync();
			List<Request> requestExecuting = new List<Request>();
			try
			{
				requestExecuting = requestStack;
				requestStack = new List<Request>(); // reset stack
				results = await web.postAsync(urlStr, arguments);
			}
			catch (Exception e)
			{
				foreach (var request in requestExecuting)
				{
					request.OnError(e);
				}
				requesting = false;
			}
			finally
			{
				RequestStackMutex.Release();
				requesting = false;
			}

			// in case of error
			if (results == null)
			{
				return;
			}
            foreach (JObject result in results["Multiple"].ToArray())
            {
                int requestid = result.Value<int>("requestid");
                var request = requestExecuting.Find(x => x.Requestid == requestid);
                if (request == null) { throw new Exception("Bad request id : " + requestid); }
                request.onFinishAsync(result);
            }
        }

        private bool requesting = false; // to know if the timer is started or not
		const bool combineRequest = false;
		/// <summary>
		/// Add a request to the stack to be executed
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>

		async private Task<JObject> addRequest(Request request, bool combine = combineRequest)
        {
			//Debug.WriteLine("Add Request : " + request.Requestid + " : " + request.Controller + ":" + request.Method);
			if (!combine)
			{
                string url = this.url.makeApi(request.Controller, request.Method, request.Id.ToString(), request.Args);
				var result = await web.postAsync(url, request.Args);
				return result;
			}
			await RequestStackMutex.WaitAsync();
			try
			{
				requestStack.Add(request);
			}
			finally
			{
				RequestStackMutex.Release();
			}
            
            if (!requesting)
            {
                requesting = true;
                await Task.Delay(100);
                runRequest();
            }
            var r = await request.finishAsyn();
			//Debug.WriteLine("Request done : " + request.Requestid);
			return r;
        }

        async public Task<IEntity> findById<T>(int id, bool force = false)
        {
            return await findById(typeof(T), id, force);
        }

        async public Task<IEntity> findById(Type type, int id, bool force = false)
        {
            IEntity obj;
            if (!force)
            {
                obj = getFromCache(type, id);
                if (obj != null)
                {
                    return obj;
                }
            }
            JToken json = await findByIdJson(type, id);
            // not found
            if (json == null)
            {
                return null;
            }
            obj = entityFromJson(type, json);
            return obj;
        }



        /// <summary>
        /// same as findbyid but return only json, for refreshing model
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        async public Task<JToken> findByIdJson(Type type, int id)
        {
            Request request = new Request("Entity", type.Name, id, null, type);
            JObject response = await addRequest(request, true);
            if (response == null) { return null; }

            return response[type];
        }

        public IEntity entityFromJson(Type type, JToken json)
        {
            if (json == null)
            {
                throw new Exception("RepositoryService::entityFromJson json is null");
            }
            IEntity obj;
            try
            {
                obj = (dynamic)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new Exception("The entity " + type.Name + " is not found, or it misses the controller(RepositoryService) : " + e.Message);
            }

            obj.updateEntityFromJson(this, json);
            // push into cache
            pushIntoCache(type, obj);
            pushIntoDb(type, obj);
            return obj;
        }

 

        private void pushIntoDb(Type name, IEntity obj)
        {
            //db.saveEntity(obj);
        }

        async public Task<List<T>> findAll<T>() where T : IEntity
        {
            var type = typeof(T);
            Request request = new Request("Entity", type.Name, 0, null, type);
            JObject response = await addRequest(request);
            if (response == null) { return null; }
            JArray array = (JArray)response[type.Name + "s"];
            var list = new List<IEntity>();
            foreach (JToken obj in array)
            {
                list.Add(entityFromJson(type, obj));
            }
            return list as List<T>;
        }
		/// <summary>
		/// JTs the oken2 list.
		/// </summary>
		/// <returns>The oken2 list.</returns>
		/// <param name="source">Source.</param>
		public List<JToken> JToken2List(JToken source)
		{
			JTokenType type = source.Type;
			List<JToken> tokenList = new List<JToken>();
			// Case array and case object
			if (type == JTokenType.Object)
			{
				JObject obj = (JObject)source;
				foreach (KeyValuePair<string, JToken> item in obj)
				{
					tokenList.Add(item.Value);
				}
			}
			else if (type == JTokenType.Array)
			{
				JArray array = (JArray)source;
				foreach (var item in array)
				{
					tokenList.Add(item);
				}
			}
			else
			{
				throw new Exception("RepositoryService::findSome : Not handle type ");
			}
			return tokenList;
		}

        public async Task<List<T>> findSome<T>(string method, int id, IDictionary<string, dynamic> args) where T : IEntity
        {
            var type = typeof(T);
            Request request = new Request(type.Name, method, id, args, type);
            //JObject response = await addRequest(request);
			string u = this.url.makeApi(type.Name, method, id.ToString(), args);
			var response = await web.postAsync(u, args);
            // check existance of index
            if (response == null || response[type.Name + "s"] == null)
            {
                throw new Exception("The response must contain " + type.Name + "s");
            }

            List<T> list = new List<T>();
			List<JToken> tokenList = JToken2List(response[type.Name + "s"]);
            
			foreach (var item in tokenList)
			{
				int itemId = item.Value<int>("id");
				IEntity fromCache = getFromCache(type, itemId);
				if (fromCache == null)
				{
					// create from json
					list.Add((T)entityFromJson(type, item));
				}
				else
				{
					// update from json
					fromCache.updateEntityFromJson(this, item);
					list.Add((T)fromCache);
				}

			}



            return list;
        }

    }
	public class TimeoutRepositoryException : Exception { }

    class Request
    {

        static int requestglobal = 1;
        string controller;
        string method;
        int id;
        int requestid = requestglobal++;
        Type entity;
        IDictionary<string, dynamic> args;
		private TaskCompletionSource<JObject> done = new TaskCompletionSource<JObject>();
					 public DateTime Created { get; } = new DateTime();
        // CAUTION RACE CONDITION HERE !!
        public void onFinishAsync(JObject e)
        {
            done.SetResult(e);
        }

		public void OnError(Exception e)
		{
			done.SetException(new List<Exception>() { e});
		}
        public async Task<JObject> finishAsyn()
        {
            return await done.Task;
        }
    
        public string Controller
        {
            get
            {
                return controller;
            }

            set
            {
                controller = value;
            }
        }

        public string Method
        {
            get
            {
                return method;
            }

            set
            {
                method = value;
            }
        }

        public int Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        public Type Entity
        {
            get
            {
                return entity;
            }

            set
            {
                entity = value;
            }
        }

        public IDictionary<string, dynamic> Args
        {
            get
            {
                return args;
            }

            set
            {
                args = value;
            }
        }

        public int Requestid
        {
            get
            {
                return requestid;
            }

            set
            {
                requestid = value;
            }
        }

        public Request(
            string controller,
            string method,
            int id,
            IDictionary<string, dynamic> args,
            Type entity
        )
        {
            this.Controller = controller;
            this.Method = method;
            this.Id = id;
            this.Entity = entity;
            this.args = args;
        }


    }
}
