#include <Windows.h>


#ifdef DOTNETINJECTNATIVE_EXPORTS
#define DOTNETINJECT_API __declspec(dllexport)
#else
#define DOTNETINJECT_API __declspec(dllimport)
#endif

#if defined(_WIN32)
#define HOSTFXR_CALLTYPE __cdecl
#define CORECLR_DELEGATE_CALLTYPE __stdcall
#else
#define HOSTFXR_CALLTYPE
#define CORECLR_DELEGATE_CALLTYPE
#endif

#pragma once
__declspec(dllexport) void __cdecl LoadRuntime(wchar_t* hostFxrPath, wchar_t* runtime_config_path, wchar_t* assemblyDllPath, wchar_t* entryPointAssemblyQualifiedName, wchar_t* initArgs);
__declspec(dllexport) void __cdecl LoadRuntimePacked(wchar_t* packedArgs);
