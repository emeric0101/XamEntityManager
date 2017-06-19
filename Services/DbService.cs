using XamEntityManager.db;
using XamEntityManager.Entity;
using SQLite.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace XamEntityManager.Service
{
    public class NotFoundException : Exception { }
	public class DbService
	{

		private SQLiteConnection connection;
		/// <summary>
		/// Set a global variable in db
		/// </summary>
		/// <param name="field"></param>
		/// <param name="value"></param>
		public void setVariable(string field, string value)
		{
			connection.InsertOrReplace(new DbVariable(field, value));
			return;
		}
		/// <summary>
		/// Get a global var in db
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public string getVariable(string field)
		{
			DbVariable s = connection.Find<DbVariable>((item) => item.Field == field);
			if (s == null) { return null; }
			return s.Value;
		}




		public void init()
		{
            //connection.DropTable<DbVariable>();
            connection.CreateTable<DbVariable>();

		}

		[Preserve]
        public DbService()
        {
			ISQLite CSQLite = DependencyService.Get<ISQLite>();
            connection = CSQLite.GetConnection();
        }
    }
}
