#pragma once

#include <Arduino.h>
#include "flash_efc.h"
#include "efc.h"
// #define FLASH_DEBUG

#define DATA_LENGTH   ((IFLASH1_PAGE_SIZE/sizeof(byte))*4)
#define  FLASH_START  ((byte *)IFLASH1_ADDR)

#ifdef FLASH_DEBUG
#define _FLASH_DEBUG(x) Serial.print(x);
#else
#define _FLASH_DEBUG(x)
#endif

class EEPROM {
public:
    EEPROM();
	byte read(uint32_t address);
	byte* readAddress(uint32_t address);
	boolean write(uint32_t address, byte value);
	boolean write(uint32_t address, byte *data, uint32_t dataLength);
	boolean write_unlocked(uint32_t address, byte value);
	boolean write_unlocked(uint32_t address, byte *data, uint32_t dataLength);
};

EEPROM::EEPROM() {
  uint32_t retCode;

  /* Initialize flash: 6 wait states for flash writing. */
  retCode = flash_init(FLASH_ACCESS_MODE_128, 6);
  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Flash init failed\n");
  }
}

byte EEPROM::read(uint32_t address) {
  return FLASH_START[address];
}
byte* EEPROM::readAddress(uint32_t address) {
  return FLASH_START+address;
}

boolean EEPROM::write(uint32_t address, byte value) {
  uint32_t retCode;
  uint32_t byteLength = 1;  
  byte *data;

  retCode = flash_unlock((uint32_t)FLASH_START+address, (uint32_t)FLASH_START+address + byteLength - 1, 0, 0);
  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Failed to unlock flash for write\n");
    return false;
  }

  // write data
  retCode = flash_write((uint32_t)FLASH_START+address, &value, byteLength, 1);
  //retCode = flash_write((uint32_t)FLASH_START, data, byteLength, 1);

  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Flash write failed\n");
    return false;
  }

  // Lock page
  retCode = flash_lock((uint32_t)FLASH_START+address, (uint32_t)FLASH_START+address + byteLength - 1, 0, 0);
  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Failed to lock flash page\n");
    return false;
  }
  return true;
}

boolean EEPROM::write(uint32_t address, byte *data, uint32_t dataLength) {
  uint32_t retCode;

  if ((uint32_t)FLASH_START+address < IFLASH1_ADDR) {
    _FLASH_DEBUG("Flash write address too low\n");
    return false;
  }

  if ((uint32_t)FLASH_START+address >= (IFLASH1_ADDR + IFLASH1_SIZE)) {
    _FLASH_DEBUG("Flash write address too high\n");
    return false;
  }

  if (((uint32_t)FLASH_START+address & 3) != 0) {
    _FLASH_DEBUG("Flash start address must be on four byte boundary\n");
    return false;
  }

  // Unlock page
  retCode = flash_unlock((uint32_t)FLASH_START+address, (uint32_t)FLASH_START+address + dataLength - 1, 0, 0);
  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Failed to unlock flash for write\n");
    return false;
  }

  // write data
  retCode = flash_write((uint32_t)FLASH_START+address, data, dataLength, 1);

  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Flash write failed\n");
    return false;
  }

  // Lock page
    retCode = flash_lock((uint32_t)FLASH_START+address, (uint32_t)FLASH_START+address + dataLength - 1, 0, 0);
  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Failed to lock flash page\n");
    return false;
  }
  return true;
}

boolean EEPROM::write_unlocked(uint32_t address, byte value) {
  uint32_t retCode;
  uint32_t byteLength = 1;  
  byte *data;

  // write data
  retCode = flash_write((uint32_t)FLASH_START+address, &value, byteLength, 1);
  //retCode = flash_write((uint32_t)FLASH_START, data, byteLength, 1);

  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Flash write failed\n");
    return false;
  }

  return true;
}

boolean EEPROM::write_unlocked(uint32_t address, byte *data, uint32_t dataLength) {
  uint32_t retCode;

  if ((uint32_t)FLASH_START+address < IFLASH1_ADDR) {
    _FLASH_DEBUG("Flash write address too low\n");
    return false;
  }

  if ((uint32_t)FLASH_START+address >= (IFLASH1_ADDR + IFLASH1_SIZE)) {
    _FLASH_DEBUG("Flash write address too high\n");
    return false;
  }

  if (((uint32_t)FLASH_START+address & 3) != 0) {
    _FLASH_DEBUG("Flash start address must be on four byte boundary\n");
    return false;
  }

  // write data
  retCode = flash_write((uint32_t)FLASH_START+address, data, dataLength, 1);

  if (retCode != FLASH_RC_OK) {
    _FLASH_DEBUG("Flash write failed\n");
    return false;
  }

  return true;
}