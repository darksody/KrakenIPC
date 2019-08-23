using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenIPC.Models
{
    internal class MethodCallRequest
    {
        public string MethodName { get; set; }
        public List<Type> ParameterTypes { get; set; }
        public List<object> ParameterValues { get; set; }
        public Type ReturnType { get; set; }
    }
}
