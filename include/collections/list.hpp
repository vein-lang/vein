#pragma once
#include <vector>
#include "../emit/Exceptions.hpp"
using namespace std;


template <class K>
class list : public vector<K>
{
public:
    // TODO, impl lazy
    [[nodiscard]] list<K>* Where(Predicate<K> predicate) noexcept(true)
    {
        auto cp = new list<K>();
        for(const auto k : this)
        {
            if (predicate(k))
                cp->push_back(k);
        }
        return cp;
    }
    [[nodiscard]] K First() noexcept(false)
    {
        return this->First(LAMBDA_TRUE(_));
    }
    [[nodiscard]] K First(Predicate<K> predicate) noexcept(false)
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
    [[nodiscard]] K FirstOrDefault(Predicate<K> predicate) noexcept(true)
    {
        try
        {
            return this->First(predicate);
        }
        catch (SequenceContainsNoElements)
        {
            return Nullable<K>::Value;
        }
    }
    [[nodiscard]] K FirstOrDefault() noexcept(true)
    {
        try
        {
            return this->First(LAMBDA_TRUE(_));
        }
        catch (SequenceContainsNoElements)
        {
            return Nullable<K>::Value;
        }
    }
    [[nodiscard]] K Last() noexcept(false)
    {
        return this->Last(LAMBDA_TRUE(_));
    }
    [[nodiscard]] K Last(Predicate<K> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = LAMBDA_TRUE(_);
        for (auto t = this->rbegin(); t != this->rend();)
        {
            if (predicate(t))
                return t;
        }
        throw SequenceContainsNoElements();
    }
    [[nodiscard]] K LastOrDefault(Predicate<K> predicate) noexcept(true)
    {
        try
        {
            return this->Last(predicate);
        }
        catch (SequenceContainsNoElements)
        {
            return Nullable<K>::Value;
        }
    }
    [[nodiscard]] K LastOrDefault() noexcept(true)
    {
        try
        {
            return this->Last(LAMBDA_TRUE(_));
        }
        catch (SequenceContainsNoElements)
        {
            return Nullable<K>::Value;
        }
    }
};