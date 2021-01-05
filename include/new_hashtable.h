#pragma once
#include "compatibility.types.h"


template<typename TKey, typename TValue>
struct _slot_ {
    const TKey& key;
    const TValue* value;

    _slot_(const TKey& k, TValue* v) : key(k), value(v) {}
};

template <typename TKey, typename TValue>
using equalDelegate = bool(const TKey key, const TValue val);


void x(equalDelegate<int, int> a)
{
    x([](const int x, const int y) { return x == y; });
}
//template<typename TKey, typename TValue>
//class hash_table_2
//{
//public:
//    std::vector<std::pmr::vector<_slot_<TKey, TValue>>> table_;
//    int size_;
//    int count_;
//
//    f__hashDelegate      hash_func;
//    f__equalDelegate     key_equal_func;
//    
//    hash_table_2(const int size, f__hashDelegate hash, f__equalDelegate eq) {
//        for (auto i = 0; i < size; i++) {
//            std::pmr::vector<_slot_<TKey, TValue>> v;
//            table_.push_back(v);
//        }
//        this->hash_func = hash;
//        this->key_equal_func = eq;
//    }
//    ~hash_table_2() {}
//
//    void set(const TKey& k, const TValue& v) {
//        _slot_<TKey, TValue> b(k, v);
//        for (auto i = 0; i < table_[hash_func((void*)k)].size(); i++)
//            if (key_equal_func((const wpointer)table_[hash_func((void*)k)][i].key, (wpointer)k)) {
//                table_[hash_func((void*)k)][i] = b;
//                return;
//            }
//        table_[hash_func((void*)k)].push_back(b);
//    }
//
//    /*TValue* get(const TKey& k) {
//        for (auto i = 0; i < table_[hash_func((void*)k)].size(); i++)
//            if (key_equal_func((const wpointer)table_[hash_func((void*)k)][i].key, (wpointer)k))
//                return table_[hash_func((void*)k)][i].value;
//    }*/
//
//    bool exist(const TKey& k) {
//        for (auto i = 0; i < table_[hash_func((void*)k)].size(); i++)
//            if (key_equal_func((const wpointer)table_[hash_func((void*)k)][i].key, (wpointer)k))
//                return true;
//        return false;
//    }
//};