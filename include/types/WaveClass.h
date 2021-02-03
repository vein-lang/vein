#pragma once
#include "compatibility.types.h"
#include "WaveClassField.h"

struct WaveClass {
	nativeString name;
	nativeString path;
	uint32_t   type_token;
	uint32_t   inited : 1;

	WaveClass* parent;
	
	int        instance_size;
	int        class_size;

	
	struct {
		uint32_t first, last;
		int count;
	} field, method;
	
	WaveClassField* fields;
};