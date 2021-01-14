#pragma once
#include "collections/hashtable.h"


struct WaveImage {
	char* name;

	hashtable<nativeString>* method_cache;
	hashtable<nativeString>* class_cache;
	hashtable<size_t>*       db_str_cache;
};
