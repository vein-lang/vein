#pragma once


#if defined(ARDUINO)
#include "Arduino.h"
#define ASM(x) __ASM volatile (x)
#else
#include <string>
typedef std::string String;
#define ASM(x)
#define sleep(x) 
void setup();
void loop();
int main(int argc, char* argv[])
{
    setup();
    while (1)
    {
        loop();
    }
}
#endif