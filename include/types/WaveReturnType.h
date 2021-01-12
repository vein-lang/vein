#pragma once
#include "WaveModificator.h"


typedef struct {
	/* maybe use a union here: saves 4 bytes */
	WaveType* type; /* NULL for VOID */
	short param_attrs; /* 22.1.11 */
	char typedbyref;
	char num_modifiers;
	WaveModificator modifiers[0]; /* this may grow */
} WaveReturnType;
typedef WaveReturnType WaveParam;
typedef WaveReturnType WaveFieldType;