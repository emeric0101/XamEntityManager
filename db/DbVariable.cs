using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace XamEntityManager.db
{
	[PreserveAttribute(AllMembers=true)]
    public class DbVariable
    {
        string value;
        string field;

        public DbVariable(string field, string value)
        {
            this.value = value;
            this.field = field;
        }
        public DbVariable()
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
