#pragma once


#if defined(ARDUINO_ARCH_SAM)
#include "SAM/eeprom_sam.h"
#elif defined(WIN32)
#include "Windows/eeprom_windows.h"
#endif