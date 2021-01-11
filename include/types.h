#pragma once
#include "image.h"
#include "collections/hashtable.h"

struct WaveArray;
struct _WaveClass;
struct _WaveType;
struct _WaveImage;

typedef _WaveType WaveType;
typedef _WaveClass WaveClass;


typedef _WaveImage WaveImage;


typedef unsigned char uchar_t;


typedef struct {
	uint32_t                code_size;
	unsigned char*          code;
	short                   max_stack;
	uint32_t                local_var_sig_tok;
	unsigned int            init_locals : 1;
	void*                   exception_handler_list;
} MetaMethodHeader;

typedef struct {
	unsigned char mod;
	uint32_t token;
} WaveModificator;
typedef struct {
	WaveType* type;
	int num_modifiers;
	WaveModificator modifiers[0];
} ModifiedType;

typedef struct {
	/* maybe use a union here: saves 4 bytes */
	WaveType* type; /* NULL for VOID */
	short param_attrs; /* 22.1.11 */
	char typedbyref;
	char num_modifiers;
	WaveModificator modifiers[0]; /* this may grow */
} WaveReturnType;
typedef WaveReturnType WaveParam;
typedef WaveReturnType WaveFieldType;

struct WaveMethodSignature {
	char call_convention;
	int param_count;
	int sentinelpos;
	WaveReturnType* ret;
	WaveParam** params;
};


typedef enum {
	WAVE_CALL_DEFAULT,
	WAVE_CALL_C,
	WAVE_CALL_STDCALL,
	WAVE_CALL_THISCALL,
	WAVE_CALL_FASTCALL,
	WAVE_CALL_VARARG
} WaveCallConvention;


typedef struct {
	uint16_t iflags;
	wpointer addr;
} WaveMethodPInvokeInfo;

typedef struct {
	const char*         name;
	uint16_t            flags;
	WaveMethodSignature*signature;
	WaveImage*          image;
	union {
		MetaMethodHeader        *header;
		WaveMethodPInvokeInfo   *piinfo;
	} data;
} WaveMethod;

typedef struct {
	WaveFieldType   *type;
	int             offset;
	uint32_t        flags;
} ClassField;



typedef struct {
	char  flags;
	char *ret_type;
	int   param_count;
	char **param;
} MethodSignature;

struct _WaveType {
	uchar_t type; /* ElementTypeEnum */
	uchar_t custom_mod; /* for PTR and SZARRAY: use data.mtype instead of data.type */
	uchar_t byref; /* when included in a MonoRetType */
	uchar_t constraint; /* valid when included in a local var signature */
	union {
		uint32_t token; /* for VALUETYPE and CLASS */
		WaveType *type;
		ModifiedType *mtype;
		WaveArray *array; /* for ARRAY */
		MethodSignature *method;
	} data;
};


struct WaveArray {
	WaveType *type;
	int rank;
	int numsizes;
	int numlobounds;
	int *sizes;
	int *lobounds;
};

struct _WaveClass {
	uint32_t   type_token;
	uint32_t   inited : 1;

	WaveClass *parent;
	
	/*
	 * Computed object instance size, total.
	 */
	int        instance_size;
	int        class_size;

	/*
	 * From the TypeDef table
	 */
	uint32_t    flags;
	struct {
		uint32_t first, last;
		int count;
	} field, method;

	/*
	 * Field information: Type and location from object base
	 */
	ClassField *fields;
	/*
	 * After the fields, there is room for the static fields...
	 */
};

struct WaveObject {
	WaveClass *clazz;
	WaveObject() { clazz = new WaveClass(); }
	WaveObject(WaveClass* _) {
		this->clazz = _;
	}
};

typedef struct {
	WaveType* type;
	wpointer  value;
} WaveRef;

struct _WaveImage {
	char* name;
	
	hashtable<nativeString>* method_cache;
	hashtable<nativeString>* class_cache;
};

typedef struct {
	WaveObject object;
	uint32_t length;
	char* chars;
} WaveString;



typedef struct
{
    
} WaveCore;