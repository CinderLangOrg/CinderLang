using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendInterface
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple = false,Inherited = false)]
    public class BackendAttribute : Attribute
    {
        public string BackendName { get; set; }

        public BackendAttribute(string name) { BackendName = name; }
    }

}
