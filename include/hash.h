#pragma once
#include "compatibility.types.h"

template<class T> struct hash_gen {};


template<> struct hash_gen<char*> {
    static size_t getHashCode(const char* v1) {
        const auto* ch_ptr1 = static_cast<const char*>(v1);
        auto num1 = 0x1505;
        auto num2 = num1;
        const auto* ch_ptr2 = ch_ptr1;
        int num3;
        while ((num3 = *ch_ptr2) != 0)
        {
            num1 = (num1 << 5) + num1 ^ num3;
            const auto num4 = static_cast<int>(ch_ptr2[1]);
            if (num4 != 0)
            {
                num2 = (num2 << 5) + num2 ^ num4;
                ch_ptr2 += 2;
            }
            else break;
        }
        return num1 + num2 * 0x5D588B65;
    }
};

template<> struct hash_gen<String> {
    static size_t getHashCode(const String& s) {
        return hash_gen<char*>::getHashCode(s.c_str());
    }
};

template<> struct hash_gen<wpointer> {
    static size_t getHashCode(const wpointer m) {
        return reinterpret_cast<size_t>(m);
    }
};

template<> struct hash_gen<int> {
    static size_t getHashCode(const int m) {
        return static_cast<uint32_t>(m);
    }
};
