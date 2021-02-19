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
    long Read8() const noexcept
    {
        return
            static_cast<long>(_origin->ReadByte()) << 0  | 
            static_cast<long>(_origin->ReadByte()) << 8  | 
            static_cast<long>(_origin->ReadByte()) << 16 | 
            static_cast<long>(_origin->ReadByte()) << 24 | 
            static_cast<long>(_origin->ReadByte()) << 32 | 
            static_cast<long>(_origin->ReadByte()) << 40 | 
            static_cast<long>(_origin->ReadByte()) << 48;
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
        const auto body = ReadBytes(size);

        return BytesToUTF8(body, size);
    }




private:
    MemoryStream* _origin;


    static std::wstring BytesToUTF8(const char* in, size_t size)
    {
        std::wstring out;
        unsigned int codepoint;
        int arrow = 0;
        while (arrow != size)
        {
            unsigned char ch = static_cast<unsigned char>(*in);
            if (ch <= 0x7f)
                codepoint = ch;
            else if (ch <= 0xbf)
                codepoint = (codepoint << 6) | (ch & 0x3f);
            else if (ch <= 0xdf)
                codepoint = ch & 0x1f;
            else if (ch <= 0xef)
                codepoint = ch & 0x0f;
            else
                codepoint = ch & 0x07;
            ++in;
            ++arrow;
            if (((*in & 0xc0) != 0x80) && (codepoint <= 0x10ffff))
            {
                if (sizeof(wchar_t) > 2)
                    out.append(1, static_cast<wchar_t>(codepoint));
                else if (codepoint > 0xffff)
                {
                    out.append(1, static_cast<wchar_t>(0xd800 + (codepoint >> 10)));
                    out.append(1, static_cast<wchar_t>(0xdc00 + (codepoint & 0x03ff)));
                }
                else if (codepoint < 0xd800 || codepoint >= 0xe000)
                    out.append(1, static_cast<wchar_t>(codepoint));
            }
        }
        return out;
    }
};
