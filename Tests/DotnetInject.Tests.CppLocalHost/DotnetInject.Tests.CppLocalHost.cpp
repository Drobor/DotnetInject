// DotnetInject.Tests.CppLocalHost.cpp : This file contains the 'main' function. Program execution begins and ends there.
//
//#include "DotnetNativeInject.h"
//#include "../../DotnetInject.Native/ClrStarter.h"
//#include "manualHeader.h"

#include <iostream>
#include <windows.h>

using namespace std;

void (__cdecl *LoadRuntimePacked)(wchar_t*);

//void (*fun_ptr)(int)

void CopyPackString(const wchar_t* src, wchar_t*& dest)
{
    for (; *src != 0; src++)
    {
        *dest = *src;
        dest++;
    }

    *dest = 0;
    dest++;
}

void LoadDotnetInjectNative()
{
    auto dllHandle = LoadLibraryA("DotnetInject.Native.dll");
    LoadRuntimePacked = (void (*)(wchar_t*))GetProcAddress(dllHandle, "LoadRuntimePacked");
}

int main()
{    
    std::cout << "Hello World!\n";
    int a;

    LoadDotnetInjectNative();

    //cin >> a;
    //auto hostFxrPath = LR"(c:\publish\hostfxr.dll)";
    //auto hostFxrPath = LR"(c:\Program Files (x86)\dotnet\host\fxr\5.0.12\hostfxr.dll)";
    auto hostFxrPath = L"c:\\priv\\Projects\\DotnetInject\\Tests\\DotnetInject.Tests.Payload\\bin\\Release\\net5.0\\win7-x86\\publish\\hostfxr.dll";
    auto runtimeConfigPath = LR"(c:\publish\ConsoleApp8.runtimeconfig.json)";
    //auto runtimeConfigPath = L"c:\\priv\\Projects\\DotnetInject\\Tests\\DotnetInject.Tests.Payload\\bin\\Release\\net5.0\\win7-x86\\publish\\DotnetInject.Tests.Payload.runtimeconfig.json";
    //auto assemblyPath = LR"(c:\publish\ConsoleApp81.dll)";
    auto assemblyPath = LR"(c:\priv\Projects\DotnetInject\Tests\DotnetInject.Tests.Payload\bin\Release\net5.0\win7-x86\publish\DotnetInject.Tests.Payload.dll)";
    auto initArgs = L"";

    wchar_t packBuffer[10240];
    wchar_t* currentBufferPtr = packBuffer;

    CopyPackString(hostFxrPath, currentBufferPtr);
    CopyPackString(runtimeConfigPath, currentBufferPtr);
    CopyPackString(assemblyPath, currentBufferPtr);
    CopyPackString(initArgs, currentBufferPtr);

    //LoadRuntime((wchar_t*)hostFxrPath, (wchar_t*)runtimeConfigPath, (wchar_t*)assemblyPath, (wchar_t*)initArgs);
    LoadRuntimePacked(packBuffer);

    cin >> a;
}
