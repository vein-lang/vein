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

typedef void* wpointer;
static inline wpointer malloc0(const uintptr_t x)
{
    if (x)
        return calloc(1, x);
    return nullptr;
}

#define new0(type,size)  ((type *) malloc0(sizeof (type)* (size)))