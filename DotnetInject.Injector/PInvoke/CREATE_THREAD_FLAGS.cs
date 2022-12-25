using System;

namespace DotnetInject.Injector.PInvoke;

[Flags]
internal enum CREATE_THREAD_FLAGS
{
    RUN_IMMEDIATELY = 0,
    CREATE_SUSPENDED = 4,
    STACK_SIZE_PARAM_IS_A_RESERVATION = 65536, // 0x00010000
}