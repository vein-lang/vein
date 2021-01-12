#pragma once
#include "WaveReturnType.h"


struct WaveMethodSignature {
	char call_convention;
	int param_count;
	int sentinelpos;
	WaveReturnType* ret;
	WaveParam** params;
};