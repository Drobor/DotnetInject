#include <wchar.h>

#include "ClrStarter.h"

void* _pBuf;
HANDLE _hMapFile;

__declspec(dllexport) int __stdcall LoadClrFromMemoryMappedFile(LPVOID lpParam)
{
    LoadRuntimePacked((wchar_t*)_pBuf);

    ((char*)_pBuf)[0] = 0;

    UnmapViewOfFile(_pBuf);
    CloseHandle(_hMapFile);
}

void LoadMemoryMappedFile()
{
    wchar_t processId[32] = L"";

    swprintf_s(processId, 32, L"%d", GetCurrentProcessId());

    wchar_t memoryMappedFileName[64] = L"DotnetInjectBootstrapperParameters-";
    wcscat_s(memoryMappedFileName, 64, processId);

    _hMapFile = OpenFileMappingW(
        FILE_MAP_ALL_ACCESS, // read/write access
        FALSE, // do not inherit the name
        memoryMappedFileName); // name of mapping object

    if (_hMapFile == NULL)
        return;

    _pBuf = MapViewOfFile(_hMapFile, // handle to map object
                          FILE_MAP_ALL_ACCESS, // read/write permission
                          0,
                          0,
                          10240);

    if (_pBuf == NULL)
    {
        CloseHandle(_hMapFile);
        return;
    }

    *(((unsigned char*)_pBuf) + 10239) = 255;

    int* functionAddress = (int*)(((char*)_pBuf) + 10235);
    *functionAddress = (int)&LoadClrFromMemoryMappedFile;
}

int APIENTRY DllMain(HMODULE hModule,
                     DWORD ul_reason_for_call,
                     LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        LoadMemoryMappedFile();
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
