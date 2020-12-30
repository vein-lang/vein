#pragma once
#include "compatibility.types.h"

uint32_t w_hash_direct(const wpointer v1)
{
	return static_cast<uint32_t>(reinterpret_cast<intptr_t>(v1));
}

uint32_t w_hash_int(const wpointer v1)
{
	return w_hash_direct(v1);
}

uint32_t w_hash_str(const wpointer v1)
{
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