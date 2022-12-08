#include <wchar.h>

#include "ClrStarter.h"

void LoadFromMemoryMappedFile()
{
    wchar_t processId[32] = L"";

    swprintf_s(processId, 32, L"%d", GetCurrentProcessId());

    wchar_t memoryMappedFileName[64] = L"DotnetInjectBootstrapperParameters-";
    wcscat_s(memoryMappedFileName, 64, processId);

    HANDLE hMapFile = OpenFileMappingW(
        FILE_MAP_ALL_ACCESS, // read/write access
        FALSE, // do not inherit the name
        memoryMappedFileName); // name of mapping object

    if (hMapFile == NULL)
        return;

    void* pBuf = MapViewOfFile(hMapFile, // handle to map object
                               FILE_MAP_ALL_ACCESS, // read/write permission
                               0,
                               0,
                               10240);

    if (pBuf == NULL)
    {
        CloseHandle(hMapFile);
        return;
    }

    LoadRuntimePacked((wchar_t*)pBuf);

    *(char*)pBuf = 0;

    UnmapViewOfFile(pBuf);
    CloseHandle(hMapFile);
}

int APIENTRY DllMain(HMODULE hModule,
                     DWORD ul_reason_for_call,
                     LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        LoadFromMemoryMappedFile();
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
