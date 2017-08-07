using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace XamEntityManager.db
{
	[Preserve]
    public interface ISQLite
    {
        SQLiteConnection GetConnection(string dbname);
    }
}
