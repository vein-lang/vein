#pragma once
#include "WaveMethod.hpp"
#include "WaveType.hpp"
#include "WaveTypeCode.hpp"
#include "collections/list_t.hpp"
#include "emit/ClassFlags.hpp"
#include "emit/TypeName.hpp"
#include "emit/WaveField.hpp"

class WaveClass {
public:
	TypeName* FullName;

	wstring GetName() { return FullName->get_name(); }
    wstring GetPath()  { return FullName->get_namespace(); }

	ClassFlags Flags;
    WaveClass* Parent;

	list_t<WaveField*>* Fields = new list_t<WaveField*>();
    list_t<WaveMethod*>* Methods = new list_t<WaveMethod*>();


	WaveTypeCode TypeCode = TYPE_CLASS;


    WaveClass(TypeName* name, WaveClass* parent)
    {
        Parent = parent;
        Flags = CLASS_None;
        FullName = name;
    }

    WaveClass(WaveType* type, WaveClass* parent)
    {
        Parent = parent;
        Flags = CLASS_None;
        FullName = type->get_full_name();
    }
    [[nodiscard]]
    WaveMethod* DefineMethod(const wstring& name, MethodFlags flags, WaveType* retType, list_t<WaveArgumentRef*>* args)
    {
        auto* method = new WaveMethod(name, flags, retType, this, args);

        Methods->push_back(method);

        return method;
    }

    WaveMethod* FindMethod(const wstring& name) const noexcept(false)
    {
        for(auto mh : *Methods)
        {
            if (mh->Name._Equal(name))
                return mh;
        }
        throw "Not found method";
    }
};
