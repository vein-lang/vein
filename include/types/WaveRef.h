#pragma once
#include "compatibility.types.hpp"
#include "WaveRuntimeType.hpp"

typedef struct {
	WaveRuntimeType* type;
	wpointer  value;
} WaveRef;
