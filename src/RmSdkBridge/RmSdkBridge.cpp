#include "RmSdkBridge.h"
#include <windows.h>
#include <string>
#include "IPlatform.h"
#include "IDeviceManager.h"
#include "ICPUEx.h"

namespace
{
    typedef IPlatform& (__stdcall* GetPlatformFunc)();

    HMODULE g_hPlatformDll = nullptr;
    IPlatform* g_pPlatform = nullptr;
    ICPUEx* g_pCpu = nullptr;
    bool g_initialized = false;
    DWORD g_lastError = 0;
    int g_lastStep = 0;
    // 0 = not started, 1 = registry read, 2 = LoadLibraryEx, 3 = GetProcAddress,
    // 4 = platform.Init, 5 = GetDevice, 6 = success

    bool GetRegistryInstallPath(std::wstring& outPath)
    {
        HKEY hKey = nullptr;
        if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"Software\\AMD\\RyzenMasterMonitoringSDK", 0, KEY_READ, &hKey) != ERROR_SUCCESS)
            return false;

        wchar_t buffer[MAX_PATH] = { 0 };
        DWORD size = sizeof(buffer);
        DWORD type = 0;
        LONG result = RegQueryValueEx(hKey, L"InstallationPath", nullptr, &type, reinterpret_cast<LPBYTE>(buffer), &size);
        RegCloseKey(hKey);

        if (result != ERROR_SUCCESS) return false;
        outPath = buffer;
        return true;
    }
}

extern "C" RMSDK_API bool RmSdk_Init()
{
    if (g_initialized) return true;
    g_lastStep = 0;
    g_lastError = 0;

    std::wstring installPath;
    if (!GetRegistryInstallPath(installPath)) { g_lastStep = 1; g_lastError = GetLastError(); return false; }

    if (!installPath.empty() && installPath.back() != L'\\' && installPath.back() != L'/')
        installPath += L'\\';

    std::wstring platformDllPath = installPath + L"bin\\Platform.dll";

    g_hPlatformDll = LoadLibraryEx(
        platformDllPath.c_str(),
        nullptr,
        LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS
    );
    if (!g_hPlatformDll) { g_lastStep = 2; g_lastError = GetLastError(); return false; }

    auto getPlatform = reinterpret_cast<GetPlatformFunc>(GetProcAddress(g_hPlatformDll, "GetPlatform"));
    if (!getPlatform) { g_lastStep = 3; FreeLibrary(g_hPlatformDll); g_hPlatformDll = nullptr; return false; }

    IPlatform& platform = getPlatform();
    if (!platform.Init()) { g_lastStep = 4; FreeLibrary(g_hPlatformDll); g_hPlatformDll = nullptr; return false; }

    g_pPlatform = &platform;
    IDeviceManager& deviceManager = g_pPlatform->GetIDeviceManager();
    g_pCpu = reinterpret_cast<ICPUEx*>(deviceManager.GetDevice(dtCPU, 0));

    if (!g_pCpu) { g_lastStep = 5; g_pPlatform->UnInit(); FreeLibrary(g_hPlatformDll); g_hPlatformDll = nullptr; g_pPlatform = nullptr; return false; }

    g_lastStep = 6;
    g_initialized = true;
    return true;
}

extern "C" RMSDK_API int RmSdk_GetLastStep()
{
    return g_lastStep;
}

extern "C" RMSDK_API DWORD RmSdk_GetLastError()
{
    return g_lastError;
}

extern "C" RMSDK_API bool RmSdk_GetCpuTemperature(double* outTemp)
{
    if (!g_initialized || !g_pCpu || !outTemp) return false;

    CPUParameters stData{};
    int result = g_pCpu->GetCPUParameters(stData);
    if (result != 0) return false;

    *outTemp = stData.dTemperature;
    return true;
}

extern "C" RMSDK_API void RmSdk_Shutdown()
{
    if (g_pPlatform)
    {
        g_pPlatform->UnInit();
        g_pPlatform = nullptr;
    }
    g_pCpu = nullptr;

    if (g_hPlatformDll)
    {
        FreeLibrary(g_hPlatformDll);
        g_hPlatformDll = nullptr;
    }

    g_initialized = false;
}