#include "core.h"
#include "stack.h"
#include "interp.h"


enum {
  #include "../metadata/opcodes.def"
	LAST = 0xff
};

enum {
	VAL_I32,
	VAL_I64,
	VAL_DOUBLE,
	VAL_OBJ
};

typedef struct {
	union {
		uint32_t i;
		long l;
		double f;
		size_t p;
	} data;
	int type;
} stackval;


auto global_stack = Stack<ulong>(1024);
void setup() {
  d_init();
  Serial.print("NOP");
  deconstruct(0x123456789);
  pinMode(LED_BUILTIN, OUTPUT);
}

// the loop function runs over and over again forever
void loop() {
  //auto input = Serial.readStringUntil('\n');
  //Serial.println(hex_cast<ulong>(input));
}


#define CASE(x) case x:\
    d_print("@"); \
    d_print(#x); \
    d_print("\n");


template<class T>
T cast_t(void* v)
{
  return (T)v;
}
#define GET_NANI(sp) ((uint32_t)(sp).data.i)

void exec(unsigned char i, MetaMethodHeader* mh)
{
  stackval* stack = cast_t<stackval*>(__builtin_alloca(sizeof(stackval) * mh->max_stack));
  register stackval *sp = stack;
  register unsigned char* ip = mh->code;

  unsigned char *end = ip + mh->code_size;

  stackval locals[16];
  switch (i)
  {
    CASE(NOP)
      __ASM volatile ("nop");
      break;
    CASE(ADD)
      ++ip;
			--sp;
			if (sp->type == VAL_I32)
				sp[-1].data.i *= GET_NANI(sp[0]);
			else if (sp->type == VAL_I64)
				sp[-1].data.l *= sp[0].data.l;
			else if (sp->type == VAL_DOUBLE)
				sp[-1].data.f *= sp[0].data.f;
      break;
    default:
      break;
  }
  
}