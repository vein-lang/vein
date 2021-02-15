#pragma once
#include <unordered_map>
#include "../emit/Exceptions.hpp"
using namespace std;


CUSTOM_EXCEPTION(SequenceContainsNoElements);


template <class K, class V>
class dictionary : public unordered_map<K, V>
{
public:
    [[nodiscard]] tuple<K, V> First() noexcept(false)
    {
        return this->First(LAMBDA_TRUE(_));
    }
    [[nodiscard]] tuple<K, V> First(Predicate<tuple<K, V>> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = LAMBDA_TRUE(_);
        for(const auto kv : this)
        {
            auto t = make_tuple(kv.first, kv.second);
            if (predicate(t))
                return t;
            delete t;
        }
        throw SequenceContainsNoElements();
    }
    [[nodiscard]] tuple<K, V> FirstOrDefault(Predicate<tuple<K, V>> predicate) noexcept(true)
    {
        try
        {
            return this->First(predicate);
        }
        catch (SequenceContainsNoElements)
        {
            return make_tuple(Nullable<K>::Value, Nullable<V>::Value);
        }
    }
    [[nodiscard]] tuple<K, V> FirstOrDefault() noexcept(true)
    {
        try
        {
            return this->First(LAMBDA_TRUE(_));
        }
        catch (SequenceContainsNoElements)
        {
            return make_tuple(Nullable<K>::Value, Nullable<V>::Value);
        }
    }
    [[nodiscard]] tuple<K, V> Last() noexcept(false)
    {
        return this->Last(LAMBDA_TRUE(_));
    }
    [[nodiscard]] tuple<K, V> Last(Predicate<tuple<K, V>> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = LAMBDA_TRUE(_);
        for (auto iter = this->rbegin(); iter != this->rend();)
        {
            auto t = make_tuple(iter->first, iter->second);
            if (predicate(t))
                return t;
            delete t;
        }
        throw SequenceContainsNoElements();
    }
    [[nodiscard]] tuple<K, V> LastOrDefault(Predicate<tuple<K, V>> predicate) noexcept(true)
    {
        try
        {
            return this->Last(predicate);
        }
        catch (SequenceContainsNoElements)
        {
            return make_tuple(Nullable<K>::Value, Nullable<V>::Value);
        }
    }
    [[nodiscard]] tuple<K, V> LastOrDefault() noexcept(true)
    {
        try
        {
            return this->Last(LAMBDA_TRUE(_));
        }
        catch (SequenceContainsNoElements)
        {
            return make_tuple(Nullable<K>::Value, Nullable<V>::Value);
        }
    }
};