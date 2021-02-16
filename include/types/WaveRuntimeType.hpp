#pragma once
#include "compatibility.types.hpp"
#include "WaveArray.hpp"
#include "WaveModificator.hpp"
#include <WaveTypeCode.hpp>
#include <types/WaveModificator.hpp>
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

