using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Xamarin.Forms;

namespace XamEntityManager.Service
{

    public class DiService
    {
		private IDictionary<Type, dynamic> container = new Dictionary<Type, dynamic>();

        public string Servername
        {
            get
            {
                UrlService u = get(typeof(UrlService));
                return u.Servername;
            }
            set
            {
                UrlService u = get(typeof(UrlService));
                u.Servername = value;
            }
        }


        public DiService(string servername, Type userType)
        {
            Servername = servername;
            container[typeof(DiService)] = this;
            LoginService login = get(typeof(LoginService));
            // Define the user type
            login.UserType = userType;

            // Init SQLite
            DbService dbservice = get(typeof(DbService));
            RepositoryService repo = get(typeof(RepositoryService));
            dbservice.init();


        }

        public dynamic createPage(Type name)
        {
            return createInstance(name);
        }



        private dynamic createInstance(Type obj)
        {
            // auto detect if it is an internal service
			/*Type obj = Type.GetType("XamEntityManager.Service." + name);
            if (obj == null)
            {
                // else looking for the right name
                obj = Type.GetType(name);
            }*/
            
            FieldInfo o = obj.GetRuntimeField("inject");
            object[] objects = null;
            if (o != null)
            {
                Type[] servs = (Type[])o.GetValue(null);
                objects = new object[servs.Length];
                // get service
                for (int i = 0; i < servs.Length; i++)
                {
                    objects[i] = get(servs[i]);
                }

            }
            try
            {
                // iOS : in case of exception while trying to instanciate a valid constructor or Type.getType, it could
                // be an issue due to the linker which remove all unnecessary element, go to iOS settings, iOS build and set the 
                // linker to Only SDK
                return (dynamic)Activator.CreateInstance(obj, objects);
            }
            catch (TargetInvocationException e)
            {
                throw new Exception("The contructor of " + obj.Name + " is unresolvable, maybe there the contructor is private or doesn't have the good args , error : " + e.InnerException.Message );
            }
        }
        public dynamic get(Type name)
        {
            if (!container.ContainsKey(name))
            {
                container[name] = createInstance(name);
            }
            return container[name];
        }
    }
}
