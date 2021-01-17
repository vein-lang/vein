#include "api/Windows/eeprom_windows.h"
#if defined(WIN32)
#include <cassert>
#include <windows.h>
#include <iostream>
#include "io.h"
#include <fstream>
#include <vector>
#include <filesystem>
using namespace std::filesystem;
#define var auto
#define FS_SIZE 8192

#define validate_handle(h) do { if(!h) { std::cerr << "failed:" << GetLastError() << "\n"; }  } while(0)

HANDLE _fs_handle = nullptr;
byte* _fs_buffer = nullptr;
path fs;
std::ofstream fs_stream;

EEPROM::EEPROM()
{
    fs = current_path() / "vm.fs";

    printf("vm filesystem: %ls", fs.c_str());
    

    var fs_exist = exists(L"vm.fs");

    if (fs_exist)
    {
       var a = std::ifstream(fs);
       _fs_buffer = static_cast<byte*>(malloc(FS_SIZE));
       a.read(reinterpret_cast<char*>(_fs_buffer), FS_SIZE);
       a.close();
       fs_stream = std::ofstream(fs);
    }
    else
    {
        _fs_buffer = static_cast<byte*>(malloc(FS_SIZE));
        fs_stream = std::ofstream(fs);
        fs_stream.write(reinterpret_cast<char*>(_fs_buffer), FS_SIZE);
    }

}

EEPROM::~EEPROM()
{
    fs_stream.close();
}

byte EEPROM::read(uint32_t address)
{
    if (address >= FS_SIZE)
        return NULL;
    return _fs_buffer[address];
}
byte* EEPROM::readAddress(uint32_t address, uint32_t size)
{
    if (address >= FS_SIZE)
        return nullptr;
    const var buffer = new byte[size];
    for (uint32_t i = 0; i != size; i++)
        buffer[i] = this->read(address + i);
    return buffer;
}

bool EEPROM::write(uint32_t address, byte value)
{
    if(address >= FS_SIZE)
        return false;
    _fs_buffer[address] = value;
    fs_stream.seekp(0);
    fs_stream.write(reinterpret_cast<char*>(_fs_buffer), FS_SIZE);
    fs_stream.flush();
}
bool EEPROM::write(uint32_t address, byte* data, uint32_t dataLength)
{
    if (address >= FS_SIZE)
        return false;
    for(uint32_t i = 0; i != dataLength; i++)
        _fs_buffer[address + i] = data[i];
    fs_stream.seekp(0);
    fs_stream.write(reinterpret_cast<char*>(_fs_buffer), FS_SIZE);
    fs_stream.flush();
    return true;
}
#endif