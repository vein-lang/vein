#pragma once
#include "compatibility.types.hpp"
#include "WaveImage.hpp"
#include "WaveMethodHeader.hpp"
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
