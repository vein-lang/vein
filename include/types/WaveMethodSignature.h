#pragma once
#include "WaveRuntimeType.h"
struct WaveMethodSignature {
	char call_convention;
	int param_count;
	int sentinelpos;
	WaveRuntimeType* ret;
	WaveRuntimeType** params;
};