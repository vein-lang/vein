#pragma once
#include "compatibility.types.h"
#include "hash.h"
#include "eq.h"

template<typename TKey>
struct bucket {
    TKey key;
    wpointer val;
    int hash_coll;
};


template<typename T>
struct ValueType { inline static const T NullValue = NULL; };

template<typename T>
struct ValueType<T*> { inline static const T* NullValue = nullptr; };


template<typename TKey>
class hashtable
{ 
public:
    hashtable() : hashtable(0) { } 
    hashtable(int capacity) : hashtable(capacity, 1.0f) { } 
    hashtable(int capacity, float loadFactor) { 
        this->loadFactor_ = 0.72f * loadFactor; 

        auto rawsize = capacity / this->loadFactor_; 
        auto hashsize = (rawsize > 4) ? Hash::getPrime(static_cast<int>(rawsize)) : 4; 
        this->buckets_ = new bucket<TKey>[hashsize];
        for (auto i = 0; i != hashsize; i++) 
        { 
            this->buckets_[i].hash_coll = 0; 
            this->buckets_[i].key = ValueType<TKey>::NullValue;
            this->buckets_[i].val = nullptr; 
        } 
        this->loadsize_ = static_cast<int>(this->loadFactor_ * hashsize); 
        this->buckets_size_ = hashsize; 
    } 


    virtual void add(TKey key, wpointer value)
    { 
        insert(key, value, true); 
    } 

    virtual wpointer get(TKey key)
    { 
        uint32_t seed; 
        uint32_t incr; 
        
        auto hashcode = initHash(key, buckets_size_, &seed, &incr); 
        auto ntry = 0; 
        bucket<TKey> b; 
        auto bucketNumber = static_cast<int>(seed % static_cast<uint32_t>(buckets_size_)); 
        do 
        { 
            int currentversion; 
            do { 
                currentversion = version_; 
                b = buckets_[bucketNumber]; 
            } while ((currentversion != version_)); 

            if (b.key == ValueType<TKey>::NullValue)
                return nullptr; 
            if (((b.hash_coll & 0x7FFFFFFF) == hashcode) && 
                default_equal(b.key, key)) 
                return b.val; 
            bucketNumber = static_cast<int>((static_cast<long>(bucketNumber) + incr) % static_cast<uint32_t>(buckets_size_)); 
        } while (b.hash_coll < 0 && ++ntry < buckets_size_); 
        return nullptr; 
    } 
    virtual void set(TKey key, wpointer value)
    { 
        insert(key, value, false); 
    } 

private: 
    bucket<TKey>* buckets_ = new bucket<TKey>[0];
    int buckets_size_ = 0; 
    int count_ = 0; 
    int occupancy_ = 0; 
    int loadsize_ = 0; 
    float loadFactor_ = 0; 
    int version_ = 0;


    uint32_t initHash(TKey key, int hashsize, uint32_t* seed, uint32_t* incr)
    {
        auto hashcode = static_cast<uint32_t>(hash_gen<TKey>::getHashCode(key)) & 0x7FFFFFFF; 
        *seed = static_cast<uint32_t>(hashcode); 
        *incr = static_cast<uint32_t>(1 + ((*seed * HASH_PRIME) % (static_cast<uint32_t>(hashsize) - 1))); 
        return hashcode; 
    } 

    void insert(TKey key, wpointer nvalue, bool add)
    { 
        if (count_ >= loadsize_)  
            expand(); 
        else if (occupancy_ > loadsize_ && count_ > 100) 
            rehash(); 

        uint32_t seed; 
        uint32_t incr; 
        uint32_t hashcode = initHash(key, buckets_size_, &seed, &incr); 
        int ntry = 0; 
        int emptySlotNumber = -1; 
        int bucketNumber = static_cast<int>(seed % static_cast<uint32_t>(buckets_size_)); 

        do 
        { 
            if (emptySlotNumber == -1 && this->buckets_[bucketNumber].hash_coll < 0) 
                emptySlotNumber = bucketNumber; 

            if ((this->buckets_[bucketNumber].key == ValueType<TKey>::NullValue))
            { 
                if (emptySlotNumber != -1) 
                    bucketNumber = emptySlotNumber; 
                
                this->buckets_[bucketNumber].val = nvalue; 
                this->buckets_[bucketNumber].key = key; 
                this->buckets_[bucketNumber].hash_coll |= static_cast<int>(hashcode); 
                count_++; 
                version_++; 
                return; 
            } 

            if (((this->buckets_[bucketNumber].hash_coll & 0x7FFFFFFF) == hashcode) && 
                default_equal(this->buckets_[bucketNumber].key, key)) 
            { 
                if (add) 
                    return; 
                this->buckets_[bucketNumber].val = nvalue; 
                version_++; 
                return; 
            } 

            if (emptySlotNumber == -1)  
            { 
                if (this->buckets_[bucketNumber].hash_coll >= 0) { 
                    this->buckets_[bucketNumber].hash_coll |= static_cast<int>(0x80000000); 
                    occupancy_++; 
                } 
            } 

            bucketNumber = static_cast<int>((static_cast<long>(bucketNumber) + incr) % static_cast<uint32_t>(buckets_size_)); 
        } 
        while (++ntry < buckets_size_); 

        if (emptySlotNumber != -1) 
        { 
            this->buckets_[emptySlotNumber].val = nvalue; 
            this->buckets_[emptySlotNumber].key = key; 
            this->buckets_[emptySlotNumber].hash_coll |= static_cast<int>(hashcode); 
            count_++; 
            version_++; 
        } 
    } 


    void expand() 
    { 
        auto rawsize = Hash::expandPrime(buckets_size_); 
        rehash(rawsize, false); 
    } 
    void rehash() 
    { 
        rehash(buckets_size_, false); 
    } 
    void rehash(int newsize, bool forceNewHashCode) 
    { 
        occupancy_ = 0; 
        bucket<TKey>* newBuckets = new bucket<TKey>[newsize];

        for (auto nb = 0; nb < buckets_size_; nb++)  
        { 
            auto oldb = this->buckets_[nb]; 
            if (oldb.key != ValueType<TKey>::NullValue)
            { 
                auto hashcode = ((forceNewHashCode ? hash_gen<TKey>::getHashCode(oldb.key) : oldb.hash_coll) & 0x7FFFFFFF);
                putEntry(newBuckets, newsize, oldb.key, oldb.val, hashcode); 
            } 
        } 
        delete[] this->buckets_; 
        this->buckets_ = newBuckets; 
        loadsize_ = static_cast<int>(loadFactor_ * newsize); 
        version_++; 
    } 

    void putEntry(bucket<TKey>* newBuckets, int newBucketsSize, TKey key, wpointer nvalue, int hashcode)
    { 
        auto seed = static_cast<uint32_t>(hashcode); 
        auto incr = static_cast<uint32_t>(1 + ((seed * HASH_PRIME) % (static_cast<uint32_t>(newBucketsSize) - 1))); 
        auto bucketNumber = static_cast<int>(seed % static_cast<uint32_t>(newBucketsSize)); 
        do { 

            if ((newBuckets[bucketNumber].key == ValueType<TKey>::NullValue))
            { 
                newBuckets[bucketNumber].val = nvalue; 
                newBuckets[bucketNumber].key = key; 
                newBuckets[bucketNumber].hash_coll |= hashcode; 
                return; 
            } 

            if (newBuckets[bucketNumber].hash_coll >= 0) { 
                newBuckets[bucketNumber].hash_coll |= static_cast<int>(0x80000000); 
                occupancy_++; 
            } 
            bucketNumber = static_cast<int>((static_cast<long>(bucketNumber) + incr) % static_cast<uint32_t>(newBucketsSize)); 
        } while (true); 
    } 
};

