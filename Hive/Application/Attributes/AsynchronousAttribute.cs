using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hive.Application.Attributes
{
    [AttributeUsage(System.AttributeTargets.Method)]
    public class AsynchronousAttribute : Attribute
    {
        public AsynchronousAttribute()
        {

        }
    }
}
