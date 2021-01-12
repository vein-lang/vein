#pragma once
#include "WaveClass.h"


struct WaveObject {
	WaveClass* clazz;
	WaveObject() { clazz = new WaveClass(); }
	WaveObject(WaveClass* _) {
		this->clazz = _;
	}
};
