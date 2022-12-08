﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace DotnetInject.Injector
{
    public class ClrInjector : IClrInjector
    {
        public void Inject(Process process, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, string? pathToHostFxr = null, string? runtimeConfigPath = null)
            => InjectInternal(process, pathToInjectingAssembly, entryPointAssemblyQualifiedName, "", pathToHostFxr, runtimeConfigPath);

        public void Inject<T>(Process process, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, T entryPointArgs, string? pathToHostFxr = null, string? runtimeConfigPath = null)
            => InjectInternal(process, pathToInjectingAssembly, entryPointAssemblyQualifiedName, JsonSerializer.Serialize(entryPointArgs), pathToHostFxr, runtimeConfigPath);

        private void InjectInternal(
            Process process,
            string pathToInjectingAssembly,
            string entryPointAssemblyQualifiedName,
            string entryPointArgs,
            string? pathToHostFxr = null,
            string? runtimeConfigPath = null)
        {
            pathToHostFxr ??= Process
                .GetCurrentProcess()
                .Modules
                .Cast<ProcessModule>()
                .First(x => x.ModuleName!.Equals("hostfxr.dll", StringComparison.InvariantCultureIgnoreCase))
                .FileName!;

            using var memoryMappedFile = MemoryMappedFile.CreateNew(
                $"DotnetInjectBootstrapperParameters-{process.Id}",
                10240,
                MemoryMappedFileAccess.ReadWrite);

            using var stream = memoryMappedFile.CreateViewStream();

            var bw = new BinaryWriter(stream, Encoding.Unicode); //(wchar_t* hostFxrPath, wchar_t* runtime_config_path, wchar_t* assemblyDllPath, wchar_t* initArgs)
            bw.WriteCString(pathToHostFxr);
            bw.WriteCString(runtimeConfigPath);
            bw.WriteCString(pathToInjectingAssembly);
            bw.WriteCString(entryPointAssemblyQualifiedName);
            bw.WriteCString(entryPointArgs);

            using var nativeInjector = new Reloaded.Injector.Injector(process);

            Console.WriteLine("WAiting for input to proceed with injection");
            Console.ReadLine();

            var injectresult = nativeInjector.Inject(Path.GetFullPath("DotnetInject.Native.dll"));

            for (int i = 0; i < 1000; i++)
            {
                stream.Position = 0;

                if (stream.ReadByte() == 0)
                    return;

                Thread.Sleep(10);
            }

            throw new Exception("Failed to inject - nothing happened after 10s timeout");
        }
    }
}