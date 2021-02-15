#pragma once

struct WaveType;

struct WaveArray {
	WaveType* type;
	int rank;
	int numsizes;
	int numlobounds;
	int* sizes;
	int* lobounds;
};
