/* Copyright Yuuki Wesp and other Vein Runtime contributors. All rights reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */



// !!!! NOT COMPLETED !!!!


#ifndef ISHTAR_H
#define ISHTAR_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdint.h>

typedef void ishtar_str;

typedef enum {
    // Nope operation.
    NOP = 0x00,
    // Add operation.
    ADD = 0x01,
    // Substract operation.
    SUB = 0x02,
    // Divide operation.
    DIV = 0x03,
    // Multiple operation.
    MUL = 0x04,
    // Modulo operation.
    MOD = 0x05,
    // Load into stack from argument.
    LDARG_0 = 0x06,
    // Load into stack from argument.
    LDARG_1 = 0x07,
    // Load into stack from argument.
    LDARG_2 = 0x08,
    // Load into stack from argument.
    LDARG_3 = 0x09,
    // Load into stack from argument.
    LDARG_4 = 0x0A,
    // Load into stack from argument.
    LDARG_5 = 0x0B,
    // Load into stack from argument by index.
    LDARG_S = 0x0C,
    // Stage into argument from stack.
    STARG_0 = 0x0D,
    // Stage into argument from stack.
    STARG_1 = 0x0E,
    // Stage into argument from stack.
    STARG_2 = 0x0F,
    // Stage into argument from stack.
    STARG_3 = 0x10,
    // Stage into argument from stack.
    STARG_4 = 0x11,
    // Stage into argument from stack.
    STARG_5 = 0x12,
    // Stage into argument from stack by index.
    STARG_S = 0x13,
    // Load constant into stack.
    LDC_F4 = 0x14,
    // Load constant into stack.
    LDC_F2 = 0x15,
    // Load constant into stack.
    LDC_STR = 0x16,
    // Load int32 constant into stack.
    LDC_I4_0 = 0x17,
    // Load int32 constant into stack.
    LDC_I4_1 = 0x18,
    // Load int32 constant into stack.
    LDC_I4_2 = 0x19,
    // Load int32 constant into stack.
    LDC_I4_3 = 0x1A,
    // Load int32 constant into stack.
    LDC_I4_4 = 0x1B,
    // Load int32 constant into stack.
    LDC_I4_5 = 0x1C,
    // Load int32 constant into stack.
    LDC_I4_S = 0x1D,
    // Load int16 constant into stack.
    LDC_I2_0 = 0x1E,
    // Load int16 constant into stack.
    LDC_I2_1 = 0x1F,
    // Load int16 constant into stack.
    LDC_I2_2 = 0x20,
    // Load int16 constant into stack.
    LDC_I2_3 = 0x21,
    // Load int16 constant into stack.
    LDC_I2_4 = 0x22,
    // Load int16 constant into stack.
    LDC_I2_5 = 0x23,
    // Load int16 constant into stack.
    LDC_I2_S = 0x24,
    // Load int64 constant into stack.
    LDC_I8_0 = 0x25,
    // Load int64 constant into stack.
    LDC_I8_1 = 0x26,
    // Load int64 constant into stack.
    LDC_I8_2 = 0x27,
    // Load int64 constant into stack.
    LDC_I8_3 = 0x28,
    // Load int64 constant into stack.
    LDC_I8_4 = 0x29,
    // Load int64 constant into stack.
    LDC_I8_5 = 0x2A,
    // Load int64 constant into stack.
    LDC_I8_S = 0x2B,
    // Load float64 constant into stack.
    LDC_F8 = 0x2C,
    // Load float128 constant into stack.
    LDC_F16 = 0x2D,
    // Reserved operation.
    RESERVED_0 = 0x2E,
    // Reserved operation.
    RESERVED_1 = 0x2F,
    // Reserved operation.
    RESERVED_2 = 0x30,
    // Return operation.
    RET = 0x31,
    // Call operation.
    CALL = 0x32,
    // Load NULL into stack.
    LDNULL = 0x33,
    // Load value from field in instance into stack.
    LDF = 0x34,
    // Load value from static field into stack.
    LDSF = 0x35,
    // Stage into instance field value from stack.
    STF = 0x36,
    // Stage into static field value from stack.
    STSF = 0x37,
    // Load from locals into stack.
    LDLOC_0 = 0x38,
    // Load from locals into stack.
    LDLOC_1 = 0x39,
    // Load from locals into stack.
    LDLOC_2 = 0x3A,
    // Load from locals into stack.
    LDLOC_3 = 0x3B,
    // Load from locals into stack.
    LDLOC_4 = 0x3C,
    // Load from locals into stack.
    LDLOC_5 = 0x3D,
    LDLOC_S = 0x3E,
    // Load from stack into locals.
    STLOC_0 = 0x3F,
    // Load from stack into locals.
    STLOC_1 = 0x40,
    // Load from stack into locals.
    STLOC_2 = 0x41,
    // Load from stack into locals.
    STLOC_3 = 0x42,
    // Load from stack into locals.
    STLOC_4 = 0x43,
    // Load from stack into locals.
    STLOC_5 = 0x44,
    STLOC_S = 0x45,
    // Initialization locals stack.
    LOC_INIT = 0x46,
    // (part of LOD.INIT) Initialization locals slot as derrived type.
    LOC_INIT_X = 0x47,
    // Duplicate memory from stack.
    DUP = 0x48,
    // Pop value from stack.
    POP = 0x69,
    // Allocate memory block.
    ALLOC_BLOCK = 0x6A,
    // Leave from protected zone.
    SEH_LEAVE_S = 0x6C,
    // Leave from protected zone.
    SEH_LEAVE = 0x6D,
    // End of finally statement.
    SEH_FINALLY = 0x6E,
    // End of filter statement.
    SEH_FILTER = 0x6F,
    // Enter protected zone.
    SEH_ENTER = 0x70,
    // Free memory at point in stack.
    DELETE = 0x6B,
    // XOR Operation.
    XOR = 0x49,
    // OR Operation.
    OR = 0x4A,
    // AND Operation.
    AND = 0x4B,
    // Shift Right Operation.
    SHR = 0x4C,
    // Shift Left Operation.
    SHL = 0x4D,
    // Convertation operation.
    CONV_R4 = 0x4E,
    // Convertation operation.
    CONV_R8 = 0x4F,
    // Convertation operation.
    CONV_I4 = 0x50,
    // Throw exception operation.
    THROW = 0x51,
    // New object Operation.
    NEWOBJ = 0x52,
    // Cast T1 to T2
    CAST = 0x71,
    // Allocate array onto evaluation stack by specified size and type.
    NEWARR = 0x53,
    // Load size of Array onto evaluation stack.
    LDLEN = 0x54,
    LDELEM_S = 0x55,
    STELEM_S = 0x56,
    // Load type token.
    LD_TYPE = 0x57,
    // Compare two value, when first value is less than or equal to second value stage 1 (int32) into stack, otherwise 0 (int32). (a <= b)
    EQL_LQ = 0x58,
    // Compare two value, when first value is less second value stage 1 (int32) into stack, otherwise 0 (int32). (a < b)
    EQL_L = 0x59,
    // Compare two value, when first value is greater than or equal to second value stage 1 (int32) into stack, otherwise 0 (int32). (a >= b)
    EQL_HQ = 0x5A,
    // Compare two value, when first value is greater second value stage 1 (int32) into stack, otherwise 0 (int32). (a > b)
    EQL_H = 0x5B,
    // Compare two value, when two integer/float is equal stage 1 (int32) into stack, otherwise 0 (int32). (a == b)
    EQL_NQ = 0x5C,
    // Compare two value, when two integer/float is not equal stage 1 (int32) into stack, otherwise 0 (int32). (a != b)
    EQL_NN = 0x5D,
    // Compare two value, when value has false, null or zero stage 1 (int32) into stack, otherwise 0 (int32). (!a)
    EQL_F = 0x5E,
    // Compare two value, when value has true or either differs from null or from zero stage 1 (int32) into stack, otherwise 0 (int32). (a)
    EQL_T = 0x5F,
    // Control flow, jump onto label. (unconditional)
    JMP = 0x60,
    // Control flow, jump onto label when first value is less than or equal to second value. (a <= b)
    JMP_LQ = 0x61,
    // Control flow, jump onto label when first value is less second value. (a < b)
    JMP_L = 0x62,
    // Control flow, jump onto label when first value is greater than or equal to second value. (a >= b)
    JMP_HQ = 0x63,
    // Control flow, jump onto label when first value is greater second value. (a > b)
    JMP_H = 0x64,
    // Control flow, jump onto label when two integer/float is equal. (a == b)
    JMP_NQ = 0x65,
    // Control flow, jump onto label when two integer/float is not equal. (a != b)
    JMP_NN = 0x66,
    // Control flow, jump onto label when value has false, null or zero. (!a)
    JMP_F = 0x67,
    // Control flow, jump onto label when value has true or either differs from null or from zero. (a)
    JMP_T = 0x68,
} ishtar_opcode_t;

typedef enum {
    TYPE_NONE = 0x0,
    TYPE_VOID,
    TYPE_OBJECT,
    TYPE_BOOLEAN,
    TYPE_CHAR,
    TYPE_I1, /* sbyte  */
    TYPE_U1, /* byte   */
    TYPE_I2, /* short  */
    TYPE_U2, /* ushort */
    TYPE_I4, /* int32  */
    TYPE_U4, /* uint32 */
    TYPE_I8, /* long   */
    TYPE_U8, /* ulong  */
    TYPE_R2, /* half  */
    TYPE_R4, /* float  */
    TYPE_R8, /* double */
    TYPE_R16, /* decimal */
    TYPE_STRING, /* string */
    TYPE_CLASS, /* custom class */
    TYPE_ARRAY, /* Array<?> */
    TYPE_TOKEN, /* type token */
    TYPE_RAW, /* raw pointer */
    TYPE_FUNCTION /* function class */
} ishtar_type_code_t;

typedef enum {
    NONE = 0,
    MISSING_METHOD,
    MISSING_FIELD,
    MISSING_TYPE,
    TYPE_LOAD,
    TYPE_MISMATCH,
    MEMBER_ACCESS,
    STATE_CORRUPT,
    ASSEMBLY_COULD_NOT_LOAD,
    END_EXECUTE_MEMORY,
    OUT_OF_MEMORY,
    ACCESS_VIOLATION,
    OVERFLOW,
    OUT_OF_RANGE,
    NATIVE_LIBRARY_COULD_NOT_LOAD,
    NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND,
    MEMORY_LEAK,
    JIT_ASM_GENERATOR_TYPE_FAULT,
    JIT_ASM_GENERATOR_INCORRECT_CAST,
    GC_MOVED_UNMOVABLE_MEMORY,
    PROTECTED_ZONE_LABEL_CORRUPT,
    SEMAPHORE_FAILED,
    THREAD_STATE_CORRUPTED
} ishtar_error_t;

typedef enum {
    RED = 0,
    GREEN,
    YELLOW
} ishtar_gc_color_t;

typedef enum {
    NONE = 0,
    NATIVE_REF = 1 << 1,
    IMMORTAL = 1 << 2
} ishtar_gc_flags_t;


typedef enum {
    CREATED = 0,
    RUNNING,
    PAUSED,
    EXITED
} ishtar_thread_status_t;

typedef enum {
    NONE        = 0,
    PUBLIC      = 1 << 1,
    STATIC      = 1 << 2,
    INTERNAL    = 1 << 3,
    PROTECTED   = 1 << 4,
    PRIVATE     = 1 << 5,
    ABSTRACT    = 1 << 6,
    SPECIAL     = 1 << 7,
    INTERFACE   = 1 << 8,
    ASPECT      = 1 << 9,
    UNDEFINED   = 1 << 10,
    UNRESOLVED  = 1 << 11,
    PREDEFINED  = 1 << 12,
    NOTCOMPLETED= 1 << 13,
} ishtar_class_flags_t;

typedef enum {
    NONE        = 0 << 0,
    PUBLIC      = 1 << 0,
    STATIC      = 1 << 1,
    INTERNAL    = 1 << 2,
    PROTECTED   = 1 << 3,
    PRIVATE     = 1 << 4,
    EXTERN      = 1 << 5,
    VIRTUAL     = 1 << 6,
    ABSTRACT    = 1 << 7,
    OVERRIDE    = 1 << 8,
    SPECIAL     = 1 << 9,
    ASYNC       = 1 << 10
} ishtar_method_flag_t;

typedef enum {
    NONE        = 0 << 0,
    LITERAL     = 1 << 1,
    PUBLIC      = 1 << 2,
    STATIC      = 1 << 3,
    PROTECTED   = 1 << 4,
    VIRTUAL     = 1 << 5,
    ABSTRACT    = 1 << 6,
    OVERRIDE    = 1 << 7,
    SPECIAL     = 1 << 8,
    READONLY    = 1 << 9,
    INTERNAL    = 1 << 10
} ishtar_field_flags_t;

typedef enum {
    NONE = 0,
    FILTER,
    CATCH_ANY,
    FINALLY
} ishtar_exp_mark_kind_t;

typedef enum {
    TRY = 0,
    FILTER,
    CATCH,
    FINALLY,
    DONE
} ishtar_exp_block_state_t;

typedef union {
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
    // C does not have a native decimal type
    struct {
        uint64_t low;
        uint64_t mid;
        uint64_t high;
        uint16_t sign_scale;
    } d;
    // C does not have a native Half type
    uint16_t hf;
    void* p;
} ishtar_stackval_union_t;

typedef struct {
    ishtar_stackval_union_t data;
    ishtar_type_code_t type;
} ishtar_stackval_t;

typedef struct {
    ishtar_class_t* clazz;
    void** vtable;
    ishtar_gc_flags_t flags;
    ishtar_gc_color_t color;
    uint64_t refs_size;
    uint32_t vtable_size;
} ishtar_object_t;

typedef struct ishtar_callframe_t ishtar_callframe_t;

typedef struct {
    const ishtar_callframe_t* self;
    const ishtar_callframe_t* parent;
    const ishtar_method_t* method;
    const int level;
    ishtar_stackval_t** returnvalue;
    ishtar_stackval_t* args;
    ishtar_opcode_t last_ip;
    ishtar_callframe_expt_t exception;
} ishtar_callframe_t;


typedef struct {
    ishtar_type_arg_t* _typearg;
    ishtar_class_t* _class;
} ishtar_complex_t;


typedef struct {
    ishtar_complex_t returntype;
    void* arguments;
} ishtar_method_sig_t ;

typedef struct {
    void* module_handle;
    void* symbol_handle;
    void* extern_function_declaration;
    void* jitted_wrapper;
    void* compiled_func_ref;
    bool isinternal;
} ishtar_method_native_data_t;

typedef struct {
    uint32_t code_size;
    uint32_t* code;
    int16_t max_stack;
    void* exception_handler_list;
    void* labels_map;
    void* labels;
} ishtar_method_header_t;

typedef struct  {
    ishtar_method_t* self;
    ishtar_method_header_t* header;
    ishtar_method_native_data_t native_info;
    uint64_t vtable_offset;
    const char* _name;
    const char* _rawname;
    bool _ctor_called;
    ishtar_method_flag_t flags;
    ishtar_class_t* owner;
    void* aspects;
    ishtar_method_sig_t* signature;
} ishtar_method_t;

typedef struct {
    uint32_t moduleid;
    uint32_t classid;
} ishtar_runtime_token_t;


typedef struct {
    const ishtar_str* _fullname;
    const ishtar_str* _name;
    const ishtar_str* _namespace;
    const ishtar_str* _asmname;
    const ishtar_str* _namewithns;
} ishtar_quality_name_t;

typedef struct {
    ishtar_class_t* _selfreference;

    void* methods;
    void* fields;
    void* aspects;

    void* owner;
    ishtar_class_t* parent;
    ishtar_quality_name_t* fullname;

    bool disposed;

    ishtar_type_code_t typecode;
    ishtar_class_flags_t flags;

    ishtar_runtime_token_t runtime_token;
    uint32_t id;

    uint16_t m1;
    uint16_t m2;

    bool isvalid;

    uint64_t computed_size;
    bool is_inited;
    void** vtable;
    uint64_t vtable_size;
} ishtar_class_t;

typedef struct {
 void* mem_base;
 void* reg_base;
} ishtar_gc_stack_base;


// ishtar GC
ishtar_stackval_t* ishtar_gc_allocvalue(ishtar_callframe_t* frame);
ishtar_stackval_t* ishtar_gc_allocvalue(ishtar_class_t* clazz, ishtar_callframe_t* frame);
ishtar_stackval_t* ishtar_gc_allocatestack(ishtar_callframe_t* frame, int32_t size);
void ishtar_gc_freestack(ishtar_callframe_t* frame, ishtar_stackval_t* stack, int32_t size);
void ishtar_gc_freevalue(ishtar_stackval_t* value);
void** ishtar_gc_allocvtable(uint32_t size);
ishtar_object_t* ishtar_gc_alloctypeinfoobject(ishtar_class_t* clazz, ishtar_callframe_t* frame);
ishtar_object_t* ishtar_gc_allocfieldinfoobject(void* field, ishtar_callframe_t* frame);
ishtar_object_t* ishtar_gc_allocmethodinfoobject(ishtar_method_t* method, ishtar_callframe_t* frame);
ishtar_object_t* ishtar_gc_allocobject(ishtar_class_t* clazz, ishtar_callframe_t* frame);
void ishtar_gc_freeobject(ishtar_object_t** obj, ishtar_callframe_t* frame);
void ishtar_gc_freeobject(ishtar_object_t* obj, ishtar_callframe_t* frame);
bool ishtar_gc_isalive(ishtar_object_t* obj);
void ishtar_gc_objectregisterfinalizer(ishtar_object_t* obj, void* proc, ishtar_callframe_t* frame);
void ishtar_gc_registerweaklink(ishtar_object_t* obj, void** link, bool longlive);
void ishtar_gc_unregisterweaklink(void** link, bool longlive);
long ishtar_gc_getusedmemorysize();
void ishtar_gc_collect();
void ishtar_gc_register_thread(ishtar_gc_stack_base* attr);
void ishtar_gc_unregister_thread() ;
bool ishtar_gc_get_stack_base(ishtar_gc_stack_base* attr);
// end ishtar GC


#ifdef __cplusplus
}
#endif
#endif /* ISHTAR_H */