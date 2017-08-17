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
        Dictionary<string, object> serialize();

        int getId();
        void updateEntityFromJson(RepositoryService repo, JToken json);
        /// <summary>
        /// Return all entities referenced inside the object
        /// </summary>
        /// <returns></returns>
        List<IEntity> getInternalEntities();
        string getName();
        Task<InnerClassInfo> getInnerClassInfo<T>(RepositoryService repo, string field, bool force = false) where T : IEntity;
        Task<List<InnerClassInfo>> getInnerClassInfos<T>(RepositoryService repo, string field, bool force = false) where T : IEntity;
        void updateEntityFromEntity(IEntity newEntity);
        void setRepo(RepositoryService r);
        RepositoryService getRepo();
        Task Refresh<T>() where T : IEntity;
        bool Dirty { get; }
    }
}
