using Contracts;
using KrakenIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var pipeServer = new PipeServer<MyService, IMyService>();
            Console.WriteLine("Server started...");
            Console.ReadLine();
        }
    }
}
