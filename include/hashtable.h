#pragma once
#include <cmath>
#include <cstdint>
#include <functional>


#include "compatibility.types.h"
#include "hash.h"
#include "eq.h"

typedef struct _hash_table hash_table;
typedef struct _slot slot;

struct _slot {
	void* key;
	void* value;
	slot* next;
};

static const uint32_t prime_tbl[] = {
	11, 19, 37, 73, 109, 163, 251, 367, 557, 823, 1237,
	1861, 2777, 4177, 6247, 9371, 14057, 21089, 31627,
	47431, 71143, 106721, 160073, 240101, 360163,
	540217, 810343, 1215497, 1823231, 2734867, 4102283,
	6153409, 9230113, 13845163
};

typedef uint32_t(*f_hashDelegate )     (const wpointer key);
typedef bool    (*f_equalDelegate)     (const wpointer a, const wpointer b);
typedef void    (*f_destroyEvent )     (wpointer data);
typedef bool    (*f_hashPredicate)     (wpointer key, wpointer value);

struct _hash_table
{
	f_hashDelegate      hash_func;
	f_equalDelegate     key_equal_func;

	slot** table;
	int   table_size;
	int   in_use;
	int   threshold;
	int   last_rehash;

	f_destroyEvent value_destroy_func, key_destroy_func;
};

static bool test_prime(const int x)
{
	if ((x & 1) != 0) {
        for (auto n = 3; n < static_cast<int>(sqrt(x)); n += 2) {
			if (x % n == 0)
				return false;
		}
		return true;
	}
	return (x == 2);
}

static int calc_prime(const int x)
{
    for (auto i = (x & (~1)) - 1; i < INT32_MAX; i += 2) {
		if (test_prime(i))
			return i;
	}
	return x;
}

uint32_t spaced_primes_closest(uint32_t x)
{
    for (auto i : prime_tbl)
		if (x <= i) return i;
	return calc_prime(x);
}
hash_table* hash_table_new(f_hashDelegate hash_func, f_equalDelegate key_equal_func)
{
    if (hash_func == nullptr)
		hash_func = hash_gen<wpointer>::getHashCode;
	if (key_equal_func == nullptr)
		key_equal_func = w_equal_direct;
    auto* hash = new0(hash_table, 1);

	hash->hash_func = hash_func;
	hash->key_equal_func = key_equal_func;

	hash->table_size = spaced_primes_closest(1);
	hash->table = new slot*[hash->table_size];//new0(slot*, hash->table_size);
	hash->last_rehash = hash->table_size;

	return hash;
}
uint32_t hash_table_size(hash_table* hash)
{
	if (hash == nullptr)
		return 0;
	return hash->in_use;
}

static void do_rehash(hash_table* hash)
{
    printf("Resizing diff=%d slots=%d\n", hash->in_use - hash->last_rehash, hash->table_size);
	hash->last_rehash = hash->table_size;
    const auto current_size = hash->table_size;
	hash->table_size = spaced_primes_closest(hash->in_use);
	printf("New size: %d\n", hash->table_size);
	auto** const table = hash->table;
	hash->table = new0(slot*, hash->table_size);

	for (auto i = 0; i < current_size; i++) {
		slot* next;

		for (auto* s = table[i]; s != nullptr; s = next) {
            const auto hashcode = ((*hash->hash_func) (s->key)) % hash->table_size;
			next = s->next;

			s->next = hash->table[hashcode];
			hash->table[hashcode] = s;
		}
	}
	free(table);
}

static void rehash(hash_table* hash)
{
    const auto diff = abs(hash->last_rehash - hash->in_use);

	/* These are the factors to play with to change the rehashing strategy */
	/* I played with them with a large range, and could not really get */
	/* something that was too good, maybe the tests are not that great */
	if (!(diff * 0.75 > hash->table_size * 2))
		return;
	do_rehash(hash);
}



wpointer hash_table_find(hash_table* hash, f_hashPredicate predicate)
{
    if (hash == nullptr)
		return nullptr;
	if (predicate == nullptr)
		return nullptr;

	for (auto i = 0; i <= hash->table_size; i++) {
        for (auto* s = hash->table[i]; s != nullptr; s = s->next)
			if ((*predicate)(s->key, s->value))
				return s->value;
	}
	return nullptr;
}


void hash_table_insert(hash_table* hash, wpointer key, wpointer value, bool replace)
{
    slot* s;

    if (hash == nullptr)
		return;

    const auto equal = hash->key_equal_func;
	if (hash->in_use >= hash->threshold)
		rehash(hash);

    const auto hashcode = ((*hash->hash_func)(key)) % hash->table_size;
	for (s = hash->table[hashcode]; s != nullptr; s = s->next) {
		if ((*equal) (s->key, key)) {
			if (replace) {
				if (hash->key_destroy_func != nullptr)
					(*hash->key_destroy_func)(s->key);
				s->key = key;
			}
			if (hash->value_destroy_func != nullptr)
				(*hash->value_destroy_func) (s->value);
			s->value = value;
			return;
		}
	}
	s = new0(slot, 1);
	s->key = key;
	s->value = value;
	s->next = hash->table[hashcode];
	hash->table[hashcode] = s;
	hash->in_use++;
}

bool hash_table_lookup_extended(hash_table* hash, const wpointer key, wpointer* orig_key, wpointer* value)
{
    if (hash == nullptr)
		return false;
    const auto equal = hash->key_equal_func;
    const auto hashcode = ((*hash->hash_func)(key)) % hash->table_size;

	for (auto* s = hash->table[hashcode]; s != nullptr; s = s->next) {
		if ((*equal)(s->key, key)) {
			*orig_key = s->key;
			*value = s->value;
			return true;
		}
	}
	return false;
}

wpointer hash_table_lookup(hash_table* hash, const wpointer key)
{
	wpointer orig_key, value;

	if (hash_table_lookup_extended(hash, key, &orig_key, &value))
		return value;
	else
		return nullptr;
}
