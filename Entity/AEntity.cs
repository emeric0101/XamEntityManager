using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using XamEntityManager.Service;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.ComponentModel;
using Xamarin.Forms.Internals;

namespace XamEntityManager.Entity
{
    public class InnerClassInfo
    {
        public string entity;
        public int id;
        public InnerClassInfo(string e, int i)
        {
            id = i;
            entity = e;
        }
    }



    [Preserve(AllMembers = true)]
    public abstract class AEntity : IEntity, INotifyPropertyChanged
    {
        public bool Dirty { get; set; } = true;

        RepositoryService repo = null;
        public void setRepo(RepositoryService r)
        {
            repo = r;
        }
        public RepositoryService getRepo() { return this.repo; }
        

        public AEntity()
        {
        }

        public override bool Equals(object obj)
        {
            AEntity user1 = obj as AEntity;
            if (user1 == null) { return false; }
            return user1.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        protected int id = 0;

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

  

        public int getId()
        {
            return Id;
        }

        public override string ToString()
        {
            return id.ToString();
        }

        /// <summary>
        /// Return the list of all internal entities
        /// </summary>
        /// <returns></returns>
        public List<IEntity> getInternalEntities()
        {
            List<IEntity> internalEntites = new List<IEntity>();
            var properties = GetType().GetRuntimeFields();
            foreach (FieldInfo property in properties)
            {
                var value = property.GetValue(this);
                if (value is IEntity)
                {
                    internalEntites.Add((IEntity)value);
                }
            }
            return internalEntites;
        }

        public Dictionary<string, dynamic> serialize()
        { 
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
            var properties = GetType().GetRuntimeFields();
            foreach (FieldInfo property in properties)
            {
                // Using to string to convert ENtity to "id"
                object obj = property.GetValue(this);
				string objType = property.FieldType.ToString();
                if (obj != null)
                {
					if (objType == "System.DateTime")
					{
						DateTime d = (DateTime)obj;
						values[property.Name] = d;
					}
					else
					{
						values[property.Name] = obj.ToString();
					}
                }
                else
                {
                    values[property.Name] = null;
                }
            }
            return values;
        }

        /// <summary>
        /// Return the json property according to prop.name 
        /// null if not found
        /// </summary>
        /// <param name="json"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        private JProperty getProperty(JToken json, FieldInfo prop)
        {
			if (!json.HasValues)
			{
				throw new Exception("AEntity::getProperty : json has no values, bad format ");
			}
            // repo is a service
            if (prop.Name == "repo"){return null; }

            string name = prop.Name.ToLower();
            return json.FirstOrDefault((x) =>
            {
                var property = x as JProperty;
                return property.Name.ToLower() == name;
            }) as JProperty;

        }
		System.Threading.SemaphoreSlim InnerClassInfosSemaphone = new System.Threading.SemaphoreSlim(1, 1);
		System.Threading.SemaphoreSlim InnerClassInfoSemaphone = new System.Threading.SemaphoreSlim(1, 1);
        IDictionary<string, InnerClassInfo> innerClassInfo = null;
        IDictionary<string, List<InnerClassInfo>> innerClassInfos = null;

        public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler((sender, arg) =>
        {
            (sender as AEntity).Dirty = true;
        });
        /// <summary>
        /// Notify that a property was changed on a objet
        /// </summary>
        /// <param name="property"></param>
        public void OnPropertyChanged(string property)
        {
            Dirty = true;
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }

        [Obsolete("Assign directly and use OnPropertyChanged")]
        public void setField(string name, dynamic value)
        {
			
            name = name.ToLower();
            Type t = GetType();
            var fields = t.GetRuntimeFields();
            // find the property
            var field = fields.Where((info) =>
            {
                return info.Name.ToLower() == name;
            });
            if (field.Count() != 1)
            {
                throw new Exception("No property found : " + name);
            }
            field.First().SetValue(this, value);

			name = char.ToUpper(name[0]) + name.Substring(1);
			OnPropertyChanged(name); // à tester !!
        }

        /// <summary>
        /// Get a property into the class (and check if exist)
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public async Task<InnerClassInfo> getInnerClassInfo(RepositoryService repo, string field, bool force = false)
        {
			await InnerClassInfoSemaphone.WaitAsync();
			var innerClassInfoLocal = innerClassInfo;
			InnerClassInfoSemaphone.Release();
			if (innerClassInfoLocal == null || force)
            {
                // we must download again the entity
                var newModel = await repo.findById(GetType(),id, true);
                var r = await newModel.getInnerClassInfo(repo, field);
                // maj du parent : 
                innerClassInfoLocal[field] = r;
            }
            if (!innerClassInfoLocal.Keys.Contains(field)) {
                return null;
            }
            return innerClassInfoLocal[field];
        }

        /// <summary>
        /// Get a property into the class (and check if exist)
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public async Task<List<InnerClassInfo>> getInnerClassInfos(RepositoryService repo, string field, bool force)
        {
			await InnerClassInfosSemaphone.WaitAsync();
			IDictionary<string, List<InnerClassInfo>> localInnerClassInfos = innerClassInfos;
			InnerClassInfosSemaphone.Release();
			if (localInnerClassInfos == null || force)
            {
                // we must download again the entity
                var newModel = await repo.findById(GetType(), id, true);
                var r = await newModel.getInnerClassInfos(repo, field);
                // maj du parent : 
                localInnerClassInfos[field] = r;
            }
            if (!localInnerClassInfos.Keys.Contains(field))
            {
                return new List<InnerClassInfo>();
            }

            return localInnerClassInfos[field];
        }
        protected async Task<T> foreignKey<T>(RepositoryService repo, string field) where T: IEntity
        {
            InnerClassInfo value = await getInnerClassInfo(repo, field);
            if (value == null)
            {
                return default(T);
            }
            T entity = (T)await repo.findById(typeof(T), value.id);
            return entity;
        }

        protected async Task<List<T>> foreignKeys<T>(RepositoryService repo, string field, bool force = false) where T : IEntity
        {
            List<InnerClassInfo> value = await getInnerClassInfos(repo, field, force);
            if (value == null)
            {
                return null;
            }
            List<T> entities = new List<T>();
			List<Task> entitiesFindTasks = new List<Task>();
			System.Threading.SemaphoreSlim entitiesSemaphore = new System.Threading.SemaphoreSlim(1,1);
            foreach (InnerClassInfo v in value)
            {
                if (v.entity != typeof(T).Name)
                {
                    // FATAL ERROR !!!
                    Environment.FailFast("foreignKeys : T type is different from server : " + v.entity + " vs " + typeof(T).Name);
                }
				entitiesFindTasks.Add(repo.findById(typeof(T), v.id, force).ContinueWith(async (arg) =>
				{
					var e = arg.Result;
					await entitiesSemaphore.WaitAsync();
					entities.Add((T)e);
					entitiesSemaphore.Release();
				}));
            }
			await Task.WhenAll(entitiesFindTasks);
            return entities;
        }
   

        private void parseProperty(RepositoryService repo, JProperty property, FieldInfo prop)
        {
            setRepo(repo);

            Type fieldType = prop.FieldType;
            string tgg = fieldType.ToString();
            JToken value = property.Value;
            if (value.Type == JTokenType.Null) { return; }

            // entities
            if (tgg == "System.DateTime")
            {
                try
                {
                    prop.SetValue(this, value["date"].Value<DateTime>());
                }
                catch (Exception)
                {
                    Debug.WriteLine("parseProperty : bad date");
                }
                
            }
            else if (value.Type == JTokenType.Array)
            {
	
				InnerClassInfosSemaphone.Wait();
				innerClassInfos[prop.Name] = new List<InnerClassInfo>();
				foreach (JToken arrayValue in value)
				{
					var lentityName = arrayValue["entity"].ToString();
					var lid = int.Parse(arrayValue["id"].ToString());
					innerClassInfos[prop.Name].Add(new InnerClassInfo(lentityName, lid));
				}
				InnerClassInfosSemaphone.Release();
            }
            // entity
            else if (value.HasValues)
            {
                var lentityName = value["entity"].ToString();
                var lid = int.Parse(value["id"].ToString());
                var innerClassInfot = new InnerClassInfo(lentityName, lid);
				InnerClassInfoSemaphone.Wait();
				innerClassInfo[prop.Name] = innerClassInfot;
				InnerClassInfoSemaphone.Release();
            }

            // Standard types
            switch (fieldType.ToString())
            {
                case "System.Int32":
                    prop.SetValue(this, value.Value<int>());
                    break;
                case "System.Double":
                    prop.SetValue(this, value.Value<double>());
                    break;
                case "System.String":
                    string test = value.Value<string>();
                    prop.SetValue(this, test);
                    break;
            }
        }

        public void updateEntityFromJson(RepositoryService repo, JToken jsonToken)
        {
            setRepo(repo);
            if (jsonToken == null)
            {
                throw new Exception("updateEntityFromJson : jsonToken null");
            }
			InnerClassInfosSemaphone.Wait();
            innerClassInfos = new Dictionary<string, List<InnerClassInfo>>();
			InnerClassInfosSemaphone.Release();
			InnerClassInfoSemaphone.Wait();
            innerClassInfo = new Dictionary<string, InnerClassInfo>();
			InnerClassInfoSemaphone.Release();
            Type t = GetType();
            var fields = t.GetRuntimeFields();
            foreach (FieldInfo prop in fields)
            {
				// If the property is nullable, we null it
				var value = prop.GetValue(this);
				if (value is System.Collections.IList || value is IEntity)
				{
					// null all foreign keys to force refresh
					prop.SetValue(this, null);
				}
                JProperty property = getProperty(jsonToken, prop);
                if (property == null) { continue; }

               parseProperty(repo, property, prop);
            }
            Dirty = false;
        }

		public void updateEntityFromEntity(IEntity newEntity)
		{
			InnerClassInfosSemaphone.Wait();
			innerClassInfos = new Dictionary<string, List<InnerClassInfo>>();
			InnerClassInfosSemaphone.Release();
			InnerClassInfoSemaphone.Wait();
			innerClassInfo = new Dictionary<string, InnerClassInfo>();
			InnerClassInfoSemaphone.Release();
			Type t = GetType();
			var fields = t.GetRuntimeFields();
			foreach (FieldInfo prop in fields)
			{
				prop.SetValue(this, prop.GetValue(newEntity));
			}
			Dirty = false;
		}

        public abstract string getName();



        public async Task refresh()
        {
            var json = await repo.findByIdJson(GetType(), Id);
            updateEntityFromJson(repo, json);
        }
     }
}
