using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hive.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SynchronousAttribute : Attribute
    {
        public SynchronousAttribute()
        {

        }
    }
}
