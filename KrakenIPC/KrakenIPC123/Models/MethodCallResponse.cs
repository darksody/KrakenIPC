using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenIPC.Models
{
    public class MethodCallResponse
    {
        public string MethodName { get; set; }
        public Type ReturnType { get; set; }
        public object ReturnValue { get; set; }
    }
}
