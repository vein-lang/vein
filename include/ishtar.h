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

#define ISHTAR_VERSION "0.30.1.2558-master+54046a5"
#define ISHTAR_VERSION_MAJOR 0
#define ISHTAR_VERSION_MINOR 30
#define ISHTAR_VERSION_PATH 1
#define ISHTAR_VERSION_COMMIT_SHA "54046a5883e57a19c4a052821705aef92b773f55"
#define ISHTAR_VERSION_BRANCH "master"
#define ISHTAR_VERSION_COMMIT_DATE "2024-07-27"

#ifdef __cplusplus
extern "C" {
#endif
#include <stdint.h>
#include <stdbool.h>
struct ishtar_class_t;
struct ishtar_object_t;
struct ishtar_callframe_exception_t;
struct ishtar_array_block_t;
struct call_frame_t;
struct RuntimeIshtarAlias_Method;
struct RuntimeIshtarAlias_Type;
struct ishtar_module_t;
struct RuntimeComplexType;
struct ishtar_task_data_t;
struct ishtar_thread_ctx_t;
struct ishtar_aspect_class_t;
struct ishtar_aspect_method_t;
struct ishtar_aspect_field_t;
struct ishtar_method_header_t;
struct ishtar_method_call_info_t;



typedef struct decimal_t { uint64_t h; uint64_t l; } decimal_t;
typedef uint16_t half_t;
typedef enum gc_flags_t {
    NONE = 0,
    NATIVE_REF = 2,
    IMMORTAL = 4,
} gc_flags_t;

typedef enum gc_color_t {
    RED = 0,
    YELLOW = 1,
    GREEN = 2,
} gc_color_t;

typedef enum ishtar_error_e {
    ISHTAR_ERR_NONE = 0,
    ISHTAR_ERR_MISSING_METHOD = 1,
    ISHTAR_ERR_MISSING_FIELD = 2,
    ISHTAR_ERR_MISSING_TYPE = 3,
    ISHTAR_ERR_TYPE_LOAD = 4,
    ISHTAR_ERR_TYPE_MISMATCH = 5,
    ISHTAR_ERR_MEMBER_ACCESS = 6,
    ISHTAR_ERR_STATE_CORRUPT = 7,
    ISHTAR_ERR_ASSEMBLY_COULD_NOT_LOAD = 8,
    ISHTAR_ERR_END_EXECUTE_MEMORY = 9,
    ISHTAR_ERR_OUT_OF_MEMORY = 10,
    ISHTAR_ERR_ACCESS_VIOLATION = 11,
    ISHTAR_ERR_OVERFLOW = 12,
    ISHTAR_ERR_OUT_OF_RANGE = 13,
    ISHTAR_ERR_NATIVE_LIBRARY_COULD_NOT_LOAD = 14,
    ISHTAR_ERR_NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND = 15,
    ISHTAR_ERR_MEMORY_LEAK = 16,
    ISHTAR_ERR_JIT_ASM_GENERATOR_TYPE_FAULT = 17,
    ISHTAR_ERR_JIT_ASM_GENERATOR_INCORRECT_CAST = 18,
    ISHTAR_ERR_GC_MOVED_UNMOVABLE_MEMORY = 19,
    ISHTAR_ERR_PROTECTED_ZONE_LABEL_CORRUPT = 20,
    ISHTAR_ERR_SEMAPHORE_FAILED = 21,
    ISHTAR_ERR_THREAD_STATE_CORRUPTED = 22,
} ishtar_error_e;

typedef enum ishtar_thread_status_e {
    THREAD_STATUS_CREATED = 0,
    THREAD_STATUS_RUNNING = 1,
    THREAD_STATUS_PAUSED = 2,
    THREAD_STATUS_EXITED = 3,
} ishtar_thread_status_e;

typedef enum ishtar_job_status_e {
    JOB_STATUS_CREATED = 0,
    JOB_STATUS_RUNNING = 1,
    JOB_STATUS_PAUSED = 2,
    JOB_STATUS_CANCELED = 3,
    JOB_STATUS_EXITED = 4,
} ishtar_job_status_e;

typedef enum x64_instruction_target_t {
    PUSH = 0,
    MOV = 1,
} x64_instruction_target_t;

typedef enum class_flag_t {
    CLASS_NONE = 0,
    CLASS_PUBLIC = 2,
    CLASS_STATIC = 4,
    CLASS_INTERNAL = 8,
    CLASS_PROTECTED = 16,
    CLASS_PRIVATE = 32,
    CLASS_ABSTRACT = 64,
    CLASS_SPECIAL = 128,
    CLASS_INTERFACE = 256,
    CLASS_ASPECT = 512,
    CLASS_UNDEFINED = 1024,
    CLASS_UNRESOLVED = 2048,
    CLASS_PREDEFINED = 4096,
    CLASS_NOTCOMPLETED = 8192,
    CLASS_AMORPHOUS = 16384,
} class_flag_t;

typedef enum field_flag_t {
    FIELD_NONE = 0,
    FIELD_LITERAL = 2,
    FIELD_PUBLIC = 4,
    FIELD_STATIC = 8,
    FIELD_PROTECTED = 16,
    FIELD_VIRTUAL = 32,
    FIELD_ABSTRACT = 64,
    FIELD_OVERRIDE = 128,
    FIELD_SPECIAL = 256,
    FIELD_READONLY = 512,
    FIELD_INTERNAL = 1024,
} field_flag_t;

typedef enum method_flags_t {
    METHOD_NONE = 0,
    METHOD_PUBLIC = 1,
    METHOD_STATIC = 2,
    METHOD_INTERNAL = 4,
    METHOD_PROTECTED = 8,
    METHOD_PRIVATE = 16,
    METHOD_EXTERN = 32,
    METHOD_VIRTUAL = 64,
    METHOD_ABSTRACT = 128,
    METHOD_OVERRIDE = 256,
    METHOD_SPECIAL = 512,
    METHOD_ASYNC = 1024,
    METHOD_GENERIC = 2048,
} method_flags_t;

typedef enum ishtar_opcode_e {
    OPCODE_NOP = 0,
    OPCODE_ADD = 1,
    OPCODE_SUB = 2,
    OPCODE_DIV = 3,
    OPCODE_MUL = 4,
    OPCODE_MOD = 5,
    OPCODE_LDARG_0 = 6,
    OPCODE_LDARG_1 = 7,
    OPCODE_LDARG_2 = 8,
    OPCODE_LDARG_3 = 9,
    OPCODE_LDARG_4 = 10,
    OPCODE_LDARG_5 = 11,
    OPCODE_LDARG_S = 12,
    OPCODE_STARG_0 = 13,
    OPCODE_STARG_1 = 14,
    OPCODE_STARG_2 = 15,
    OPCODE_STARG_3 = 16,
    OPCODE_STARG_4 = 17,
    OPCODE_STARG_5 = 18,
    OPCODE_STARG_S = 19,
    OPCODE_LDC_F4 = 20,
    OPCODE_LDC_F2 = 21,
    OPCODE_LDC_STR = 22,
    OPCODE_LDC_I4_0 = 23,
    OPCODE_LDC_I4_1 = 24,
    OPCODE_LDC_I4_2 = 25,
    OPCODE_LDC_I4_3 = 26,
    OPCODE_LDC_I4_4 = 27,
    OPCODE_LDC_I4_5 = 28,
    OPCODE_LDC_I4_S = 29,
    OPCODE_LDC_I2_0 = 30,
    OPCODE_LDC_I2_1 = 31,
    OPCODE_LDC_I2_2 = 32,
    OPCODE_LDC_I2_3 = 33,
    OPCODE_LDC_I2_4 = 34,
    OPCODE_LDC_I2_5 = 35,
    OPCODE_LDC_I2_S = 36,
    OPCODE_LDC_I8_0 = 37,
    OPCODE_LDC_I8_1 = 38,
    OPCODE_LDC_I8_2 = 39,
    OPCODE_LDC_I8_3 = 40,
    OPCODE_LDC_I8_4 = 41,
    OPCODE_LDC_I8_5 = 42,
    OPCODE_LDC_I8_S = 43,
    OPCODE_LDC_F8 = 44,
    OPCODE_LDC_F16 = 45,
    OPCODE_RESERVED_0 = 46,
    OPCODE_RESERVED_1 = 47,
    OPCODE_RESERVED_2 = 48,
    OPCODE_RET = 49,
    OPCODE_CALL = 50,
    OPCODE_LDNULL = 51,
    OPCODE_LDF = 52,
    OPCODE_LDSF = 53,
    OPCODE_STF = 54,
    OPCODE_STSF = 55,
    OPCODE_LDLOC_0 = 56,
    OPCODE_LDLOC_1 = 57,
    OPCODE_LDLOC_2 = 58,
    OPCODE_LDLOC_3 = 59,
    OPCODE_LDLOC_4 = 60,
    OPCODE_LDLOC_5 = 61,
    OPCODE_LDLOC_S = 62,
    OPCODE_STLOC_0 = 63,
    OPCODE_STLOC_1 = 64,
    OPCODE_STLOC_2 = 65,
    OPCODE_STLOC_3 = 66,
    OPCODE_STLOC_4 = 67,
    OPCODE_STLOC_5 = 68,
    OPCODE_STLOC_S = 69,
    OPCODE_LOC_INIT = 70,
    OPCODE_LOC_INIT_X = 71,
    OPCODE_DUP = 72,
    OPCODE_XOR = 73,
    OPCODE_OR = 74,
    OPCODE_AND = 75,
    OPCODE_SHR = 76,
    OPCODE_SHL = 77,
    OPCODE_CONV_R4 = 78,
    OPCODE_CONV_R8 = 79,
    OPCODE_CONV_I4 = 80,
    OPCODE_THROW = 81,
    OPCODE_NEWOBJ = 82,
    OPCODE_NEWARR = 83,
    OPCODE_LDLEN = 84,
    OPCODE_LDELEM_S = 85,
    OPCODE_STELEM_S = 86,
    OPCODE_LD_TYPE = 87,
    OPCODE_EQL_LQ = 88,
    OPCODE_EQL_L = 89,
    OPCODE_EQL_HQ = 90,
    OPCODE_EQL_H = 91,
    OPCODE_EQL_NQ = 92,
    OPCODE_EQL_NN = 93,
    OPCODE_EQL_F = 94,
    OPCODE_EQL_T = 95,
    OPCODE_JMP = 96,
    OPCODE_JMP_LQ = 97,
    OPCODE_JMP_L = 98,
    OPCODE_JMP_HQ = 99,
    OPCODE_JMP_H = 100,
    OPCODE_JMP_NQ = 101,
    OPCODE_JMP_NN = 102,
    OPCODE_JMP_F = 103,
    OPCODE_JMP_T = 104,
    OPCODE_POP = 105,
    OPCODE_ALLOC_BLOCK = 106,
    OPCODE_DELETE = 107,
    OPCODE_SEH_LEAVE_S = 108,
    OPCODE_SEH_LEAVE = 109,
    OPCODE_SEH_FINALLY = 110,
    OPCODE_SEH_FILTER = 111,
    OPCODE_SEH_ENTER = 112,
    OPCODE_CAST = 113,
    OPCODE_CALL_SP = 114,
    OPCODE_LDFN = 115,
    OPCODE_LD_TYPE_G = 116,
} ishtar_opcode_e;

typedef struct ishtar_version_t {
    uint32_t major;
    uint32_t minor;
    uint32_t patch;
    uint32_t build;
} ishtar_version_t;

typedef struct ishtar_array_block_t {
    uint64_t offset_value;
    uint64_t offset_block;
    uint64_t offset_rank;
    uint64_t offset_size;
} ishtar_array_block_t;

typedef struct ishtar_array_t {
    struct ishtar_class_t* clazz;
    struct ishtar_object_t* memory;
    gc_flags_t flags;
    uint32_t vtable_size;
    void* owner;
    int64_t __gc_id;
    struct ishtar_class_t* element_clazz;
    struct ishtar_array_block_t _block;
} ishtar_array_t;


typedef struct ishtar_frames_t {
    struct call_frame_t* module_loader_frame;
    struct call_frame_t* entry_point;
    struct call_frame_t* jit;
    struct call_frame_t* garbage_collector;
    struct call_frame_t* native_loader;
} ishtar_frames_t;

typedef struct ishtar_object_t {
    struct ishtar_class_t* clazz;
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

typedef struct ishtar_method_call_info_t {
    size_t module_handle;
    size_t symbol_handle;
    void* extern_function_declaration;
    void* jitted_wrapper;
    size_t compiled_func_ref;
    bool is_internal;
} ishtar_method_call_info_t;
typedef struct ishtar_method_t {
    void* _self;
    struct ishtar_method_header_t* header;
    struct ishtar_method_call_info_t p_i_info;
    uint64_t vtable_offset;
    struct ishtar_string_t* _name;
    struct ishtar_string_t* _raw_name;
    bool _ctor_called;
    method_flags_t flags;
    struct ishtar_class_t* owner;
    void* aspects;
    void* signature;
} ishtar_method_t;

typedef struct ishtar_callframe_exception_t {
    void* last_ip;
    struct ishtar_object_t* value;
    struct ishtar_string_t* stack_trace;
} ishtar_callframe_exception_t;

typedef struct call_frame_t {
    struct call_frame_t* self;
    struct call_frame_t* parent;
    struct ishtar_method_t* method;
    int32_t level;
    void* return_value;
    void* args;
    ishtar_opcode_e last_ip;
    struct ishtar_callframe_exception_t exception;
} call_frame_t;



typedef struct ishtar_illabel_t {
    int32_t pos;
    ishtar_opcode_e opcode;
} ishtar_illabel_t;

typedef struct ishtar_types_t {
    struct ishtar_class_t* object_class;
    struct ishtar_class_t* value_type_class;
    struct ishtar_class_t* void_class;
    struct ishtar_class_t* string_class;
    struct ishtar_class_t* byte_class;
    struct ishtar_class_t* s_byte_class;
    struct ishtar_class_t* int32_class;
    struct ishtar_class_t* int16_class;
    struct ishtar_class_t* int64_class;
    struct ishtar_class_t* u_int32_class;
    struct ishtar_class_t* u_int16_class;
    struct ishtar_class_t* u_int64_class;
    struct ishtar_class_t* half_class;
    struct ishtar_class_t* float_class;
    struct ishtar_class_t* double_class;
    struct ishtar_class_t* decimal_class;
    struct ishtar_class_t* bool_class;
    struct ishtar_class_t* char_class;
    struct ishtar_class_t* array_class;
    struct ishtar_class_t* exception_class;
    struct ishtar_class_t* raw_class;
    struct ishtar_class_t* aspect_class;
    struct ishtar_class_t* function_class;
    struct ishtar_class_t* range_class;
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


typedef struct RuntimeIshtarAlias_Method {
    void* name;
    void* method;
} RuntimeIshtarAlias_Method;
typedef struct RuntimeIshtarAlias_Type {
    void* name;
    struct ishtar_class_t* _class;
} RuntimeIshtarAlias_Type;

typedef struct RuntimeIshtarAlias {
    union { /* Offset: 0 */
        uint8_t kind;
    };
    union { /* Offset: 1 */
        RuntimeIshtarAlias_Method method;
        RuntimeIshtarAlias_Type type;
    };
} RuntimeIshtarAlias;





typedef struct ishtar_class_t {
    void* _self_reference;
    void* methods;
    void* fields;
    void* aspects;
    struct ishtar_module_t* owner;
    struct ishtar_class_t* parent;
    void* full_name;
    bool _is_disposed;
    int32_t type_code;
    class_flag_t flags;
    struct ishtar_rtoken_t runtime_token;
    uint32_t i_d;
    uint16_t magic1;
    uint16_t magic2;
    uint64_t computed_size;
    bool is_inited;
    void* vtable;
    uint64_t vtable_size;
} ishtar_class_t;

typedef struct RuntimeComplexType {
    void* _type_arg;
    struct ishtar_class_t* _class;
} RuntimeComplexType;

typedef struct RuntimeIshtarField {
    struct ishtar_class_t* owner;
    struct RuntimeComplexType field_type;
    uint64_t vtable_offset;
    field_flag_t flags;
    void* full_name;
    void* aspects;
    struct ishtar_object_t* default_value;
    void* _self_ref;
} RuntimeIshtarField;

typedef struct RuntimeMethodArgument {
    struct RuntimeComplexType type;
    struct ishtar_string_t* name;
    void* self;
} RuntimeMethodArgument;

typedef struct RuntimeIshtarTypeArg {
    struct ishtar_string_t* id;
    struct ishtar_string_t* name;
    void* constraints;
} RuntimeIshtarTypeArg;

typedef struct IshtarParameterConstraint {
    int32_t kind;
    struct ishtar_class_t* type;
} IshtarParameterConstraint;



typedef struct RuntimeIshtarSignature {
    struct RuntimeComplexType return_type;
    void* arguments;
} RuntimeIshtarSignature;



typedef struct rawval_union {
    union { /* Offset: 0 */
        struct ishtar_method_t* m;
        struct ishtar_class_t* c;
    };
} rawval_union;

typedef struct rawval {
    struct rawval_union data;
    int32_t type;
} rawval;
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
typedef struct stackval {
    struct stack_union data;
    int32_t type;
} stackval;



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

typedef struct LLVMContext {
    void* _ctx;
    void* _ffi_module;
    void* _execution_engine;
} LLVMContext;

typedef struct ishtar_threading_t {
    void* threads;
} ishtar_threading_t;

typedef struct vm_t {
    struct ishtar_string_t* name;
    struct ishtar_frames_t* frames;
    ishtar_trace_t trace;
    struct LLVMContext jitter;
    struct ishtar_types_t* types;
    struct ishtar_threading_t threading;
    struct ishtar_scheduler_t* task_scheduler;
    struct ishtar_module_t* internal_module;
    struct ishtar_class_t* internal_class;
} vm_t;



typedef struct vm_applet {
    void* _module;
} vm_applet;

typedef struct comparer_applet {
} comparer_applet;

typedef struct ishtar_thread_raw_t {
    struct ishtar_module_t* main_module;
    void* thread_id;
    void* call_frame;
    ishtar_string_t* name;
} ishtar_thread_raw_t;

typedef struct ishtar_task_t {
    uint64_t index;
    struct ishtar_task_data_t* data;
    call_frame_t* frame;
} ishtar_task_t;

typedef struct ishtar_thread_t {
    struct ishtar_thread_ctx_t* ctx;
    void* thread_id;
    struct call_frame_t* call_frame;
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
    struct ishtar_job_ctx_t* ctx;
    void* worker_id;
    struct call_frame_t* call_frame;
} ishtar_job_t;



typedef struct ishtar_scheduler_t {
    uint64_t task_index;
    void* async_header;
    size_t loop;
    void* _queue;
} ishtar_scheduler_t;

typedef struct RuntimeQualityTypeName {
    struct ishtar_string_t* full_name;
    struct ishtar_string_t* _fullname;
    struct ishtar_string_t* _name;
    struct ishtar_string_t* _namespace;
    struct ishtar_string_t* _asm_name;
    struct ishtar_string_t* _name_with_n_s;
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
    struct ishtar_module_t* _module;
    void* storage;
} RuntimeConstStorage;

typedef struct RuntimeFieldName {
    struct ishtar_string_t* full_name;
    struct ishtar_string_t* _full_name;
    struct ishtar_string_t* _name;
    struct ishtar_string_t* _class_name;
} RuntimeFieldName;

typedef struct ishtar_module_t {
    void* vault;
    uint32_t i_d;
    struct ishtar_version_t version;
    void* alias_table;
    void* class_table;
    void* deps_table;
    void* aspects_table;
    void* types_table;
    void* generics_table;
    void* fields_table;
    void* string_table;
    void* const_storage;
    struct ishtar_string_t* _name;
    struct ishtar_module_t* _self;
    struct ishtar_class_t* bootstrapper;
} ishtar_module_t;

typedef struct RuntimeAspectArgument {
    struct ishtar_aspect_t* owner;
    uint32_t index;
    struct stackval value;
    void* self;
} RuntimeAspectArgument;

typedef struct ishtar_aspect_class_t {
    struct ishtar_string_t* class_name;
} ishtar_aspect_class_t;

typedef struct ishtar_aspect_method_t {
    struct ishtar_string_t* class_name;
    struct ishtar_string_t* method_name;
} ishtar_aspect_method_t;

typedef struct ishtar_aspect_field_t {
    struct ishtar_string_t* class_name;
    struct ishtar_string_t* field_name;
} ishtar_aspect_field_t;


typedef struct ishtar_aspect_union_t {
    union { /* Offset: 0 */
        struct ishtar_aspect_class_t class_aspect;
        struct ishtar_aspect_method_t method_aspect;
        struct ishtar_aspect_field_t field_aspect;
    };
} ishtar_aspect_union_t;



typedef struct ishtar_aspect_t {
    struct ishtar_aspect_t* _self;
    struct ishtar_string_t* _name;
    struct ishtar_aspect_union_t _union;
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
