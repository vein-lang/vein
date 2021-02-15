#pragma once
#include "compatibility.types.hpp"

typedef struct {
	uint32_t                code_size;
	uint32_t*               code;
	short                   max_stack;
	uint32_t                local_var_sig_tok;
	unsigned int            init_locals : 1;
	void* exception_handler_list;
} MetaMethodHeader;