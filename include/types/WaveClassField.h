#pragma once
#include "compatibility.types.h"
#include "WaveReturnType.h"

typedef struct {
	WaveFieldType* type;
	int             offset;
	uint32_t        flags;
} WaveClassField;
