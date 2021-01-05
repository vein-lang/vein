#pragma once
#include "types.h"
WaveObject *wave_object_new  (uint32_t type_token);
void        wave_object_free (WaveObject *o);


WaveObject* wave_object_new(uint32_t type_token)
{
	return nullptr;
}

static void* wave_object_allocate(size_t size)
{
	void* o = calloc(1, size);
	return o;
}



