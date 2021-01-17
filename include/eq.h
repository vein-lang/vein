#pragma once
#include "compatibility.types.h"
#define PTR_TO_INT(x) static_cast<int32_t>(reinterpret_cast<intptr_t>(x)) 
#define INT_TO_PTR(x) reinterpret_cast<wpointer>(static_cast<intptr_t>(x))


template<class T> struct equality {};

template<> struct equality<const char*> {
	static bool equal(const char* l, const char* r) {
		return strcmp(l, r) == 0;
	}
};

template<> struct equality<size_t> {
	static bool equal(const size_t l, const size_t r) {
		return l == r;
	}
};

template<> struct equality<int32_t> {
	static bool equal(const int32_t l, const int32_t r) {
		return l == r;
	}
};

template<> struct equality<int64_t> {
	static bool equal(const int64_t l, const int64_t r) {
		return l == r;
	}
};

template<> struct equality<int16_t> {
	static bool equal(const int16_t l, const int16_t r) {
		return l == r;
	}
};

template<> struct equality<WaveString> {
	static bool equal(const WaveString* l, const WaveString* r) {
		return equality<const char*>::equal(l->chars, r->chars);
	}
};