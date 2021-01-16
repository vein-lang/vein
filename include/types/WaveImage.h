#pragma once
#include "collections/hashtable.h"


struct WaveImage {
	char* name;

	hashtable<nativeString>* method_cache;
	hashtable<nativeString>* class_cache;
	hashtable<size_t>*       db_str_cache;

    WaveImage(const char* _name)
    {
        name = const_cast<char*>(_name);
        method_cache = new hashtable<nativeString>();
        class_cache = new hashtable<nativeString>();
        db_str_cache = new hashtable<size_t>();
    }
};
