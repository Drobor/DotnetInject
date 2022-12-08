using System;
using System.IO;

//using DotnetInject.Payload;

namespace DotnetInject.Tests.Payload
{
    public class DotnetInjectStartup
    {
        public static unsafe int Init(IntPtr argument, int argSize)
        {
            try
            {
                File.WriteAllText(@"c:\Downloads\InjectLog.txt", $"InjectLog {DateTime.Now}\nNetVersion{Environment.Version}");
            }
            finally
            {
                Console.WriteLine("Inject Completed");
            }
            
            

            return 0;
        }
    }
}
/*
namespace DotnetInject.Tests.Payload
{
    

    public class DotnetInjectTestStartupNwe : IDotnetInjectEntryPoint
    {
        public void Main()
        {
            try
            {
                File.WriteAllText(@"c:\Downloads\InjectLog.txt", $"Injected with reflection\nInjectLog {DateTime.Now}");
            }
            finally
            {
                Console.WriteLine("Inject Completed");
            }
        }
    }
}*/