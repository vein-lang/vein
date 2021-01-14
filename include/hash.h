#pragma once
#include "compatibility.types.h"
#include "types/WaveString.h"

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

template<> struct hash_gen<const char*> {
    static size_t getHashCode(const char* s) {
        return hash_gen<char*>::getHashCode(s);
    }
};
template<> struct hash_gen<WaveString> {
    static size_t getHashCode(WaveString* s) {
        return hash_gen<char*>::getHashCode(s->chars);
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

template<> struct hash_gen<size_t> {
    static size_t getHashCode(const size_t m) {
        return m;
    }
};

int primes[] = {
   3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
   1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
   17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
   187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
   1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
};
#define PRIMES_SIZE 72
#define HASH_PRIME 101
class Hash
{
public:
    static int getPrime(int min)
    {
        if (min < 0)
            return getMinPrime();

        for (auto prime : primes)
        {
            if (prime >= min) 
                return prime;
        }
        for (auto i = (min | 1); i < 2147483647; i += 2)
        {
            if (isPrime(i) && ((i - 1) % HASH_PRIME != 0))
                return i;
        }
        return min;
    }

    static int getMinPrime()
    {
        return primes[0];
    }

    static bool isPrime(int candidate)
    {
        if ((candidate & 1) != 0)
        {
            const auto limit = static_cast<int>(sqrt(candidate));
            for (auto divisor = 3; divisor <= limit; divisor += 2)
            {
                if ((candidate % divisor) == 0)
                    return false;
            }
            return true;
        }
        return (candidate == 2);
    }

    static int expandPrime(int oldSize)
    {
        auto newSize = 2 * oldSize;
        if (static_cast<uint32_t>(newSize) > 0x7FEFFFFD && 0x7FEFFFFD > oldSize)
            return 0x7FEFFFFD;
        return getPrime(newSize);
    }
};