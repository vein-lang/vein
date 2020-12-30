#pragma once
#include "compatibility.types.h"
#define PTR_TO_INT(x) static_cast<int32_t>(reinterpret_cast<intptr_t>(x)) 
#define INT_TO_PTR(x) reinterpret_cast<wpointer>(static_cast<intptr_t>(x))


bool w_equal_str(const wpointer v1, const wpointer v2)
{
	return strcmp(static_cast<const char*>(v1), static_cast<const char*>(v2)) == 0;
}


bool w_equal_int(const wpointer v1, const wpointer v2)
{
	return PTR_TO_INT(v1) == PTR_TO_INT(v2);
}

bool w_equal_direct(const wpointer v1, const wpointer v2)
{
	return v1 == v2;
}