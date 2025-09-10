#include "pch.h"

// Убеждаемся, что макрос экспорта определен
#ifndef L2MARKETINJECTOR_EXPORTS
#define L2MARKETINJECTOR_EXPORTS
#endif

#include "FlunderDLL.h"
#include "injector.h"

#include <stdio.h>
#include <string>
#include <iostream>
#include <set>
#include <tlhelp32.h>
#include <winternl.h>

using namespace std;

// Макрос для проверки успешности NTSTATUS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)

// Дополнительные определения, которых нет в winternl.h
#ifndef ObjectNameInformation
#define ObjectNameInformation 1
#endif

#ifndef ObjectTypeInformation
#define ObjectTypeInformation 2
#endif

#define SystemHandleInformation 16

typedef struct _SYSTEM_HANDLE {
    ULONG ProcessId;
    BYTE ObjectTypeNumber;
    BYTE Flags;
    USHORT Handle;
    PVOID Object;
    ACCESS_MASK GrantedAccess;
} SYSTEM_HANDLE, *PSYSTEM_HANDLE;

typedef struct _SYSTEM_HANDLE_INFORMATION {
    ULONG HandleCount;
    SYSTEM_HANDLE Handles[1];
} SYSTEM_HANDLE_INFORMATION, *PSYSTEM_HANDLE_INFORMATION;

typedef NTSTATUS(NTAPI* NtQuerySystemInformationPrototype)(
    ULONG SystemInformationClass,
    PVOID SystemInformation,
    ULONG SystemInformationLength,
    PULONG ReturnLength
);

typedef NTSTATUS(NTAPI* NtQueryObjectPrototype)(
    HANDLE Handle,
    OBJECT_INFORMATION_CLASS ObjectInformationClass,
    PVOID ObjectInformation,
    ULONG ObjectInformationLength,
    PULONG ReturnLength
);

typedef struct _OBJECT_NAME_INFORMATION {
    UNICODE_STRING Name;
} OBJECT_NAME_INFORMATION, *POBJECT_NAME_INFORMATION;

typedef struct _OBJECT_TYPE_INFORMATION {
    UNICODE_STRING TypeName;
    ULONG TotalNumberOfObjects;
    ULONG TotalNumberOfHandles;
    ULONG TotalPagedPoolUsage;
    ULONG TotalNonPagedPoolUsage;
    ULONG TotalNamePoolUsage;
    ULONG TotalHandleTableUsage;
    ULONG HighWaterNumberOfObjects;
    ULONG HighWaterNumberOfHandles;
    ULONG HighWaterPagedPoolUsage;
    ULONG HighWaterNonPagedPoolUsage;
    ULONG HighWaterNamePoolUsage;
    ULONG HighWaterHandleTableUsage;
    ULONG InvalidAttributes;
    GENERIC_MAPPING GenericMapping;
    ULONG ValidAccessMask;
    BOOLEAN SecurityRequired;
    BOOLEAN MaintainHandleCount;
    USHORT MaintainTypeList;
    ULONG PoolType;
    ULONG DefaultPagedPoolCharge;
    ULONG DefaultNonPagedPoolCharge;
} OBJECT_TYPE_INFORMATION, *POBJECT_TYPE_INFORMATION;

// Глобальные переменные для DLL
static bool g_initialized = false;
static NtQuerySystemInformationPrototype g_NtQuerySystemInformation = nullptr;

// Функции для работы с системными хендлами (скопированы из Flunder.cpp)
void PrintError(const char* msg) {
    DWORD error = GetLastError();
    printf("[!] %s Error: %lu\n", msg, error);
}

NtQuerySystemInformationPrototype LoadNtQuerySystemInformation() {
    HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
    if (!ntdll) {
        PrintError("Failed to get ntdll.dll module");
        return nullptr;
    }

    auto func = (NtQuerySystemInformationPrototype)GetProcAddress(ntdll, "NtQuerySystemInformation");
    if (!func) PrintError("Failed to load NtQuerySystemInformation");
    return func;
}

NtQueryObjectPrototype LoadNtQueryObject() {
    HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
    if (!ntdll) {
        PrintError("Failed to get ntdll.dll module");
        return nullptr;
    }

    auto func = (NtQueryObjectPrototype)GetProcAddress(ntdll, "NtQueryObject");
    if (!func) PrintError("Failed to load NtQueryObject");
    return func;
}

PSYSTEM_HANDLE_INFORMATION QueryHandleInformation(NtQuerySystemInformationPrototype NtQuerySystemInformation) {
    ULONG bufferSize = 0x1000000;
    PSYSTEM_HANDLE_INFORMATION handleInfo = (PSYSTEM_HANDLE_INFORMATION)malloc(bufferSize);
    NTSTATUS status = NtQuerySystemInformation(SystemHandleInformation, handleInfo, bufferSize, &bufferSize);

    if (status == 0xC0000004) {
        free(handleInfo);
        handleInfo = (PSYSTEM_HANDLE_INFORMATION)malloc(bufferSize);
        status = NtQuerySystemInformation(SystemHandleInformation, handleInfo, bufferSize, &bufferSize);
    }

    if (status) {
        free(handleInfo);
        PrintError("NtQuerySystemInformation failed");
        return nullptr;
    }

    return handleInfo;
}

HANDLE FindPrivilegedHandleToProcess(DWORD targetPID, NtQuerySystemInformationPrototype NtQuerySystemInformation) {
    printf("[*] Searching for privileged handles to process PID %lu...\n", targetPID);
    
    auto handleInfo = QueryHandleInformation(NtQuerySystemInformation);
    if (!handleInfo) {
        printf("[!] Failed to get system handle information\n");
        return NULL;
    }

    HANDLE bestHandle = NULL;
    ACCESS_MASK maxAccess = 0;
    DWORD bestOwnerPID = 0;

    printf("[*] Total handles in system: %lu\n", handleInfo->HandleCount);
    printf("[*] Looking for handles with PROCESS_ALL_ACCESS rights...\n\n");

    for (ULONG i = 0; i < handleInfo->HandleCount; i++) {
        SYSTEM_HANDLE handle = handleInfo->Handles[i];

        if ((handle.GrantedAccess & PROCESS_ALL_ACCESS) && handle.ObjectTypeNumber == 7 && handle.ProcessId != targetPID) {
            HANDLE processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_DUP_HANDLE, FALSE, handle.ProcessId);
            if (processHandle && processHandle != INVALID_HANDLE_VALUE) {
                HANDLE duplicatedHandle = NULL;

                if (DuplicateHandle(processHandle, (HANDLE)(ULONG_PTR)handle.Handle, GetCurrentProcess(), &duplicatedHandle, 0, FALSE, DUPLICATE_SAME_ACCESS)) {
                    if (duplicatedHandle && duplicatedHandle != INVALID_HANDLE_VALUE) {
                        DWORD targetProcessID = GetProcessId(duplicatedHandle);
                        if (targetProcessID == targetPID) {
                            printf("[+] Found handle to target process!\n");
                            printf("    Handle: 0x%04X\n", handle.Handle);
                            printf("    Process ID (owner): %lu\n", handle.ProcessId);
                            printf("    Object Type: %d (Process)\n", handle.ObjectTypeNumber);
                            printf("    Granted Access: 0x%08X\n", handle.GrantedAccess);
                            printf("    Flags: 0x%02X\n", handle.Flags);
                            printf("    Object Address: 0x%p\n", handle.Object);
                            printf("    Duplicated Handle: 0x%p\n", duplicatedHandle);
                            printf("\n");

                            if (handle.GrantedAccess > maxAccess) {
                                if (bestHandle) {
                                    CloseHandle(bestHandle);
                                }
                                bestHandle = duplicatedHandle;
                                maxAccess = handle.GrantedAccess;
                                bestOwnerPID = handle.ProcessId;
                                printf("[+] Updated to handle with better access: 0x%08X from PID %lu\n", maxAccess, bestOwnerPID);
                            } else {
                                CloseHandle(duplicatedHandle);
                            }
                        } else {
                            CloseHandle(duplicatedHandle);
                        }
                    }
                }
                CloseHandle(processHandle);
            }
        }
    }

    free(handleInfo);

    if (bestHandle) {
        printf("[+] Using privileged handle with access: 0x%08X from PID %lu\n", maxAccess, bestOwnerPID);
    } else {
        printf("[!] No privileged handles found to target process (PID: %lu)\n", targetPID);
    }

    return bestHandle;
}

DWORD GetParentProcessId(DWORD processId) {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        return 0;
    }

    PROCESSENTRY32W pe32;
    pe32.dwSize = sizeof(PROCESSENTRY32W);

    if (!Process32FirstW(hSnapshot, &pe32)) {
        CloseHandle(hSnapshot);
        return 0;
    }

    do {
        if (pe32.th32ProcessID == processId) {
            CloseHandle(hSnapshot);
            return pe32.th32ParentProcessID;
        }
    } while (Process32NextW(hSnapshot, &pe32));

    CloseHandle(hSnapshot);
    return 0;
}

HANDLE FindPrivilegedHandleFromParent(DWORD targetPID, NtQuerySystemInformationPrototype NtQuerySystemInformation) {
    DWORD parentPID = GetParentProcessId(targetPID);
    if (parentPID == 0) {
        printf("[!] Failed to get parent process for PID %lu\n", targetPID);
        return NULL;
    }
    
    printf("[*] Parent process of PID %lu is PID: %lu\n", targetPID, parentPID);
    printf("[*] Searching for handles from parent process...\n");
    
    auto handleInfo = QueryHandleInformation(NtQuerySystemInformation);
    if (!handleInfo) {
        return NULL;
    }

    HANDLE bestHandle = NULL;
    ACCESS_MASK maxAccess = 0;

    for (ULONG i = 0; i < handleInfo->HandleCount; i++) {
        SYSTEM_HANDLE handle = handleInfo->Handles[i];
        
        if (handle.ObjectTypeNumber == 7 && handle.ProcessId == parentPID) {
            HANDLE ownerProcessHandle = OpenProcess(PROCESS_DUP_HANDLE, FALSE, handle.ProcessId);
            if (ownerProcessHandle && ownerProcessHandle != INVALID_HANDLE_VALUE) {
                HANDLE duplicatedHandle = NULL;
                
                if (DuplicateHandle(ownerProcessHandle, (HANDLE)(ULONG_PTR)handle.Handle, GetCurrentProcess(), &duplicatedHandle, 0, FALSE, DUPLICATE_SAME_ACCESS)) {
                    if (duplicatedHandle && duplicatedHandle != INVALID_HANDLE_VALUE) {
                        DWORD targetProcessID = GetProcessId(duplicatedHandle);
                        if (targetProcessID == targetPID) {
                            printf("[+] Found handle from parent PID %lu, Access: 0x%08X\n", 
                                   handle.ProcessId, handle.GrantedAccess);
                            
                            if (handle.GrantedAccess > maxAccess) {
                                if (bestHandle) {
                                    CloseHandle(bestHandle);
                                }
                                bestHandle = duplicatedHandle;
                                maxAccess = handle.GrantedAccess;
                                printf("[+] Updated to handle with better access: 0x%08X\n", maxAccess);
                            } else {
                                CloseHandle(duplicatedHandle);
                            }
                        } else {
                            CloseHandle(duplicatedHandle);
                        }
                    }
                }
                CloseHandle(ownerProcessHandle);
            }
        }
    }

    free(handleInfo);
    return bestHandle;
}

bool IsCorrectTargetArchitecture(HANDLE hProc) {
    BOOL bTarget = FALSE;
    if (!IsWow64Process(hProc, &bTarget)) {
        printf("Can't confirm target process architecture: 0x%X\n", GetLastError());
        return false;
    }

    BOOL bHost = FALSE;
    IsWow64Process(GetCurrentProcess(), &bHost);

    return (bTarget == bHost);
}

DWORD GetProcessIdByName(wchar_t* name) {
    PROCESSENTRY32 entry;
    entry.dwSize = sizeof(PROCESSENTRY32);

    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, NULL);

    if (Process32First(snapshot, &entry) == TRUE) {
        while (Process32Next(snapshot, &entry) == TRUE) {
            if (_wcsicmp(entry.szExeFile, name) == 0) {
                CloseHandle(snapshot);
                return entry.th32ProcessID;
            }
        }
    }

    CloseHandle(snapshot);
    return 0;
}

// Экспортируемые функции
extern "C" {

FLUNDER_API bool InjectDLL(const char* dllPath, int processId) {
    // Проверка параметров
    if (!dllPath || processId == 0) {
        return false;
    }
    
    // Инициализация если еще не инициализирована
    if (!g_initialized) {
        g_NtQuerySystemInformation = LoadNtQuerySystemInformation();
        if (!g_NtQuerySystemInformation) {
            return false;
        }
        g_initialized = true;
    }
    
    // Проверка существования файла
    if (GetFileAttributesA(dllPath) == INVALID_FILE_ATTRIBUTES) {
        return false;
    }
    
    // Включение привилегии отладки
    TOKEN_PRIVILEGES priv = { 0 };
    HANDLE hToken = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken)) {
        priv.PrivilegeCount = 1;
        priv.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
        if (LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &priv.Privileges[0].Luid)) {
            AdjustTokenPrivileges(hToken, FALSE, &priv, 0, NULL, NULL);
        }
        CloseHandle(hToken);
    }
    
    // Получение хендла процесса
    HANDLE hProc = FindPrivilegedHandleToProcess(processId, g_NtQuerySystemInformation);
    if (!hProc) {
        hProc = FindPrivilegedHandleFromParent(processId, g_NtQuerySystemInformation);
    }
    
    if (!hProc) {
        return false;
    }
    
    // Проверка архитектуры
    if (!IsCorrectTargetArchitecture(hProc)) {
        CloseHandle(hProc);
        return false;
    }
    
    // Загрузка DLL файла
    std::ifstream File(dllPath, std::ios::binary | std::ios::ate);
    if (File.fail()) {
        CloseHandle(hProc);
        return false;
    }
    
    auto fileSize = File.tellg();
    if (fileSize < 0x1000) {
        File.close();
        CloseHandle(hProc);
        return false;
    }
    
    BYTE* pSrcData = new BYTE[(UINT_PTR)fileSize];
    if (!pSrcData) {
        File.close();
        CloseHandle(hProc);
        return false;
    }
    
    File.seekg(0, std::ios::beg);
    File.read((char*)(pSrcData), fileSize);
    File.close();
    
    // Выполнение инжекции
    bool injectionSuccess = ManualMapDll(hProc, pSrcData, static_cast<SIZE_T>(fileSize));
    
    // Очистка
    delete[] pSrcData;
    CloseHandle(hProc);
    
    return injectionSuccess;
}

FLUNDER_API int FindProcessByName(const wchar_t* processName) {
    if (!processName) {
        return 0;
    }
    
    return GetProcessIdByName(const_cast<wchar_t*>(processName));
}

} // extern "C"

// DLL Entry Point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        // Инициализация при загрузке DLL
        break;
    case DLL_THREAD_ATTACH:
        break;
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
        // Очистка при выгрузке DLL
        g_initialized = false;
        g_NtQuerySystemInformation = nullptr;
        break;
    }
    return TRUE;
}
