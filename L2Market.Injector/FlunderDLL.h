#pragma once

#include <Windows.h>

#ifdef L2MARKETINJECTOR_EXPORTS
#define FLUNDER_API __declspec(dllexport)
#else
#define FLUNDER_API __declspec(dllimport)
#endif


// Экспортируемые функции
extern "C" {
    // Основная функция инжекции
    FLUNDER_API bool InjectDLL(const char* dllPath, int processId);
    
    // Поиск процесса по имени
    FLUNDER_API int FindProcessByName(const wchar_t* processName);
}
