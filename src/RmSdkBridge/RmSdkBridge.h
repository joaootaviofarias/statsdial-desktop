#pragma once
#include <windows.h>

#ifdef RMSDKBRIDGE_EXPORTS
#define RMSDK_API __declspec(dllexport)
#else
#define RMSDK_API __declspec(dllimport)
#endif

extern "C" {
    RMSDK_API bool RmSdk_Init();
    RMSDK_API int  RmSdk_GetLastStep();
    RMSDK_API DWORD RmSdk_GetLastError();
    RMSDK_API bool RmSdk_GetCpuTemperature(double* outTemp);
    RMSDK_API void RmSdk_Shutdown();
}