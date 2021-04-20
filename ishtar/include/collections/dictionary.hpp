#pragma once
#include <unordered_map>
#include "../emit/Exceptions.hpp"
using namespace std;

template <class K, class V>
class dictionary : public unordered_map<K, V>
{
public:
    // TODO, impl lazy
    [[nodiscard]] dictionary<K, V>* Where(Predicate<tuple<K, V>> predicate) noexcept(true)
    {
        auto cp = new dictionary<K, V>();
        for(const auto kv : this)
        {
            auto t = make_tuple(kv.first, kv.second);
            if (predicate(t))
                cp->insert(t);
        }
        return cp;
    }
    [[nodiscard]] tuple<K, V> First() noexcept(false)
    {
        return this->First([](tuple<K, V> x) { return true; });
    }
    [[nodiscard]] tuple<K, V> First(Predicate<tuple<K, V>> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = [](tuple<K, V> x) { return true; };
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
    [[nodiscard]] V GetOrDefault(K key) noexcept(true)
    {
        Predicate<tuple<K, V>> predicate = [this, key](tuple<K, V> t) {
            return std::get<0>(t) == key;
        };
        try
        {
            return std::get<1>(this->First(predicate));
        }
        catch (SequenceContainsNoElements)
        {
            return (V)Nullable<V>::Value;
        }
    }

    [[nodiscard]] tuple<K, V> FirstOrDefault() noexcept(true)
    {
        try
        {
            return this->First([](tuple<K, V> x) { return true; });
        }
        catch (SequenceContainsNoElements)
        {
            return make_tuple(Nullable<K>::Value, Nullable<V>::Value);
        }
    }
    [[nodiscard]] tuple<K, V> Last() noexcept(false)
    {
        return this->Last([](tuple<K, V> x) { return true; });
    }
    [[nodiscard]] tuple<K, V> Last(Predicate<tuple<K, V>> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = [](tuple<K, V> x) { return true; };
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
            return this->Last([](tuple<K, V> x) { return true; });
        }
        catch (SequenceContainsNoElements)
        {
            return make_tuple(Nullable<K>::Value, Nullable<V>::Value);
        }
    }
};