#pragma once
#include "compatibility.types.hpp"
#include "WaveArray.h"
#include "WaveModificator.h"
#include <WaveTypeCode.hpp>
struct ModifiedType;
struct WaveClass;
struct WaveMethodSignature;
struct WaveRuntimeType {
	WaveTypeCode type;
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

