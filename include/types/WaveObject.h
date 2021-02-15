#pragma once
#include "compatibility.types.hpp"
#include "WaveClass.hpp"


struct WaveObject {
	WaveClass*   clazz;
	WaveTypeCode type;
	wpointer     data; /* to store static class data */
};
