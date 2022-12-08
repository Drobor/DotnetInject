using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetInject.Payload
{
    public class DotnetInjectEntryPoint
    {
        public static unsafe int Init(IntPtr argument, int argSize)
        {
            Console.WriteLine("Generic startup was called");

            var entryPointType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.ExportedTypes)
                .First(x => x.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IDotnetInjectEntryPoint<>) || x == typeof(IDotnetInjectEntryPoint)));

            var entryPoint = Activator.CreateInstance(entryPointType);

            if (entryPoint is IDotnetInjectEntryPoint parameterlessEntryPoint)
            {
                Task.Run(parameterlessEntryPoint.Main);
                return 0;
            }

            var entryPointArgument = JsonSerializer.Deserialize(new string((char*)argument.ToPointer()), entryPointType.GenericTypeArguments[0]);

            Task.Run(() => entryPointType.GetMethod("Main").Invoke(entryPoint, new[] { entryPointArgument }));

            return 0;
        }
    }
}