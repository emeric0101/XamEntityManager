using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace XamEntityManager.db
{
    [Preserve(AllMembers = true)]
    public class DbCachedVariable
    {
        string value;
        string field;
        TimeSpan duration;
        DateTime registered;

        public bool IsObsolete
        {
            get
            {
                TimeSpan d = registered - DateTime.Now;
                if (d > duration)
                {
                    return true;
                }
                return false;
            }
        }

        public DbCachedVariable(string field, string value, TimeSpan duration)
        {
            this.value = value;
            this.field = field;
            registered = DateTime.Now;
            this.duration = duration;
        }

        public DbCachedVariable()
        {

        }

        public string Value
        {
            get
            {
                return value;
            }

            set
            {
                this.value = value;
            }
        }

        [PrimaryKey]
        public string Field
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
            }
        }


    }
}
