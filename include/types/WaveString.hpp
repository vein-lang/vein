#pragma once
#include "compatibility.types.hpp"
#include "eq.hpp"
#include "hash.hpp"
struct WaveString {
	uint32_t length;
	char* chars;
    
    WaveString(const char* str)
    {
        this->length = strlen(str);
        this->chars = _strdup(str);
    }
};

template<> struct equality<WaveString> {
	static bool equal(const WaveString* l, const WaveString* r) {
		return equality<const char*>::equal(l->chars, r->chars);
	}
};

template<> struct hash_gen<WaveString> {
    static size_t getHashCode(WaveString* s) {
        return hash_gen<char*>::getHashCode(s->chars);
    }
};