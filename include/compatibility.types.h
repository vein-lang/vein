#pragma once
#if defined(ARDUINO)
#include "Arduino.h"
#define ASM(x) __ASM volatile (x)
#define sleep(x) delay(x)
#else
#include <string>
typedef uint8_t byte;
typedef std::string String;
#define ASM(x)
#define sleep(x) 
void setup();
void loop();
#endif

typedef const char* nativeString;
typedef void* wpointer;

static inline wpointer malloc0(const uintptr_t x)
{
    if (x)
        return calloc(1, x);
    return nullptr;
}

#define new0(type,size)  ((type *) malloc0(sizeof (type) * (size)))


#define UINT32_TO_LE(x) (x)
#define UINT64_TO_LE(x) (x)
#define UINT16_TO_LE(x) (x)

#define UINT32_FROM_LE(x)  (UINT32_TO_LE (x))
#define UINT64_FROM_LE(x)  (UINT64_TO_LE (x))
#define UINT16_FROM_LE(x)  (UINT16_TO_LE (x))

#define read16(x) UINT16_FROM_LE (*((const uint16_t *) (x)))
#define read32(x) UINT32_FROM_LE (*((const uint32_t *) (x)))
#define read64(x) UINT64_FROM_LE (*((const uint64_t *) (x)))



template<typename T>
using Comparer = int(T t1, T t2);

template<typename T>
using Predicate = bool(T t);

template<typename T>
using Action0 = void(T t);
template<typename TSelf, typename T1>
using Action1 = void(TSelf self, T1 t1);
template<typename TSelf, typename T1, typename T2>
using Action2 = void(TSelf self, T1 t1, T2 t2);

template<typename T>
using Func0 = T();
template<typename T0, typename T1>
using Func1 = T0(T1 arg1);