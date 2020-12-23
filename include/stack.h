#pragma once

template<typename T>
class stack 
{
    public:
    explicit stack(const int volume) {
        body_ = new T[volume]();
        pos_ = -1;
        size_ = volume;
    }
    ~stack() 
    {
        delete[] body_;
    }
    bool is_empty() const
    {
        if(pos_ == -1)
            return true;
        return false;
    }
    bool is_full() const
    {
        if(pos_ == size_-1)
            return true;
        return false;
    }
    bool push(const T &value)
    {
        if(is_full())
            return false;
        else 
            body_[++pos_] = value;
        return true;
    }
    bool pop(T &value)
    {
        if (!is_empty()) 
        {
            value = body_[pos_--];
            return true;
        }
        return false;
    }
    int size() const
    {
        return pos_+1;
    }

    private:
        T* body_;
        int pos_;
        int size_;
};