#pragma once
#include "compatibility.types.h"
#include "hash.h"
#include "eq.h"

template<typename TKey>
struct bucket {
    TKey key;
    wpointer val;
    int hash_coll;
    bucket() : key(NULL_VALUE(TKey)), val(nullptr), hash_coll(0) {}
};

template<typename TKey>
class hashtable final
{ 
public:
    hashtable() : hashtable(24) { } 
    hashtable(int capacity) : hashtable(capacity, 1.0f) { } 
    hashtable(int capacity, float loadFactor) { 
        this->load_factor_ = 0.72f * loadFactor; 

        auto rawsize = capacity / this->load_factor_; 
        auto hashsize = (rawsize > 4) ? Hash::getPrime(static_cast<int>(rawsize)) : 4; 
        this->buckets_ = new bucket<TKey>[hashsize];
        for (auto i = 0; i != hashsize; i++) 
        {
            this->buckets_[i].hash_coll = 0; 
            this->buckets_[i].key = NULL_VALUE(TKey);
            this->buckets_[i].val = nullptr; 
        } 
        this->loadsize_ = static_cast<int>(this->load_factor_ * hashsize); 
        this->buckets_size_ = hashsize; 
    } 
    ~hashtable()
    {
        delete[] this->buckets_;
        delete this;
    }

    void add(TKey key, wpointer value)
    { 
        insert(key, value, true); 
    } 

    wpointer get(TKey key)
    { 
        uint32_t seed; 
        uint32_t incr; 
        
        auto hashcode = init_hash(key, buckets_size_, &seed, &incr); 
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

            if (b.key == NULL_VALUE(TKey))
                return nullptr; 
            if ((b.hash_coll & 0x7FFFFFFF) == hashcode && 
                default_equal(b.key, key)) 
                return b.val; 
            bucketNumber = static_cast<int>((static_cast<long>(bucketNumber) + incr) % static_cast<uint32_t>(buckets_size_)); 
        } while (b.hash_coll < 0 && ++ntry < buckets_size_); 
        return nullptr; 
    } 
    void set(TKey key, const wpointer value)
    { 
        insert(key, value, false); 
    } 

private: 
    bucket<TKey>* buckets_ = new bucket<TKey>[0];
    int buckets_size_ = 0; 
    int count_ = 0; 
    int occupancy_ = 0; 
    int loadsize_ = 0; 
    float load_factor_ = 0; 
    int version_ = 0;


    uint32_t init_hash(TKey key, const int hashsize, uint32_t* seed, uint32_t* incr)
    {
        const auto hashcode = static_cast<uint32_t>(hash_gen<TKey>::getHashCode(key)) & 0x7FFFFFFF; 
        *seed = static_cast<uint32_t>(hashcode); 
        *incr = static_cast<uint32_t>(1 + ((*seed * HASH_PRIME) % (static_cast<uint32_t>(hashsize) - 1))); 
        return hashcode; 
    } 

    void insert(TKey key, wpointer nvalue, const bool add)
    { 
        if (count_ >= loadsize_)  
            expand(); 
        else if (occupancy_ > loadsize_ && count_ > 100) 
            rehash(); 

        uint32_t seed; 
        uint32_t incr; 
        uint32_t hashcode = init_hash(key, buckets_size_, &seed, &incr);
        auto ntry = 0;
        auto empty_slot_number = -1;
        auto bucket_number = static_cast<int>(seed % static_cast<uint32_t>(buckets_size_)); 

        do 
        { 
            if (empty_slot_number == -1 && this->buckets_[bucket_number].hash_coll < 0) 
                empty_slot_number = bucket_number; 

            if (this->buckets_[bucket_number].key == NULL_VALUE(TKey))
            { 
                if (empty_slot_number != -1) 
                    bucket_number = empty_slot_number; 
                
                this->buckets_[bucket_number].val = nvalue; 
                this->buckets_[bucket_number].key = key; 
                this->buckets_[bucket_number].hash_coll |= static_cast<int>(hashcode); 
                count_++; 
                version_++; 
                return; 
            } 

            if (((this->buckets_[bucket_number].hash_coll & 0x7FFFFFFF) == hashcode) && 
                default_equal(this->buckets_[bucket_number].key, key)) 
            { 
                if (add) 
                    return; 
                this->buckets_[bucket_number].val = nvalue; 
                version_++; 
                return; 
            } 

            if (empty_slot_number == -1)  
            { 
                if (this->buckets_[bucket_number].hash_coll >= 0) { 
                    this->buckets_[bucket_number].hash_coll |= static_cast<int>(0x80000000); 
                    occupancy_++; 
                } 
            } 

            bucket_number = static_cast<int>((static_cast<long>(bucket_number) + incr) % static_cast<uint32_t>(buckets_size_)); 
        } 
        while (++ntry < buckets_size_); 

        if (empty_slot_number != -1) 
        { 
            this->buckets_[empty_slot_number].val = nvalue; 
            this->buckets_[empty_slot_number].key = key; 
            this->buckets_[empty_slot_number].hash_coll |= static_cast<int>(hashcode); 
            count_++; 
            version_++; 
        } 
    } 


    void expand() 
    {
        const auto rawsize = Hash::expandPrime(buckets_size_); 
        rehash(rawsize, false); 
    } 
    void rehash() 
    { 
        rehash(buckets_size_, false); 
    } 
    void rehash(int newsize, bool force_new_hash_code) 
    { 
        occupancy_ = 0;
        auto* new_buckets = new bucket<TKey>[newsize];

        for (auto nb = 0; nb < buckets_size_; nb++)  
        { 
            auto oldb = this->buckets_[nb]; 
            if (oldb.key != NULL_VALUE(TKey))
            { 
                auto hashcode = ((force_new_hash_code ? hash_gen<TKey>::getHashCode(oldb.key) : oldb.hash_coll) & 0x7FFFFFFF);
                putEntry(new_buckets, newsize, oldb.key, oldb.val, hashcode); 
            } 
        } 
        delete[] this->buckets_; 
        this->buckets_ = new_buckets; 
        loadsize_ = static_cast<int>(load_factor_ * static_cast<float>(newsize)); 
        version_++; 
    } 

    void putEntry(bucket<TKey>* new_buckets, const int new_buckets_size, TKey key, wpointer nvalue, int hashcode)
    { 
        auto seed = static_cast<uint32_t>(hashcode); 
        auto incr = static_cast<uint32_t>(1 + seed * HASH_PRIME % (static_cast<uint32_t>(new_buckets_size) - 1)); 
        auto bucketNumber = static_cast<int>(seed % static_cast<uint32_t>(new_buckets_size)); 
        do { 

            if (new_buckets[bucketNumber].key == NULL_VALUE(TKey))
            { 
                new_buckets[bucketNumber].val = nvalue; 
                new_buckets[bucketNumber].key = key; 
                new_buckets[bucketNumber].hash_coll |= hashcode; 
                return; 
            } 

            if (new_buckets[bucketNumber].hash_coll >= 0) 
            { 
                new_buckets[bucketNumber].hash_coll |= static_cast<int>(0x80000000); 
                occupancy_++; 
            } 
            bucketNumber = static_cast<int>((static_cast<long>(bucketNumber) + incr) % static_cast<uint32_t>(new_buckets_size)); 
        } while (true); 
    } 
};

