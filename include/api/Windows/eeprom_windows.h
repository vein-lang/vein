#pragma once
#include "compatibility.types.h"
#if defined(WIN32)
class EEPROM {
public:
	EEPROM();
	~EEPROM();
	byte read(uint32_t address);
	byte* readAddress(uint32_t address, uint32_t size);
    bool write(uint32_t address, byte value);
	bool write(uint32_t address, byte* data, uint32_t dataLength);
};
#endif