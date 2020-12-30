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


#define UINT32_TO_LE(x) (x)
#define UINT64_TO_LE(x) (x)
#define UINT16_TO_LE(x) (x)

#define UINT32_FROM_LE(x)  (UINT32_TO_LE (x))
#define UINT64_FROM_LE(x)  (UINT64_TO_LE (x))
#define UINT16_FROM_LE(x)  (UINT16_TO_LE (x))

#define read16(x) UINT16_FROM_LE (*((const uint16_t *) (x)))
#define read32(x) UINT32_FROM_LE (*((const uint32_t *) (x)))
#define read64(x) UINT64_FROM_LE (*((const uint64_t *) (x)))