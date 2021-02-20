#pragma once
#include <string>


#include "WaveTypeCode.hpp"
#include "collections/list_t.hpp"
#include "emit/ClassFlags.hpp"
#include "emit/TypeName.hpp"
#include "emit/WaveMember.hpp"

using namespace std;

class WaveType : WaveMember
{
public:
    virtual wstring get_namespace()
    {
        return get_full_name()->get_namespace();
    }
    list_t<WaveMember*>* Members = new list_t<WaveMember*>();
    WaveType* Parent;
    ClassFlags classFlags;
    WaveTypeCode TypeCode;

    virtual TypeName* get_full_name() = 0;

    WaveMemberKind GetKind() override
    {
        return WaveMemberKind::Type;
    }


    [[nodiscard]] virtual bool IsArray    () const noexcept { return false; }
    [[nodiscard]] virtual bool IsSealed   () const noexcept { return false; }
    [[nodiscard]] virtual bool IsClass    () const noexcept { return false; }
    [[nodiscard]] virtual bool IsPublic   () const noexcept { return false; }
    [[nodiscard]] virtual bool IsPrimitive() const noexcept { return false; }
    [[nodiscard]]         bool IsPrivate  () const noexcept { return !IsPublic(); }


    static WaveType* ByName(TypeName* name) { throw NotImplementedException(); }

protected:
    WaveType(const wstring& name, WaveType* parent) : WaveMember(name)
    {
        Parent = parent;
        TypeCode = TYPE_CLASS;
        classFlags = CLASS_None;
    }
    WaveType() : WaveMember(L"")
    {
        Parent = nullptr;
        classFlags = CLASS_None;
    }
};

class WaveTypeImpl : public WaveType
{
    
public:
    WaveTypeImpl(TypeName* tn, WaveTypeCode code)
    {
        type_name_ = tn;
        TypeCode = code;
    }
    WaveTypeImpl(TypeName* tn, WaveTypeCode code, ClassFlags flags) : WaveTypeImpl(tn, code)
    {
        classFlags = flags;
    }
    WaveTypeImpl(TypeName* tn, WaveTypeCode code, ClassFlags flags, WaveType* parent)
    : WaveTypeImpl(tn, code, flags)
    {
        Parent = parent;
    }
    [[nodiscard]]
    bool IsArray() const noexcept override
    {
        return false;
    }
    [[nodiscard]]
    bool IsSealed() const noexcept override
    {
        return false;
    }
    [[nodiscard]]
    bool IsClass() const noexcept override
    {
        return !IsPrimitive();
    }
    [[nodiscard]]
    bool IsPublic() const noexcept override
    {
        return (classFlags & CLASS_Public) != 0;
    }
    [[nodiscard]]
    bool IsPrimitive() const noexcept override
    {
        return TypeCode != TYPE_CLASS;
    }
    TypeName* get_full_name() override
    {
        return type_name_;
    }
private:
    TypeName* type_name_;
};