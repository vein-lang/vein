#pragma once
#include "core.h"
#include "image.h"
#include "hashtable.h"

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
	char hasthis;
	char explicit_this;
	char call_convention;
	int param_count;
	int sentinelpos;
	WaveReturnType* ret;
	WaveParam** params;
};

typedef struct {
	uint16_t iflags; /* method implementation flags */
	wpointer addr;
} WaveMethodPInvokeInfo;

typedef struct {
	uint16_t            flags;
	WaveMethodSignature*signature;
	uint32_t            name_idx;
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

typedef struct {
	WaveClass *clazz;
} WaveObject;

typedef struct {
	uint32_t  sh_offset;
	uint32_t  sh_size;
} stream_header_t;

/*
 * This macro is used to extract the size of the table encoded in
 * the size_bitfield of metadata_tableinfo_t.
 */
#define meta_table_size(bitfield,table) ((((bitfield) >> ((table)*2)) & 0x3) + 1)
#define meta_table_count(bitfield) ((bitfield) >> 24)


typedef struct {
	uint32_t   rows, row_size;
	char* base;

	/*
	 * Tables contain up to 9 rows and the possible sizes of the
	 * fields in the documentation are 1, 2 and 4 bytes.  So we
	 * can encode in 2 bits the size.
	 *
	 * A 32 bit value can encode the resulting size
	 *
	 * The top eight bits encode the number of columns in the table.
	 * we only need 4, but 8 is aligned no shift required.
	 */
	uint32_t   size_bitfield;
} metadata_tableinfo_t;

typedef struct {
	char* raw_metadata;

	bool                 idx_string_wide, idx_guid_wide, idx_blob_wide;

	stream_header_t      heap_strings;
	stream_header_t      heap_us;
	stream_header_t      heap_blob;
	stream_header_t      heap_guid;
	stream_header_t      heap_tables;

	char* tables_base;

	metadata_tableinfo_t tables[64];
} metadata_t;


typedef enum {
	META_TABLE_TYPEREF,
	META_TABLE_TYPEDEF,
	META_TABLE_FIELD,
	META_TABLE_METHOD,
	META_TABLE_PARAM,
	META_TABLE_CONSTANT,
	META_TABLE_PROPERTY,
	META_TABLE_METHODIMPL,
	META_TABLE_NESTEDCLASS
} MetaTableEnum;



struct _WaveImage {
	int   ref_count;
	char* name;
	void* image_info;

	metadata_t metadata;

	/*
	 * Indexed by method tokens and typedef tokens.
	 */
	hash_table* method_cache;
	hash_table* class_cache;

	/*
	 * user_info is a public field and is not touched by the
	 * metadata engine
	 */
	void* user_info;
};