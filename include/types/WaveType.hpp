#pragma once
#include <string>


#include "WaveTypeCode.hpp"
#include "collections/list.hpp"
#include "emit/ClassFlags.hpp"
#include "emit/TypeName.hpp"
#include "emit/WaveMember.hpp"

using namespace std;

class WaveType : WaveMember
{
public:
    virtual string get_namespace() = 0;
    list<WaveMember>* Members = new list<WaveMember>();
    WaveType* Parent;
    ClassFlags classFlags;
    WaveTypeCode TypeCode;

    virtual TypeName* get_full_name() = 0;


    virtual bool IsArray() { return false; }
    virtual bool IsSealed() { return false; }
    virtual bool IsClass() { return false; }
    virtual bool IsPublic() { return false; }
    virtual bool IsPrimitive() { return false; }
    bool IsPrivate() { return !IsPublic(); }


    static WaveType* ByName(TypeName* name) { throw NotImplementedException(); }

protected:
    WaveType(const string& name, WaveType* parent) : WaveMember(name)
    {
        Parent = parent;
        TypeCode = TYPE_CLASS;
        classFlags = CLASS_None;
    }
};