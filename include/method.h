#pragma once
#include "types.h"
#include "eq.h"
#include "image.h"


void wave_metadata_decode_row(metadata_tableinfo_t* t, int idx, uint32_t* res, int res_size)
{
	auto bitfield = t->size_bitfield;
	const int count = meta_table_count(bitfield);
	auto* data = t->base + idx * t->row_size;


	for (auto i = 0; i < count; i++) {
		const int n = meta_table_size(bitfield, i);

		switch (n) {
		case 1:
			res[i] = *data; break;
		case 2:
			res[i] = read16(data); break;

		case 4:
			res[i] = read32(data); break;

		default:
			break;
		}
		data += n;
	}
}
const char* wave_metadata_blob_heap(metadata_t* meta, uint32_t index)
{
	if (!(index < meta->heap_blob.sh_size))
		return "";
	return meta->raw_metadata + meta->heap_blob.sh_offset + index;
}

uint32_t wave_metadata_decode_blob_size(const char* xptr, const char** rptr)
{
	const auto* ptr = reinterpret_cast<const unsigned char*>(xptr);
	uint32_t size;

	if ((*ptr & 0x80) == 0) {
		size = ptr[0] & 0x7f;
		ptr++;
	}
	else if ((*ptr & 0x40) == 0) {
		size = ((ptr[0] & 0x3f) << 8) + ptr[1];
		ptr += 2;
	}
	else {
		size = ((ptr[0] & 0x1f) << 24) +
			(ptr[1] << 16) +
			(ptr[2] << 8) +
			ptr[3];
		ptr += 4;
	}
	if (rptr)
		*rptr = reinterpret_cast<const char*>(ptr);
	return size;
}

WaveMethodSignature* wave_metadata_parse_method_signature(metadata_t* m, int def, const char* ptr, const char** rptr)
{
	auto* method = new0(WaveMethodSignature, 1);

	if (*ptr & 0x20)
		method->hasthis = 1;
	if (*ptr & 0x40)
		method->explicit_this = 1;
	method->call_convention = *ptr & 0x0F;
	ptr++;
	/*method->param_count = mono_metadata_decode_value(ptr, &ptr);
	method->ret = mono_metadata_parse_param(m, 1, ptr, &ptr);

	method->params = new0(WaveParam*, method->param_count);
	method->sentinelpos = -1;
	for (int i = 0; i < method->param_count; ++i) {
		if (*ptr == ELEMENT_TYPE_SENTINEL) {
			if (method->call_convention != MONO_CALL_VARARG || def)
				g_error("found sentinel for methoddef or no vararg method");
			method->sentinelpos = i;
			ptr++;
		}
		method->params[i] = mono_metadata_parse_param(m, 0, ptr, &ptr);
	}*/

	if (rptr)
		*rptr = ptr;
	return method;
}

WaveMethod* wave_get_method(WaveImage* image, uint32_t token)
{
	WaveMethod* result;
	int table = wave_metadata_token_table(token);
	int index = wave_metadata_token_index(token);
	auto* tables = image->metadata.tables;

	const char* loc;
	const char* sig = nullptr;
	uint32_t cols[6];


	if (table == META_TABLE_METHOD && ((result = static_cast<WaveMethod*>(hash_table_lookup(image->method_cache, INT_TO_PTR(token))))))
		return result;


	result = new0(WaveMethod, 1);
	result->image = image;

	wave_metadata_decode_row(&tables[table], index - 1, cols, 6);
	result->name_idx = cols[3];

	if (!sig) /* already taken from the methodref */
		sig = wave_metadata_blob_heap(&image->metadata, cols[4]);
	/*int size = wave_metadata_decode_blob_size(sig, &sig);
	result->signature = mono_metadata_parse_method_signature(&image->metadata, 0, sig, NULL);

	result->flags = cols[2];

	if (cols[2] & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		fill_pinvoke_info(image, result, index, cols[1]);
	}

	g_hash_table_insert(image->method_cache, INT_TO_PTR(token), result);*/

	return result;
}