﻿using System.Diagnostics;

namespace DotnetInject.Injector
{
    public interface IClrInjector
    {
        void Inject(Process process, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, string? pathToHostFxr = null, string? runtimeConfigPath = null);
        void Inject<T>(Process process, string pathToInjectingAssembly, string entryPointAssemblyQualifiedName, T entryPointArgs, string? pathToHostFxr = null, string? runtimeConfigPath = null);
    }
}