#pragma once
#include "WaveClass.h"


struct WaveObject {
	WaveClass*   clazz;
	WaveTypeEnum type;
	wpointer     data; /* to store static class data */
};
