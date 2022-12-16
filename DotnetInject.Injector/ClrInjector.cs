using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace DotnetInject.Injector
{
    public class ClrInjector : IClrInjector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            UIntPtr dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            CREATE_THREAD_FLAGS dwCreationFlags,
            out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetSystemWow64Directory(StringBuilder lpBuffer, uint uSize);

        [Flags]
        public enum CREATE_THREAD_FLAGS
        {
            RUN_IMMEDIATELY = 0,
            CREATE_SUSPENDED = 4,
            STACK_SIZE_PARAM_IS_A_RESERVATION = 65536, // 0x00010000
        }

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
            bw.WriteCString(Path.GetFullPath(pathToInjectingAssembly));
            bw.WriteCString(entryPointAssemblyQualifiedName);
            bw.WriteCString(entryPointArgs);

            using var nativeInjector = new Reloaded.Injector.Injector(process);

#if DEBUG
            Console.WriteLine("WAiting for input to proceed with injection");
            Console.ReadLine();
#endif

            var injectresult = nativeInjector.Inject(Path.GetFullPath("DotnetInject.Native.dll"));

            stream.Position = 10235;
            var br = new BinaryReader(stream);

            var address = br.ReadInt32();
            var signalByte = br.ReadByte();

            if (signalByte != 255)
                throw new Exception("Signal byte that was supposed to be set by native payload wasn't set");
            
            var newThread = CreateRemoteThread(process.Handle, IntPtr.Zero, UIntPtr.Zero, (nint)address, IntPtr.Zero, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out uint _);
            //Console.WriteLine("WAiting for input to proceed with injection");
            //Console.ReadLine();
            WaitForSingleObject(newThread, uint.MaxValue);


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