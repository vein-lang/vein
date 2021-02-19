#pragma once
#include <vector>
#include "../emit/Exceptions.hpp"
using namespace std;


template <class K>
class list_t : public vector<K>
{
public:
    // TODO, impl lazy
    template<class Z>
    [[nodiscard]] list_t<Z>* Cast() noexcept(false)
    {
        auto cp = new list_t<Z>();
        for (auto d = this->begin(); d != this->end(); ++d)
            cp->push_back(reinterpret_cast<Z>(*d));
        return cp;
    }
    // TODO, impl lazy
    [[nodiscard]] list_t<K>* Where(Predicate<K> predicate) noexcept(true)
    {
        auto cp = new list_t<K>();
        for (auto d = this->begin(); d != this->end(); ++d)
        {
            if (predicate(*d))
                cp->push_back(*d);
        }
        return cp;
    }
    [[nodiscard]] K First() noexcept(false)
    {
        return this->First([](K _) { return true; });
    }
    [[nodiscard]] K First(Predicate<K> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = [](K _) { return true; };

        for (auto d = this->begin(); d != this->end(); ++d)
        {
            if (predicate(*d))
                return *d;
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
            return this->First([](K _) { return true; });
        }
        catch (SequenceContainsNoElements)
        {
            return Nullable<K>::Value;
        }
    }
    [[nodiscard]] K Last() noexcept(false)
    {
        return this->Last([](K _) { return true; });
    }
    [[nodiscard]] K Last(Predicate<K> predicate) noexcept(false)
    {
        if (predicate == nullptr)
            predicate = [](K _) { return true; };
        for (auto t = this->rbegin(); t != this->rend(); ++t)
        {
            if (predicate(*t))
                return *t;
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
            return this->Last([](K _) { return true; });
        }
        catch (SequenceContainsNoElements)
        {
            return Nullable<K>::Value;
        }
    }

    void AddRange(list_t<K>* other) noexcept
    {
        for (auto d = other->begin(); d != other->end(); ++d)
            this->push_back(*d);
    }
};