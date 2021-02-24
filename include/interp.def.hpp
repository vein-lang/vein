#pragma once


#define ALLOC(t, x) ((t)__builtin_alloca(x))
#define OP_DEF(x, y, z) x = y,
#include "api/decimal.hpp"
#include "api/half.hpp"
#include "fmt/format.h"
using namespace dec;
using namespace std;
typedef decimal<5> WaveDecimal;



template <> struct fmt::formatter<WaveDecimal>: formatter<string_view> {
  template <typename FormatContext>
  auto format(WaveDecimal c, FormatContext& ctx) {
    return formatter<string_view>::format(toString(c), ctx);
  }
};
template <> struct fmt::formatter<half>: formatter<string_view> {
  template <typename FormatContext>
  auto format(half c, FormatContext& ctx) {
    return formatter<string_view>::format(to_string(static_cast<float>(c)), ctx);
  }
};

enum WaveOpCode {
    #include "../metadata/opcodes.def"
	LAST
};
#undef OP_DEF
#define OP_DEF(x, y, z) #x,
inline const char* opcodes [] = {
	#include "../metadata/opcodes.def"
	"LAST"
};
#undef OP_DEF
#define OP_DEF(x, y, z) z,
inline const unsigned char opcode_size [] = {
	#include "../metadata/opcodes.def"
	0
};
#undef OP_DEF

enum {
	VAL_INCORRECT,
	VAL_I8,
	VAL_I16,
	VAL_I32,
	VAL_I64,
	VAL_U8,
	VAL_U16,
	VAL_U32,
	VAL_U64,
	VAL_DOUBLE,
	VAL_FLOAT,
	VAL_DECIMAL,
	VAL_HALF,
	VAL_OBJ
};

static const char* VAL_NAMES[] = {
	"VAL_INCORRECT",
	"VAL_I8",
	"VAL_I16",
	"VAL_I32",
	"VAL_I64",
	"VAL_U8",
	"VAL_U16",
	"VAL_U32",
	"VAL_U64",
	"VAL_DOUBLE",
	"VAL_FLOAT",
	"VAL_DECIMAL",
	"VAL_HALF",
	"VAL_OBJ"
};

struct stackval {
	union {
		int8_t b;
		int16_t s;
		int32_t i;
		int64_t l;
		uint8_t ub;
		uint16_t us;
		uint32_t ui;
		uint64_t ul;
		float f_r4;
		double f;
        WaveDecimal d;
		half hf;
		size_t p;
	} data;
	int type;
};