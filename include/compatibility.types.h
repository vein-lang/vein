#pragma once
#define DEBUG 1



#if defined(ARDUINO)
#define AVR_PLATFORM
#endif



#if defined(AVR_PLATFORM)
#include "Arduino.h"
#define ASM(x) __ASM volatile (x)
#define sleep(x) delay(x)
#else
#include <string>
typedef uint8_t byte;
typedef std::string String;
#define ASM(x)
#define sleep(x) 
void setup(int argc, char* argv[]);
void loop();
#endif

typedef const char* nativeString;
typedef void* wpointer;
typedef unsigned char uchar_t;

static inline wpointer malloc0(const uintptr_t x)
{
    if (x)
        return calloc(1, x);
    return nullptr;
}

#define ZERO_ARRAY 1

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



#ifdef DEBUG
#define d_print(x) Serial.print(x)
#define f_print(x) do {  Serial.print(#x);Serial.print(" ");Serial.println(x); } while(0)
#define w_print(x) Serial.println(x)
#define init_serial() Serial.begin(9600)

#ifndef AVR_PLATFORM
#include <iostream>
#undef d_print
#undef f_print
#undef w_print
#undef init_serial
#define init_serial()
#define d_print(x) std::cout << x
#define f_print(x) std::cout << #x << " " << x << "\n"
#define w_print(x) std::cout << x << "\n"
#endif

#else
#define d_print(x)
#define f_print(x)
#define w_print(x)
#define init_serial()
#endif


#define CUSTOM_EXCEPTION(name) struct name : public std::exception {    \
    const char* msg;                                                    \
    name() { msg = ""; }                                                \
    name(const char* message) { msg = message; }                        \
    name(std::string message) { msg = message.data(); }                 \
    _NODISCARD const char* what() const throw () { return msg; }        \
}

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

#if __cplusplus >= 201406
template<typename T>
struct Nullable { inline static const T Value = NULL; };

template<typename T>
 struct Nullable<T*> { inline static const T* Value = nullptr; };
#else
template<typename T>
struct Nullable { static const T Value = NULL; };

template<typename T>
struct Nullable<T*>
{
    inline static T* Value = nullptr;
};
#endif
 
#define NULL_VALUE(T) Nullable<T>::Value

#if defined(AVR_PLATFORM)
namespace std
{
    template<class InputIt, class OutputIt>
    OutputIt copy(InputIt first, InputIt last, OutputIt d_first)
    {
        while (first != last)
            *d_first++ = *first++;
        return d_first;
    }

}
#endif
template<typename T>
void array_copy(T* sourceArray, int sourceIndex, T* destinationArray, int destinationIndex, int length)
{
    std::copy(sourceArray + sourceIndex,
        sourceArray + sourceIndex + length,
        destinationArray + destinationIndex);
}
inline void vm_shutdown()
{
    w_print("\t !! WM SHUTDOWN !!");
    while (true)
    {
        sleep(200);
    }
}


#if __cplusplus >= 201703L
#define REGISTER 
#else
#define REGISTER register 
#endif
static const char* out_of_memory_Str = "<<OUT OF MEMORY>>";
inline void throw_out_of_memory()
{
    w_print(out_of_memory_Str);
    vm_shutdown();
}


#if defined(AVR_PLATFORM)
#ifdef __arm__
extern "C" char* sbrk(int incr);
#else
extern char* __brkval;
#endif 

int freeMemory() {
    char top;
#ifdef __arm__
    return &top - reinterpret_cast<char*>(sbrk(0));
#elif defined(CORE_TEENSY) || (ARDUINO > 103 && ARDUINO != 151)
    return &top - __brkval;
#else
    return __brkval ? &top - __brkval : &top - __malloc_heap_start;
#endif
}
#endif
#if defined(AVR_PLATFORM)
#define MEM_CHECK(predicate) \
    if ((freeMemory() <= 1024)) { throw_out_of_memory(); }
#else
#define MEM_CHECK(predicate) \
    if (predicate) { throw_out_of_memory(); }
#endif


