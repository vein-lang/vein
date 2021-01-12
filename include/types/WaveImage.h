#pragma once
#include "collections/hashtable.h"


struct WaveImage {
	char* name;

	hashtable<nativeString>* method_cache;
	hashtable<nativeString>* class_cache;
};
