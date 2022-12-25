using System;

namespace DotnetInject.Injector.PInvoke;

[Flags]
internal enum ProcessCreationFlags : uint
{
    // No flags
    None = 0x00000000,
    // The new process is a console application
    CreateNewConsole = 0x00000010,
    // The new process is a detached process
    CreateDetachedProcess = 0x00000008,
    // The new process is a suspended process
    CreateSuspended = 0x00000004,
    // The new process inherits the error mode of the calling process
    InheritErrorMode = 0x04000000
}