using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\www\test1";
            string name = "test4";
//            IISWorker.CreateWebSite(name, path, "*:80", true)
            IISWorker.CreateVirtualDir(name, path, true);
            Console.WriteLine("done");
            Console.ReadKey();
        }
    }
}
