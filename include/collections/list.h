#pragma once
#include "compatibility.types.h"
#include "eq.h"

constexpr auto MAX_ARRAY_LEN = 256;


template<typename T>
void array_copy(T* sourceArray, int sourceIndex, T* destinationArray, int destinationIndex, int length)
{
    std::copy(sourceArray + sourceIndex,
        sourceArray + sourceIndex + length,
        destinationArray + destinationIndex);
}

template<typename T>
class List
{
public:
    List()
    {
        this->items_ = new T[0];
        this->size_ = 0;
        this->version_ = 0;
        this->capacity_ = 0;
    }
    List(uint32_t capacity)
    {
        if (capacity == 0)
            this->items_ = new T[0];
        else
            this->items_ = new T[capacity];
        this->size_ = 0;
        this->capacity_ = capacity;
        this->version_ = 0;
    }

    int get_capacity() const
    {
        return capacity_;
    }

    T operator[](int index)
    {
        if (static_cast<uint32_t>(index) >= static_cast<uint32_t>(size_))
            return NULL;
        return this->items_[index];
    }
    /*void operator[](int index, T* value)
    {
        if (static_cast<uint32_t>(index) >= static_cast<uint32_t>(size_))
            return;
        this->items_[index] = value;
    }*/

    void add(T item)
    {
        if (size_ == capacity_) 
            _ensureCapacity(size_ + 1);
        this->items_[size_++] = item;
        ++this->version_;
    }

    bool remove(T item)
    {
        auto index = indexOf(item);
        if (index >= 0) 
        {
            removeAt(index);
            return true;
        }
        return false;
    }

    void removeAt(int index)
    {
        if (static_cast<uint32_t>(index) >= static_cast<uint32_t>(size_))
            return;
        size_--;
        if (index < size_)
            array_copy(this->items_, 
                index + 1, this->items_, 
                index, size_ - index);
        this->items_[size_] = NULL;
        version_++;
    }

    T find(Predicate<T> predicate)
    {
        for(auto i = 0; i != size_; i++)
            if (predicate(items_[i]))
                return items_[i];
        return nullptr;
    }

    void foreach(Action0<T> actor)
    {
        const auto ver = version_;

        for (auto i = 0; i != size_; i++)
        {
            if (ver != version_)
                break;
            actor(this->items_[i]);
        }
    }

    int indexOf(T item)
    {
        for (auto i = 0; i < size_; i++)
            if (default_equal(this->items_[i], item)) return i;
        return -1;
    }
private:
    T* items_;
    int size_;
    int capacity_;
    int version_;

    void _ensureCapacity(int min)
    {
        if (capacity_ >= min)
            return;
        auto newCapacity = capacity_ == 0 ? 10 : capacity_ * 2;
        if (static_cast<uint32_t>(newCapacity) > MAX_ARRAY_LEN) 
            newCapacity = MAX_ARRAY_LEN;
        if (newCapacity < min) 
            newCapacity = min;
        capacity_ = newCapacity;
    }
};

/*
 * public void RemoveAt(int index) {
            if ((uint)index >= (uint)_size) {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }
            Contract.EndContractBlock();
            _size--;
            if (index < _size) {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version++;
        }
 
 *
 */