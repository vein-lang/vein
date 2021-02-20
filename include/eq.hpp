#pragma once
#include "compatibility.types.hpp"

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

template<> struct equality<std::wstring> {
	static bool equal(const std::wstring& l, const std::wstring& r) {
		return wcscmp(l.c_str(), r.c_str()) == 0;
	}
};