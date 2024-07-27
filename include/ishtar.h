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

#ifndef ISHTAR_H
#define ISHTAR_H

#define ISHTAR_VERSION "0.9.0"
#define ISHTAR_VERSION_MAJOR 0
#define ISHTAR_VERSION_MINOR 9
#define ISHTAR_VERSION_PATH 0
#define ISHTAR_VERSION_COMMIT_SHA "bdcfb0fb8b4047fd78bedea616a69e5f5b3df0d1"
#define ISHTAR_VERSION_BRANCH "master"
#define ISHTAR_VERSION_COMMIT_DATE "2024-07-27"

#ifdef __cplusplus
extern "C" {
#endif
#include <stdint.h>

typedef struct decimal_t { uint64_t h; uint64_t l; };
typedef uint16_t half_t;
typedef enum gc_flags_t {
    NONE = 0,
    NATIVE_REF = 2,
    IMMORTAL = 4,
};

typedef enum gc_color_t {
    RED = 0,
    YELLOW = 1,
    GREEN = 2,
};

typedef enum ishtar_error_e {
    NONE = 0,
    MISSING_METHOD = 1,
    MISSING_FIELD = 2,
    MISSING_TYPE = 3,
    TYPE_LOAD = 4,
    TYPE_MISMATCH = 5,
    MEMBER_ACCESS = 6,
    STATE_CORRUPT = 7,
    ASSEMBLY_COULD_NOT_LOAD = 8,
    END_EXECUTE_MEMORY = 9,
    OUT_OF_MEMORY = 10,
    ACCESS_VIOLATION = 11,
    OVERFLOW = 12,
    OUT_OF_RANGE = 13,
    NATIVE_LIBRARY_COULD_NOT_LOAD = 14,
    NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND = 15,
    MEMORY_LEAK = 16,
    JIT_ASM_GENERATOR_TYPE_FAULT = 17,
    JIT_ASM_GENERATOR_INCORRECT_CAST = 18,
    GC_MOVED_UNMOVABLE_MEMORY = 19,
    PROTECTED_ZONE_LABEL_CORRUPT = 20,
    SEMAPHORE_FAILED = 21,
    THREAD_STATE_CORRUPTED = 22,
};

typedef enum ishtar_thread_status_e {
    CREATED = 0,
    RUNNING = 1,
    PAUSED = 2,
    EXITED = 3,
};

typedef enum ishtar_job_status_e {
    CREATED = 0,
    RUNNING = 1,
    PAUSED = 2,
    CANCELED = 3,
    EXITED = 4,
};

typedef enum x64_instruction_target_t {
    PUSH = 0,
    MOV = 1,
};

typedef enum class_flag_t {
    NONE = 0,
    PUBLIC = 2,
    STATIC = 4,
    INTERNAL = 8,
    PROTECTED = 16,
    PRIVATE = 32,
    ABSTRACT = 64,
    SPECIAL = 128,
    INTERFACE = 256,
    ASPECT = 512,
    UNDEFINED = 1024,
    UNRESOLVED = 2048,
    PREDEFINED = 4096,
    NOTCOMPLETED = 8192,
    AMORPHOUS = 16384,
};

typedef enum field_flag_t {
    NONE = 0,
    LITERAL = 2,
    PUBLIC = 4,
    STATIC = 8,
    PROTECTED = 16,
    VIRTUAL = 32,
    ABSTRACT = 64,
    OVERRIDE = 128,
    SPECIAL = 256,
    READONLY = 512,
    INTERNAL = 1024,
};

typedef enum method_flags_t {
    NONE = 0,
    PUBLIC = 1,
    STATIC = 2,
    INTERNAL = 4,
    PROTECTED = 8,
    PRIVATE = 16,
    EXTERN = 32,
    VIRTUAL = 64,
    ABSTRACT = 128,
    OVERRIDE = 256,
    SPECIAL = 512,
    ASYNC = 1024,
    GENERIC = 2048,
};

typedef enum ishtar_opcode_e {
    NOP = 0,
    ADD = 1,
    SUB = 2,
    DIV = 3,
    MUL = 4,
    MOD = 5,
    LDARG_0 = 6,
    LDARG_1 = 7,
    LDARG_2 = 8,
    LDARG_3 = 9,
    LDARG_4 = 10,
    LDARG_5 = 11,
    LDARG_S = 12,
    STARG_0 = 13,
    STARG_1 = 14,
    STARG_2 = 15,
    STARG_3 = 16,
    STARG_4 = 17,
    STARG_5 = 18,
    STARG_S = 19,
    LDC_F4 = 20,
    LDC_F2 = 21,
    LDC_STR = 22,
    LDC_I4_0 = 23,
    LDC_I4_1 = 24,
    LDC_I4_2 = 25,
    LDC_I4_3 = 26,
    LDC_I4_4 = 27,
    LDC_I4_5 = 28,
    LDC_I4_S = 29,
    LDC_I2_0 = 30,
    LDC_I2_1 = 31,
    LDC_I2_2 = 32,
    LDC_I2_3 = 33,
    LDC_I2_4 = 34,
    LDC_I2_5 = 35,
    LDC_I2_S = 36,
    LDC_I8_0 = 37,
    LDC_I8_1 = 38,
    LDC_I8_2 = 39,
    LDC_I8_3 = 40,
    LDC_I8_4 = 41,
    LDC_I8_5 = 42,
    LDC_I8_S = 43,
    LDC_F8 = 44,
    LDC_F16 = 45,
    RESERVED_0 = 46,
    RESERVED_1 = 47,
    RESERVED_2 = 48,
    RET = 49,
    CALL = 50,
    LDNULL = 51,
    LDF = 52,
    LDSF = 53,
    STF = 54,
    STSF = 55,
    LDLOC_0 = 56,
    LDLOC_1 = 57,
    LDLOC_2 = 58,
    LDLOC_3 = 59,
    LDLOC_4 = 60,
    LDLOC_5 = 61,
    LDLOC_S = 62,
    STLOC_0 = 63,
    STLOC_1 = 64,
    STLOC_2 = 65,
    STLOC_3 = 66,
    STLOC_4 = 67,
    STLOC_5 = 68,
    STLOC_S = 69,
    LOC_INIT = 70,
    LOC_INIT_X = 71,
    DUP = 72,
    XOR = 73,
    OR = 74,
    AND = 75,
    SHR = 76,
    SHL = 77,
    CONV_R4 = 78,
    CONV_R8 = 79,
    CONV_I4 = 80,
    THROW = 81,
    NEWOBJ = 82,
    NEWARR = 83,
    LDLEN = 84,
    LDELEM_S = 85,
    STELEM_S = 86,
    LD_TYPE = 87,
    EQL_LQ = 88,
    EQL_L = 89,
    EQL_HQ = 90,
    EQL_H = 91,
    EQL_NQ = 92,
    EQL_NN = 93,
    EQL_F = 94,
    EQL_T = 95,
    JMP = 96,
    JMP_LQ = 97,
    JMP_L = 98,
    JMP_HQ = 99,
    JMP_H = 100,
    JMP_NQ = 101,
    JMP_NN = 102,
    JMP_F = 103,
    JMP_T = 104,
    POP = 105,
    ALLOC_BLOCK = 106,
    DELETE = 107,
    SEH_LEAVE_S = 108,
    SEH_LEAVE = 109,
    SEH_FINALLY = 110,
    SEH_FILTER = 111,
    SEH_ENTER = 112,
    CAST = 113,
    CALL_SP = 114,
    LDFN = 115,
    LD_TYPE_G = 116,
};

typedef struct ishtar_version_t {
    uint32_t major;
    uint32_t minor;
    uint32_t patch;
    uint32_t build;
} ishtar_version_t;

typedef struct ishtar_array_t {
    ishtar_class_t* clazz;
    ishtar_object_t* memory;
    gc_flags_t flags;
    uint32_t vtable_size;
    void* owner;
    int64_t __gc_id;
    ishtar_class_t* element_clazz;
    ishtar_array_block_t _block;
} ishtar_array_t;

typedef struct ishtar_array_block_t {
    uint64_t offset_value;
    uint64_t offset_block;
    uint64_t offset_rank;
    uint64_t offset_size;
} ishtar_array_block_t;

typedef struct ishtar_frames_t {
    call_frame_t* module_loader_frame;
    call_frame_t* entry_point;
    call_frame_t* jit;
    call_frame_t* garbage_collector;
    call_frame_t* native_loader;
} ishtar_frames_t;

typedef struct ishtar_object_t {
    ishtar_class_t* clazz;
    void* vtable;
    gc_flags_t flags;
    gc_color_t color;
    uint64_t refs_size;
    uint32_t vtable_size;
    int64_t __gc_id;
    int64_t m1;
    int64_t m2;
} ishtar_object_t;

typedef struct ishtar_ncd_t {
    size_t procedure;
    int64_t arg_count;
    size_t return_memory_pointer;
    size_t args_pointer;
} ishtar_ncd_t;

typedef struct x64_asm_step_t {
    x64_instruction_target_t instruction;
    int32_t _register;
    int32_t stack_offset;
} x64_asm_step_t;

typedef struct ishtar_rtoken_t {
    uint64_t value;
    uint32_t module_i_d;
    uint32_t class_i_d;
} ishtar_rtoken_t;

typedef struct ishtar_string_t {
    uint64_t id;
    uint64_t i_d;
} ishtar_string_t;

typedef struct call_frame_t {
    call_frame_t* self;
    call_frame_t* parent;
    ishtar_method_t* method;
    int32_t level;
    void* return_value;
    void* args;
    ishtar_opcode_e last_ip;
    ishtar_callframe_exception_t exception;
} call_frame_t;

typedef struct ishtar_callframe_exception_t {
    void* last_ip;
    ishtar_object_t* value;
    ishtar_string_t* stack_trace;
} ishtar_callframe_exception_t;

typedef struct ishtar_illabel_t {
    int32_t pos;
    ishtar_opcode_e opcode;
} ishtar_illabel_t;

typedef struct ishtar_types_t {
    ishtar_class_t* object_class;
    ishtar_class_t* value_type_class;
    ishtar_class_t* void_class;
    ishtar_class_t* string_class;
    ishtar_class_t* byte_class;
    ishtar_class_t* s_byte_class;
    ishtar_class_t* int32_class;
    ishtar_class_t* int16_class;
    ishtar_class_t* int64_class;
    ishtar_class_t* u_int32_class;
    ishtar_class_t* u_int16_class;
    ishtar_class_t* u_int64_class;
    ishtar_class_t* half_class;
    ishtar_class_t* float_class;
    ishtar_class_t* double_class;
    ishtar_class_t* decimal_class;
    ishtar_class_t* bool_class;
    ishtar_class_t* char_class;
    ishtar_class_t* array_class;
    ishtar_class_t* exception_class;
    ishtar_class_t* raw_class;
    ishtar_class_t* aspect_class;
    ishtar_class_t* function_class;
    ishtar_class_t* range_class;
    void* all;
    void* mapping;
} ishtar_types_t;

typedef struct ishtar_method_header_t {
    uint32_t code_size;
    void* code;
    int16_t max_stack;
    void* exception_handler_list;
    void* labels_map;
    void* labels;
} ishtar_method_header_t;

typedef struct ishtar_method_call_info_t {
    size_t module_handle;
    size_t symbol_handle;
    void* extern_function_declaration;
    void* jitted_wrapper;
    size_t compiled_func_ref;
    bool is_internal;
} ishtar_method_call_info_t;

typedef struct RuntimeIshtarAlias {
    union { /* Offset: 0 */
        uint8_t kind;
    };
    union { /* Offset: 1 */
        RuntimeIshtarAlias_Method method;
        RuntimeIshtarAlias_Type type;
    };
} RuntimeIshtarAlias;

typedef struct RuntimeIshtarAlias_Method {
    void* name;
    void* method;
} RuntimeIshtarAlias_Method;

typedef struct RuntimeIshtarAlias_Type {
    void* name;
    ishtar_class_t* _class;
} RuntimeIshtarAlias_Type;

typedef struct ishtar_class_t {
    ishtar_class_t* _self_reference;
    void* methods;
    void* fields;
    void* aspects;
    ishtar_module_t* owner;
    ishtar_class_t* parent;
    void* full_name;
    bool _is_disposed;
    int32_t type_code;
    class_flag_t flags;
    ishtar_rtoken_t runtime_token;
    uint32_t i_d;
    uint16_t magic1;
    uint16_t magic2;
    uint64_t computed_size;
    bool is_inited;
    void* vtable;
    uint64_t vtable_size;
} ishtar_class_t;

typedef struct RuntimeIshtarField {
    ishtar_class_t* owner;
    RuntimeComplexType field_type;
    uint64_t vtable_offset;
    field_flag_t flags;
    void* full_name;
    void* aspects;
    ishtar_object_t* default_value;
    void* _self_ref;
} RuntimeIshtarField;

typedef struct RuntimeMethodArgument {
    RuntimeComplexType type;
    ishtar_string_t* name;
    void* self;
} RuntimeMethodArgument;

typedef struct RuntimeIshtarTypeArg {
    ishtar_string_t* id;
    ishtar_string_t* name;
    void* constraints;
} RuntimeIshtarTypeArg;

typedef struct IshtarParameterConstraint {
    int32_t kind;
    ishtar_class_t* type;
} IshtarParameterConstraint;

typedef struct RuntimeComplexType {
    void* _type_arg;
    ishtar_class_t* _class;
} RuntimeComplexType;

typedef struct RuntimeIshtarSignature {
    RuntimeComplexType return_type;
    void* arguments;
} RuntimeIshtarSignature;

typedef struct ishtar_method_t {
    ishtar_method_t* _self;
    ishtar_method_header_t* header;
    ishtar_method_call_info_t p_i_info;
    uint64_t vtable_offset;
    ishtar_string_t* _name;
    ishtar_string_t* _raw_name;
    bool _ctor_called;
    method_flags_t flags;
    ishtar_class_t* owner;
    void* aspects;
    void* signature;
} ishtar_method_t;

typedef struct rawval_union {
    union { /* Offset: 0 */
        ishtar_method_t* m;
        ishtar_class_t* c;
    };
} rawval_union;

typedef struct rawval {
    rawval_union data;
    int32_t type;
} rawval;

typedef struct stackval {
    stack_union data;
    int32_t type;
} stackval;

typedef struct stack_union {
    union { /* Offset: 0 */
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
        decimal_t d;
        half_t hf;
        size_t p;
    };
} stack_union;

typedef struct ishtar_trace_t {
    bool use_console;
    bool use_file;
} ishtar_trace_t;

typedef struct RuntimeInfo {
    bool is_windows;
    bool is_linux;
    bool is_o_s_x;
    bool is_free_b_s_d;
    int32_t architecture;
} RuntimeInfo;

typedef struct vm_t {
    ishtar_string_t* name;
    ishtar_frames_t* frames;
    ishtar_trace_t trace;
    LLVMContext jitter;
    ishtar_types_t* types;
    ishtar_threading_t threading;
    ishtar_scheduler_t* task_scheduler;
    ishtar_module_t* internal_module;
    ishtar_class_t* internal_class;
} vm_t;

typedef struct LLVMContext {
    void* _ctx;
    void* _ffi_module;
    void* _execution_engine;
} LLVMContext;

typedef struct vm_applet {
    void* _module;
} vm_applet;

typedef struct comparer_applet {
} comparer_applet;

typedef struct ishtar_thread_raw_t {
    ishtar_module_t* main_module;
    void* thread_id;
    void* call_frame;
    ishtar_string_t* name;
} ishtar_thread_raw_t;

typedef struct ishtar_task_t {
    uint64_t index;
    ishtar_task_data_t* data;
    call_frame_t* frame;
} ishtar_task_t;

typedef struct ishtar_thread_t {
    ishtar_thread_ctx_t* ctx;
    void* thread_id;
    call_frame_t* call_frame;
} ishtar_thread_t;

typedef struct ishtar_thread_ctx_t {
    ishtar_thread_status_e status;
    void* thread_id;
    void* locker;
} ishtar_thread_ctx_t;

typedef struct ishtar_job_ctx_t {
    ishtar_job_status_e status;
    void* job_id;
    void* locker;
} ishtar_job_ctx_t;

typedef struct ishtar_job_t {
    ishtar_job_ctx_t* ctx;
    void* worker_id;
    call_frame_t* call_frame;
} ishtar_job_t;

typedef struct ishtar_threading_t {
    void* threads;
} ishtar_threading_t;

typedef struct ishtar_scheduler_t {
    uint64_t task_index;
    void* async_header;
    size_t loop;
    void* _queue;
} ishtar_scheduler_t;

typedef struct RuntimeQualityTypeName {
    ishtar_string_t* full_name;
    ishtar_string_t* _fullname;
    ishtar_string_t* _name;
    ishtar_string_t* _namespace;
    ishtar_string_t* _asm_name;
    ishtar_string_t* _name_with_n_s;
} RuntimeQualityTypeName;

typedef struct ProtectedZone {
    uint32_t start_addr;
    uint32_t end_addr;
    int32_t try_end_label;
    void* filter_addr;
    void* catch_addr;
    void* catch_class;
    void* types;
} ProtectedZone;

typedef struct RuntimeConstStorage {
    ishtar_module_t* _module;
    void* storage;
} RuntimeConstStorage;

typedef struct RuntimeFieldName {
    ishtar_string_t* full_name;
    ishtar_string_t* _full_name;
    ishtar_string_t* _name;
    ishtar_string_t* _class_name;
} RuntimeFieldName;

typedef struct ishtar_module_t {
    void* vault;
    uint32_t i_d;
    ishtar_version_t version;
    void* alias_table;
    void* class_table;
    void* deps_table;
    void* aspects_table;
    void* types_table;
    void* generics_table;
    void* fields_table;
    void* string_table;
    void* const_storage;
    ishtar_string_t* _name;
    ishtar_module_t* _self;
    ishtar_class_t* bootstrapper;
} ishtar_module_t;

typedef struct RuntimeAspectArgument {
    ishtar_aspect_t* owner;
    uint32_t index;
    stackval value;
    void* self;
} RuntimeAspectArgument;

typedef struct ishtar_aspect_union_t {
    union { /* Offset: 0 */
        ishtar_aspect_class_t class_aspect;
        ishtar_aspect_method_t method_aspect;
        ishtar_aspect_field_t field_aspect;
    };
} ishtar_aspect_union_t;

typedef struct ishtar_aspect_class_t {
    ishtar_string_t* class_name;
} ishtar_aspect_class_t;

typedef struct ishtar_aspect_method_t {
    ishtar_string_t* class_name;
    ishtar_string_t* method_name;
} ishtar_aspect_method_t;

typedef struct ishtar_aspect_field_t {
    ishtar_string_t* class_name;
    ishtar_string_t* field_name;
} ishtar_aspect_field_t;

typedef struct ishtar_aspect_t {
    ishtar_aspect_t* _self;
    ishtar_string_t* _name;
    ishtar_aspect_union_t _union;
    int32_t target;
    void* arguments;
} ishtar_aspect_t;

typedef struct GcHeapUsageStat {
    int64_t pheap_size;
    int64_t pfree_bytes;
    int64_t punmapped_bytes;
    int64_t pbytes_since_gc;
    int64_t ptotal_bytes;
} GcHeapUsageStat;

typedef struct GC_stack_base {
    void* mem_base;
    void* reg_base;
} GC_stack_base;

typedef struct AllocatorBlock {
    void* parent;
    void* free;
    void* realloc;
    void* alloc_with_history;
    void* alloc_primitives_with_history;
} AllocatorBlock;

typedef struct LongDoubleUnion {
    union { /* Offset: 0 */
        int64_t long_value;
        double double_value;
    };
} LongDoubleUnion;

typedef struct cast_uint {
    union { /* Offset: 0 */
        uint64_t _result;
        uint32_t _s1;
    };
    union { /* Offset: 4 */
        uint32_t _s2;
    };
} cast_uint;

typedef struct ishtar_task_data_t {
    void* semaphore;
} ishtar_task_data_t;

typedef struct WeakImmortalRef {
} WeakImmortalRef;

typedef struct ManagedMemHandle {
    size_t size;
    void* handler;
    size_t original_addr;
} ManagedMemHandle;

typedef struct HeapMemRef {
    size_t heap_handle;
    size_t mem_ptr;
    int64_t size;
} HeapMemRef;

typedef struct uv_sem_t {
    size_t handle;
} uv_sem_t;

typedef struct uv_thread_t {
    size_t handle;
} uv_thread_t;

typedef struct uv_work_t {
    size_t handle;
} uv_work_t;

extern void vm_init();
extern void execute_method(call_frame_t* frame);
extern void create_method(void* name, method_flags_t flags, ishtar_class_t* returnType, void* args);
#ifdef __cplusplus
}
#endif
#endif /*ISHTAR_H*/
