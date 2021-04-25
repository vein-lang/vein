#pragma once


#if defined(ARDUINO_ARCH_SAM)
#include "SAM/eeprom_sam.hpp"
#elif defined(WIN32)
#include "Windows/eeprom_windows.hpp"
#endif