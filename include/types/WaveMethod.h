#pragma once
#include "compatibility.types.h"
#include "WaveMethodHeader.h"
#include "WaveMethodPInvokeInfo.h"
#include "WaveMethodSignature.h"

typedef struct {
	const char*             name;
	uint16_t                flags;
	WaveMethodSignature*    signature;
	WaveImage*              image;
	union {
		MetaMethodHeader* header;
		WaveMethodPInvokeInfo* piinfo;
	} data;
} WaveMethod;
