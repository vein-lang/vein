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
  VAL_FLOAT,
	VAL_OBJ
};

typedef struct {
	union {
		int i;
		long l;
    struct {
			int lo;
			int hi;
		} pair;
		float f_r4;
		double f;
		size_t p;
	} data;
	int type;
} stackval;

void exec(MetaMethodHeader* mh, stackval *args);
void setup() {
  d_init();
  deconstruct(0x123456789);
  pinMode(LED_BUILTIN, OUTPUT);


  auto code_size = 5;
  auto code = new unsigned char[code_size];

  code[0] = NOP;
  code[1] = LDARG_0;
  code[2] = LDARG_1;
  code[3] = ADD;
  code[4] = DUMP_0;

  auto meta = new MetaMethodHeader();
  meta->max_stack = 24;
  meta->code_size = code_size;
  meta->code = code;

  auto args = new stackval[2];
  args[0].type = VAL_I32;
  args[0].data.i = 12;
  args[1].type = VAL_I32;
  args[1].data.i = 14;
  exec_method(meta, args);
}

void loop() {
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


#define AOperation(op) ++ip; \
			--sp; \
      if (sp->type == VAL_I32) \
        sp[-1].data.i op sp[0].data.i; \
			else if (sp->type == VAL_I64) \
				sp[-1].data.l op sp[0].data.l; \
			else if (sp->type == VAL_DOUBLE) \
				sp[-1].data.f op sp[0].data.f; \
      else if (sp->type == VAL_FLOAT) \
				sp[-1].data.f_r4 op sp[0].data.f_r4

void exec_method(MetaMethodHeader* mh, stackval *args)
{
  w_print("@exec::");
  stackval* stack = cast_t<stackval*>(__builtin_alloca(sizeof(stackval) * mh->max_stack));
  register stackval *sp = stack;
  register unsigned char* ip = mh->code;

  unsigned char *end = ip + mh->code_size;

  stackval locals[16];
  while(1)
  switch (*ip)
  {
    CASE(NOP)
      __ASM volatile ("nop");
      ++ip;
      break;
    CASE(ADD)
      AOperation(+=);
      break;
    CASE(SUB)
			AOperation(-=);
      break;
    CASE(MUL)
			AOperation(*=);
      break;
    CASE(DIV)
			AOperation(/=);
      break;
    CASE(LDARG_0)
    CASE(LDARG_1)
    CASE(LDARG_2)
    CASE(LDARG_3)
    CASE(LDARG_4)
      *sp = args[(*ip)-LDARG_0];
			++sp;
			++ip;
      break;
    CASE(LDC_I32_0)
      ++ip;
			sp->type = VAL_I32;
			sp->data.i = -1;
			++sp;
      break;
    CASE(DUMP_0)
      ++ip;
      f_print(sp[-1].type);
      f_print(sp[-1].data.i);
    CASE(HALT)
      ++ip;
      return;
    default:
      d_print("Unimplemented opcode: ");
      d_print(*ip);
      d_print("\n");
      return;
  }
  
}