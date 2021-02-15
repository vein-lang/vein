#pragma once
#include "WaveMethod.h"
#include "WaveType.hpp"
#include "WaveTypeCode.hpp"
#include "collections/list.hpp"
#include "emit/ClassFlags.hpp"
#include "emit/TypeName.hpp"
#include "emit/WaveField.hpp"

class WaveClass {
public:
	TypeName* FullName;

	string GetName() { return FullName->get_name(); }
    string GetPath()  { return FullName->get_namespace(); }

	ClassFlags Flags;
    WaveClass* Parent;

	list<WaveField>* Fields = new list<WaveField>();
    list<WaveMethod>* Methods = new list<WaveMethod>();


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
};
