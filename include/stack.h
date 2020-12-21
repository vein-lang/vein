#pragma once

template<typename T>
class Stack 
{
    public:
    Stack(int volume) {
        _body = new T[volume]();
        _pos = -1;
        _size = volume;
    }
    ~Stack() 
    {
        delete[] _body;
    }
    bool isEmpty()
    {
        if(_pos == -1)
            return true;
        else
            return false;
    }
    bool isFull()
    {
        if(_pos == _size-1)
            return true;
        else
            return false;
    }
    bool push(const T &value)
    {
        if(isFull())
            return false;
        else 
            _body[++_pos] = value;
        return true;
    }
    bool pop(T &value)
    {
        if (!isEmpty()) 
        {
            value = _body[_pos--];
            return true;
        }
        return false;
    }
    int size()
    {
        return _pos+1;
    }

    private:
        T* _body;
        int _pos;
        int _size;
};