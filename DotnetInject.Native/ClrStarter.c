#include "ClrStarter.h"

struct hostfxr_initialize_parameters
{
    size_t Size;
    const wchar_t* HostPath;
    const wchar_t* DotnetRoot;
};

typedef void* hostfxr_handle;

typedef long (HOSTFXR_CALLTYPE* hostfxr_initialize_for_runtime_config_fn)(
    const wchar_t* runtimeConfigPath,
    const struct hostfxr_initialize_parameters* parameters,
    /*out*/ hostfxr_handle* hostContextHandle);

typedef long (HOSTFXR_CALLTYPE* hostfxr_initialize_for_dotnet_command_line_fn)(
    int argc,
    const wchar_t** argv,
    const struct hostfxr_initialize_parameters* parameters,
    /*out*/ hostfxr_handle* host_context_handle);

typedef long (HOSTFXR_CALLTYPE* hostfxr_get_runtime_delegate_fn)(
    const hostfxr_handle hostContextHandle,
    enum hostfxr_delegate_type type,
    /*out*/ void** delegate);

typedef long (HOSTFXR_CALLTYPE* hostfxr_close_fn)(const hostfxr_handle host_context_handle);

typedef int (CORECLR_DELEGATE_CALLTYPE* load_assembly_and_get_function_pointer_fn)(
    const wchar_t* assemblyPath,
    const wchar_t* typeName,
    const wchar_t* methodName,
    const wchar_t* delegateTypeName,
    void* reserved,
    /*out*/ void** delegate);

typedef int (CORECLR_DELEGATE_CALLTYPE* component_entry_point_fn)(void* arg, INT32 arg_size_in_bytes);

enum hostfxr_delegate_type
{
    hdt_com_activation,
    hdt_load_in_memory_assembly,
    hdt_winrt_activation,
    hdt_com_register,
    hdt_com_unregister,
    hdt_load_assembly_and_get_function_pointer,
    hdt_get_function_pointer,
};

hostfxr_get_runtime_delegate_fn _hostfxrGetRuntimeDelegate;
hostfxr_initialize_for_runtime_config_fn _hostfxrInitializeForRuntimeConfig;
hostfxr_close_fn _hostfxrClose;
load_assembly_and_get_function_pointer_fn _loadAssemblyAndGetFunctionPointer;
hostfxr_initialize_for_dotnet_command_line_fn _hostfxrInitializeForDotnetCommandLine;

boolean LoadHostFxrLibrary(wchar_t* hostFxrPath)
{
    void* lib = LoadLibraryW(hostFxrPath);

    _hostfxrInitializeForRuntimeConfig = (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
    _hostfxrInitializeForDotnetCommandLine = (hostfxr_initialize_for_dotnet_command_line_fn)GetProcAddress(lib, "hostfxr_initialize_for_dotnet_command_line");
    _hostfxrGetRuntimeDelegate = (hostfxr_get_runtime_delegate_fn)GetProcAddress(lib, "hostfxr_get_runtime_delegate");
    _hostfxrClose = (hostfxr_close_fn)GetProcAddress(lib, "hostfxr_close");

    return (_hostfxrInitializeForRuntimeConfig && _hostfxrGetRuntimeDelegate && _hostfxrClose && _hostfxrInitializeForDotnetCommandLine);
}

load_assembly_and_get_function_pointer_fn   StartClr(const wchar_t* runtimeconfigPath, const wchar_t* assemblyDllPath)
{
    // Load .NET Core
    hostfxr_handle context = NULL;

    //const int rc = _hostfxrInitializeForRuntimeConfig(runtimeconfigPath, NULL, &context);// doesn't work for selfcontained
    const int initResult = _hostfxrInitializeForDotnetCommandLine(1, &assemblyDllPath, NULL, &context);

    if (initResult != 0 || context == NULL)
    {
        _hostfxrClose(context);
        return NULL;
    }

    // Get the load assembly function pointer
    _hostfxrGetRuntimeDelegate(context, hdt_load_assembly_and_get_function_pointer, (void**)&_loadAssemblyAndGetFunctionPointer);
    _hostfxrClose(context);
    return (load_assembly_and_get_function_pointer_fn)_loadAssemblyAndGetFunctionPointer;
}

__declspec(dllexport) void LoadRuntime(wchar_t* hostFxrPath, wchar_t* runtimeConfigPath, wchar_t* assemblyDllPath, wchar_t* entryPointAssemblyQualifiedName, wchar_t* initArgs)
{
    LoadHostFxrLibrary(hostFxrPath);
    _loadAssemblyAndGetFunctionPointer = StartClr(runtimeConfigPath, assemblyDllPath);
    component_entry_point_fn initDelegate = NULL;

    void* res = _loadAssemblyAndGetFunctionPointer(
        assemblyDllPath,
        entryPointAssemblyQualifiedName,
        //L"DotnetInjectStartup, DotnetInject.Payload",
        L"Init",
        NULL,
        NULL,
        (void**)&initDelegate);

    initDelegate(initArgs, lstrlenW(initArgs));
}

__declspec(dllexport) void LoadRuntimePacked(wchar_t* packedArgs)
{
    wchar_t* args[5];

    wchar_t* argPtr = packedArgs;

    for (int i = 0; i < 5; i++)
    {
        args[i] = argPtr;
        argPtr += (lstrlenW(argPtr) + 1);
    }

    LoadRuntime(args[0], args[1], args[2], args[3], args[4]);
}
