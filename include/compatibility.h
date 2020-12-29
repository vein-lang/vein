#pragma once
#include "compatibility.types.h"
#if defined(ARDUINO)
#else
int main(int argc, char* argv[])
{
    setup();
    while (1)
    {
        loop();
    }
}
#endif