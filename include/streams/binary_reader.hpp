#pragma once
#include "memory_stream.hpp"


class BinaryReader
{
public:
    BinaryReader(MemoryStream* origin)
    {
        _origin = origin;
    }
    [[nodiscard]]
    unsigned char ReadU2() const
    {
        return
            _origin->ReadByte();
    }
    [[nodiscard]]
    int Read4() const
    {
        return
            (static_cast<int>(_origin->ReadByte()) << 0)  | 
            (static_cast<int>(_origin->ReadByte()) << 8)  | 
            (static_cast<int>(_origin->ReadByte()) << 16) | 
            (static_cast<int>(_origin->ReadByte()) << 24); // (a4 << 24) | (a3 << 16) | (a2 << 8)| (a1 << 0)
    }
    [[nodiscard]]
    uint64_t Read8() const noexcept
    {
        return
            static_cast<uint64_t>(_origin->ReadByte()) << 0  | 
            static_cast<uint64_t>(_origin->ReadByte()) << 8  | 
            static_cast<uint64_t>(_origin->ReadByte()) << 16 | 
            static_cast<uint64_t>(_origin->ReadByte()) << 24 | 
            static_cast<uint64_t>(_origin->ReadByte()) << 32 | 
            static_cast<uint64_t>(_origin->ReadByte()) << 40 | 
            static_cast<uint64_t>(_origin->ReadByte()) << 48 |
            static_cast<uint64_t>(_origin->ReadByte()) << 56;
    }
    [[nodiscard]]
    char* ReadBytes(const size_t size) const noexcept
    {
        auto* body = new char[size];
        for (auto i = 0; i != size; i++)
            body[i] = _origin->ReadByte();
        return body;
    }

    [[nodiscard]]
    wstring ReadInsomniaString() const noexcept(false)
    {
        const auto size = Read4();
        const auto magic = ReadU2();
        if (magic != 0x45)
            throw InvalidOperationException("Cannot read string from binary stream. [magic flag invalid]");
        auto* const body = ReadBytes(size);

        return BytesToUTF8(body, size);
    }




private:
    MemoryStream* _origin;
};
