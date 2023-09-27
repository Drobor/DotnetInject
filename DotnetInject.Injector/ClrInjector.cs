using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using DotnetInject.Injector.PInvoke;

namespace DotnetInject.Injector
{
    public class ClrInjector : IClrInjector
    {
        public void Inject(Process process, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, string? pathToHostFxr = null, string? runtimeConfigPath = null)
            => InjectInternal(process, pathToInjectingAssembly, entryPointAssemblyQualifiedName, "", pathToHostFxr, runtimeConfigPath);

        public void Inject<T>(Process process, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, T entryPointArgs, string? pathToHostFxr = null, string? runtimeConfigPath = null)
            => InjectInternal(process, pathToInjectingAssembly, entryPointAssemblyQualifiedName, JsonSerializer.Serialize(entryPointArgs), pathToHostFxr, runtimeConfigPath);

        public Process InjectOnStart(ProcessStartInfo processStartInfo, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, string? pathToHostFxr = null, string? runtimeConfigPath = null)
        {
            var processInfo = CreateProcessSuspended(processStartInfo);
            var process = Process.GetProcessById(processInfo.dwProcessId);
            InjectInternal(process, pathToInjectingAssembly, entryPointAssemblyQualifiedName, "", pathToHostFxr, runtimeConfigPath);
            Kernel32.ResumeThread(processInfo.hThread);
            return process;
        }

        public Process InjectOnStart<T>(ProcessStartInfo processStartInfo, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, T entryPointArgs, string? pathToHostFxr = null, string? runtimeConfigPath = null)
        {
            var processInfo = CreateProcessSuspended(processStartInfo);
            var process = Process.GetProcessById(processInfo.dwProcessId);
            InjectInternal(process, pathToInjectingAssembly, entryPointAssemblyQualifiedName, JsonSerializer.Serialize(entryPointArgs), pathToHostFxr, runtimeConfigPath);
            Kernel32.ResumeThread(processInfo.hThread);
            return process;
        }

        private unsafe void InjectInternal(
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

            try
            {
                var _ = process.Modules;
            }
            catch
            {
                CreateDummyThread(process);
            }


            using var nativeInjector = new Reloaded.Injector.Injector(process);

#if DEBUG
            Console.WriteLine("WAiting for input to proceed with injection");
            Console.ReadLine();
#endif

            var injectresult = nativeInjector.Inject(Path.GetFullPath("DotnetInject.Native.dll"));

            stream.Position = 10231;
            var br = new BinaryReader(stream);

            nint address = sizeof(nint) == 8
                ? (nint)br.ReadInt64()
                : (nint)br.ReadInt32();

            var signalByte = br.ReadByte();

            if (signalByte != 255)
                throw new Exception("Signal byte that was supposed to be set by native payload wasn't set");

            var newThread = Kernel32.CreateRemoteThread(process.Handle, IntPtr.Zero, UIntPtr.Zero, (nint)address, IntPtr.Zero, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out uint _);
            //Console.WriteLine("WAiting for input to proceed with injection");
            //Console.ReadLine();
            Kernel32.WaitForSingleObject(newThread, uint.MaxValue);


            for (int i = 0; i < 1000; i++)
            {
                stream.Position = 0;

                if (stream.ReadByte() == 0)
                    return;

                Thread.Sleep(10);
            }

            throw new Exception("Failed to inject - nothing happened after 10s timeout");
        }

        private void CreateDummyThread(Process process)
        {
            var memorySize = 256;
            var memory = Enumerable.Repeat((byte)0xCC, memorySize).ToArray();
            memory[0] = 0xC2;
            memory[1] = 0x04;
            memory[2] = 0x00;

            var addr = Kernel32.VirtualAllocEx(process.Handle, IntPtr.Zero, (uint)memorySize, AllocationType.Commit, MemoryProtection.ReadWrite);
            Kernel32.WriteProcessMemory(process.Handle, addr, memory, memorySize, out var bytesWritten);
            Kernel32.VirtualProtectEx(process.Handle, addr, (nuint)memorySize, MemoryProtection.ExecuteRead, out var oldProtect);

            var newThread = Kernel32.CreateRemoteThread(process.Handle, IntPtr.Zero, UIntPtr.Zero, addr, IntPtr.Zero, CREATE_THREAD_FLAGS.RUN_IMMEDIATELY, out uint _);
            Kernel32.WaitForSingleObject(newThread, uint.MaxValue);
        }

        private PROCESS_INFORMATION CreateProcessSuspended(ProcessStartInfo psi)
        {
            if (psi.CreateNoWindow
                || psi.ErrorDialog
                || psi.RedirectStandardError
                || psi.RedirectStandardInput
                || psi.RedirectStandardOutput
                || psi.LoadUserProfile
                || psi.UseShellExecute
                || !string.IsNullOrEmpty(psi.Domain)
                || psi.Password != null
                || !string.IsNullOrEmpty(psi.UserName))
            {
                throw new NotSupportedException($"some of the properties of ProcessStartInfo were not supported");
            }

            STARTUPINFO si = default;

            Kernel32.CreateProcessW(
                psi.FileName,
                $"\"{Path.GetFileName(psi.FileName)}\" {psi.Arguments}",
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                ProcessCreationFlags.CreateSuspended,
                IntPtr.Zero,
                psi.WorkingDirectory,
                ref si,
                out var processInformation
            );

            return processInformation;
        }
    }
}