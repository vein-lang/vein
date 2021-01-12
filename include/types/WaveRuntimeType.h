#pragma once
#include "compatibility.types.h"
#include "typetoken.h"
#include "WaveArray.h"
#include "WaveModificator.h"
struct ModifiedType;
struct WaveClass;
struct WaveMethodSignature;
struct WaveRuntimeType {
	WaveTypeEnum type;
	union {
		WaveClass* klass;
		WaveRuntimeType* type;
		WaveArray* array;
		WaveMethodSignature* method;
	} data;
};

struct ModifiedType {
	WaveRuntimeType* type;
	int num_modifiers;
	WaveModificator modifiers[ZERO_ARRAY];
};

