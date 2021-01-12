#pragma once
#include "WaveRuntimeType.h"


struct WaveArray {
	WaveRuntimeType* type;
	int rank;
	int numsizes;
	int numlobounds;
	int* sizes;
	int* lobounds;
};
