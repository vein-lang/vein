#pragma once
#include "compatibility.types.hpp"
#if defined(ARDUINO)
#else
int main(int argc, char* argv[])
{
    setup(argc, argv);
    while (1)
    {
        loop();
    }
}
#endif