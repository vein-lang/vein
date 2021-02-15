#pragma once
#include <unordered_map>
#include "../emit/Exceptions.hpp"
using namespace std;


CUSTOM_EXCEPTION(SequenceContainsNoElements);


#define LAMBDA_TRUE(_) [](_) { return true; }

template <class K, class V>
class dictionary : public unordered_map<K, V>
{
public:
    tuple<K, V> First()
    {
        return this->First(LAMBDA_TRUE(_));
    }
    tuple<K, V> First(Predicate<tuple<K, V>> predicate)
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
    tuple<K, V> FirstOrDefault(Predicate<tuple<K, V>> predicate)
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
    tuple<K, V> FirstOrDefault()
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
    tuple<K, V> Last()
    {
        return this->Last(LAMBDA_TRUE(_));
    }
    tuple<K, V> Last(Predicate<tuple<K, V>> predicate)
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
    tuple<K, V> LastOrDefault(Predicate<tuple<K, V>> predicate)
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
    tuple<K, V> LastOrDefault()
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