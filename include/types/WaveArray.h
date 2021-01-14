#pragma once

struct WaveRuntimeType;

struct WaveArray {
	WaveRuntimeType* type;
	int rank;
	int numsizes;
	int numlobounds;
	int* sizes;
	int* lobounds;
};
