#pragma once
#include "compatibility.types.h"
#include "WaveArray.h"
#include "WaveMethodSignature.h"
struct ModifiedType;
struct WaveRuntimeType {
	uchar_t type; /* ElementTypeEnum */
	uchar_t custom_mod; /* for PTR and SZARRAY: use data.mtype instead of data.type */
	uchar_t byref; /* when included in a MonoRetType */
	uchar_t constraint; /* valid when included in a local var signature */
	union {
		uint32_t token; /* for VALUETYPE and CLASS */
		WaveRuntimeType* type;
		ModifiedType* mtype;
		WaveArray* array; /* for ARRAY */
		WaveMethodSignature* method;
	} data;
};

struct ModifiedType {
	WaveRuntimeType* type;
	int num_modifiers;
	WaveModificator modifiers[ZERO_ARRAY];
};

