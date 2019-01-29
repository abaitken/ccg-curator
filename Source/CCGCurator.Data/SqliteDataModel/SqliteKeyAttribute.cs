using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCGCurator.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqliteKeyAttribute : Attribute
    {
    }
}
