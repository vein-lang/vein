#pragma once
#include "compatibility.types.h"
struct WaveString {
	uint32_t length;
	char* chars;
    
    WaveString(const char* str)
    {
        this->length = strlen(str);
        this->chars = _strdup(str);
    }
};
