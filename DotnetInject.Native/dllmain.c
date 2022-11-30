#include "ClrStarter.h"

void LoadFromMemoryMappedFile()
{
    HANDLE hMapFile;
    LPCTSTR pBuf;

    hMapFile = OpenFileMappingW(
        FILE_MAP_ALL_ACCESS, // read/write access
        FALSE, // do not inherit the name
        L"DotnetInjectBootstrapperParameters"); // name of mapping object

    if (hMapFile == NULL)
    {
        //_tprintf(TEXT("Could not open file mapping object (%d).\n"),
        //       GetLastError());
        return;
    }

    pBuf = MapViewOfFile(hMapFile, // handle to map object
                         FILE_MAP_ALL_ACCESS, // read/write permission
                         0,
                         0,
                         10240);

    if (pBuf == NULL)
    {
        auto err = GetLastError();
        //_tprintf(TEXT("Could not map view of file (%d).\n"),
        //      err);

        CloseHandle(hMapFile);

        return;
    }

    LoadRuntimePacked((wchar_t*)pBuf);

    //MessageBox(NULL, pBuf, TEXT("Process2"), MB_OK);

    UnmapViewOfFile(pBuf);

    CloseHandle(hMapFile);

    return;
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
