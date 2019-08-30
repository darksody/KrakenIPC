using Contracts;
using KrakenIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var pipeClient = new PipeClient<IMyService>();
            var proxy = pipeClient.GetProxy();

            string data = proxy.GetData(3); //returns "You entered: 3"
            Console.WriteLine(data);
            CompositeType composite = proxy.GetDataUsingDataContract(new CompositeType()); //composite.StringValue will be "Hello Suffix"
            Console.WriteLine(composite.StringValue);

            Console.ReadLine();
        }
    }
}
