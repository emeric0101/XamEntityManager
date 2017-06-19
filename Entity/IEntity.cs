using XamEntityManager.Service;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XamEntityManager.Entity
{
    public interface IEntity
    {
        Dictionary<string, dynamic> serialize();

        int getId();
        void updateEntityFromJson(RepositoryService repo, JToken json);
        /// <summary>
        /// Return all entities referenced inside the object
        /// </summary>
        /// <returns></returns>
        List<IEntity> getInternalEntities();
        string getName();
        Task<InnerClassInfo> getInnerClassInfo(RepositoryService repo, string field, bool force = false);
        Task<List<InnerClassInfo>> getInnerClassInfos(RepositoryService repo, string field, bool force = false);
		void updateEntityFromEntity(IEntity newEntity);
        void setRepo(RepositoryService r);
        RepositoryService getRepo();
        Task refresh();
        bool Dirty { get; }
    }
}
