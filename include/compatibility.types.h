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