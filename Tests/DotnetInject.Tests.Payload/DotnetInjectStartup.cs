using System;
using System.IO;

namespace DotnetInject.Tests.Payload
{
    public class DotnetInjectStartup
    {
        public static unsafe int Init(IntPtr argument, int argSize)
        {
            try
            {
                File.WriteAllText(@"c:\Downloads\InjectLog.txt", $"InjectLog {DateTime.Now}");
            }
            finally
            {
                Console.WriteLine("Inject Completed");
            }

            return 0;
        }
    }
}
