#pragma once
#include "WaveCallConvention.hpp"
#include "WaveRuntimeType.hpp"
struct WaveMethodSignature {
	char call_convention;
	int param_count;
	int sentinelpos;
	WaveRuntimeType* ret;
	WaveRuntimeType** params;

    WaveMethodSignature() :
        call_convention(WAVE_CALL_DEFAULT),
        param_count(0),
        sentinelpos(0),
        ret(nullptr),
        params(nullptr)
    {  }
};