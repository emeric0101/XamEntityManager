using XamEntityManager.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xamarin.Forms;
[assembly: Xamarin.Forms.Dependency(typeof(XamEntityManager.Service.EntityManager))]

namespace XamEntityManager.Service
{
    public class EntityManagerEventArg : EventArgs
    {
        public List<IEntity> EntityUpdated { get; set; }
    }
    public class EntityManager
    {

        private RepositoryService repo = DependencyService.Get<RepositoryService>();
        private WebService web = DependencyService.Get<WebService>();
        private UrlService url = DependencyService.Get<UrlService>();
        private List<IEntity> persistObjs = new List<IEntity>();

		public event EventHandler<EntityManagerEventArg> entitiesUpdated = null;

        public List<IEntity> getPersistObjs() { return persistObjs; }
        public RepositoryService getRepository()
        {
            return repo;
        }

	
        /// <summary>
        /// Remove all persist objs
        /// </summary>
        public void clear()
        {
            persistObjs.Clear();
        }


        public void persist(IEntity obj, List<IEntity> exclude = null)
        {
            // Avoid infinite loop by parsing object which subobject ref to itself
            if (exclude != null)
            {
                foreach (IEntity entity in exclude)
                {
                    if (entity == obj)
                    {
                        return;
                    }
                }
            }
            else
            {
                exclude = new List<IEntity>();
            }

            // check existence
            foreach (IEntity entity in persistObjs)
            {
                if (entity == obj)
                {
                    return;
                }
            }
          /*  List<IEntity> internalEntities = obj.getInternalEntities();
            exclude.Add(obj);

            foreach (IEntity entity in internalEntities)
            {
                persist(entity, exclude);
            }*/
            persistObjs.Add(obj);
        }

        public async Task<bool> flush(bool autoclear = true)
        {
            // probleme ici probablement
            repo.clearCache();

            if (this.persistObjs.Count == 0) { return true; }

            var persistObjs = this.persistObjs.ToList<IEntity>();
            // Clear all persist obj
            if (autoclear)
            {
                clear();
            }

            foreach (IEntity entity in persistObjs)
            {
                // only on change !
                if (!entity.Dirty)
                {
                    continue;
                }
                bool result = await save(entity);
                if (!result)
                {
                    return false;
                }
            }
            var t = new EntityManagerEventArg();
            t.EntityUpdated = persistObjs;
            if (entitiesUpdated != null)
                entitiesUpdated.Invoke(this, t);
            return true;
        }


        public async Task<bool> save(IEntity entity)
        {
            var entityName = entity.getName();
            Dictionary<string, object> data = new Dictionary<string, object>();
            data[entityName] = entity.serialize();
			var curl = url.makeApi(entityName, "post", entity.getId().ToString());
			try
			{
				var response = await web.postAsync(curl, data);
				entity.updateEntityFromJson(repo, response[entityName]);
			}
			catch (Exception e)
			{
				if (e is WebServiceBadResultException)
				{
					System.Diagnostics.Debug.WriteLine("Entity save : " + e.Message);
				}
				if (e is WebServiceFalseResultException)
				{
					System.Diagnostics.Debug.WriteLine("Entity save false : " + ((WebServiceFalseResultException)e).ErrorMsg);
				}
				throw e;

			}
            return true;
        }
    }
}
