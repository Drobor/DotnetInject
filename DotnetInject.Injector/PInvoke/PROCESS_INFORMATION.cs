using System;
using System.Runtime.InteropServices;

namespace DotnetInject.Injector.PInvoke;

[StructLayout(LayoutKind.Sequential)]
internal struct PROCESS_INFORMATION
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
}