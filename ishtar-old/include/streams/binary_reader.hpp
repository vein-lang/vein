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
    unsigned char ReadByte() const
    {
        return
            _origin->ReadByte();
    }
    [[nodiscard]]
    uint16_t Read2() const
    {
        return
            (static_cast<uint16_t>(_origin->ReadByte()) << 0)  | 
            (static_cast<uint16_t>(_origin->ReadByte()) << 8);
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
        ValidateMagicFlag();
        const auto size = Read4();
        auto* const body = ReadBytes(size);
        return BytesToUTF8(body, size);
    }

    void ValidateMagicFlag() const
    {
        const int m1 = Read4();
        const int m2 = Read4();
         if (m1 != 0xFF || m2 != 0xFF)
            throw InvalidOperationException("Cannot read string from binary stream. [magic flag invalid]");
    }

    [[nodiscard]]
    int Position() const noexcept
    {
        return _origin->Position();
    }
    [[nodiscard]]
    int Length() const noexcept
    {
        return _origin->Length();
    }

    void Seek(int offset)
    {
        _origin->Seek(offset);
    }

private:
    MemoryStream* _origin;
};
