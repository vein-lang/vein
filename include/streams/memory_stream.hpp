#pragma once
#include "stream.hpp"

class MemoryStream : Stream
{
public:
    MemoryStream() : MemoryStream(0) { }
    MemoryStream(int capacity)
    {
        _buffer = capacity != 0 ? new char[capacity] : new char[1];
        _capacity = capacity;
        _length = capacity;
    }

    MemoryStream(char* buffer, int size)
    {
        _buffer = buffer;
        _capacity = size;
        _length = size;
    }
    long Capacity() override
    {
        return _capacity - _origin;
    }
    long Length() override
    {
        return _length - _origin;
    }
    long Position() override
    {
        return _position - _origin;
    }
    char Read(char* buffer, const int offset, const int count)
    {
        auto n = _length - _position;
        if (n > count)
            n = count;
        if (n <= 0)
            return 0;
        auto byteCount = n;
        while (--byteCount >= 0)
            buffer[offset + byteCount] = _buffer[_position + byteCount];
        _position += n;
        return n;
    }
    unsigned char ReadByte()
    {
      auto* buffer = new char[1];
      return Read(buffer, 0, 1) == 0 ? -1 : static_cast<unsigned char>(buffer[0]);
    }
private:
    char* _buffer;
    int _origin = 0;
    int _position = 0;
    int _length = 0;
    int _capacity = 0;
};
