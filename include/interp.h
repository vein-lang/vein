#pragma once
#include "core.h"

enum {
    #include "../metadata/opcodes.def"
	LAST = 0xff
};

enum {
	VAL_I32,
	VAL_I64,
	VAL_DOUBLE,
	VAL_OBJ
};

typedef struct {
	union {
		int i;
		long l;
		double f;
		size_t p;
	} data;
	int type;
} stackval;
