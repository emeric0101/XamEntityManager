using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XamEntityManager.Entity
{
    public interface IUser : IEntity
    {
        string Sid { get; set; }
    }
}
