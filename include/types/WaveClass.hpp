#pragma once
#include "WaveMethod.hpp"
#include "WaveType.hpp"
#include "WaveTypeCode.hpp"
#include "api/kernel_panic.hpp"
#include "collections/list_t.hpp"
#include "emit/ClassFlags.hpp"
#include "emit/TypeName.hpp"
#include "emit/WaveField.hpp"
#include "etc/debug_string.hpp"

class WaveClass {
public:
	TypeName* FullName;
    
    [[nodiscard]]
	wstring get_name() { return FullName->get_name(); }
    [[nodiscard]]
    wstring GetPath()  { return FullName->get_namespace(); }

	ClassFlags Flags;
    WaveClass* Parent;

	list_t<WaveField*>* Fields = new list_t<WaveField*>();
    list_t<WaveMethod*>* Methods = new list_t<WaveMethod*>();


	WaveTypeCode TypeCode = TYPE_CLASS;

    [[nodiscard]]
    WaveClass(TypeName* name, WaveClass* parent)
    {
        Parent = parent;
        Flags = CLASS_None;
        FullName = name;
    }
    [[nodiscard]]
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
    [[nodiscard]]
    WaveMethod* FindMethod(const wstring& name) const noexcept(true)
    {
        for(auto* mh : *Methods)
        {
            if (mh->Name == name)
                return mh;
        }
        return nullptr;
    }
    [[nodiscard]]
    WaveField* FindField(FieldName* name) const noexcept(true)
    {
        for (auto* fh : *Fields)
        {
            if (equality<FieldName>::equal(fh->FullName, name))
                return fh;
        }
        return nullptr;
    }

    size_t computed_size = 0;
    bool is_inited = false;
    void** vtable = nullptr;
    int vtable_size = 0;

    
    void init_vtable()
    {
        if (is_inited)
            return;

        if (this->Parent != nullptr)
        {
            this->Parent->init_vtable();
            computed_size += this->Parent->computed_size;
        }
        computed_size += this->Methods->size();
        computed_size += this->Fields->size();

        if (computed_size == 0)
        {
            is_inited = true;
            return;
        }

	    vtable = static_cast<void**>(malloc(sizeof(void*) * computed_size));
	    memset(vtable, 0, sizeof(void*) * computed_size);

        if (this->Parent && this->Parent->vtable_size != 0)
        {
            auto* parent = this->Parent;
            memcpy(vtable, parent->vtable,  sizeof(void*) * parent->vtable_size);
        }
        auto vtable_offset = this->Parent->vtable_size;
        for (size_t i = 0; i != this->Methods->size(); i++, vtable_offset++)
        {
            auto* method = this->Methods->at(i);

            if ((method->Flags & MethodAbstract) != 0 && (this->Flags & CLASS_Abstract) == 0)
            {
                set_failure(WAVE_EXCEPTION_TYPE_LOAD, 
                    fmt::format(L"Method '{0}' in '{1}' type has invalid mapping.",
                        toString(method), toString(this->Parent)));
                return;
            }

            vtable[vtable_offset] = method;
            method->vtable_offset = vtable_offset;
            auto* w = this->Parent->FindMethod(method->Name);
            if (w != nullptr && (method->Flags & MethodOverride) != 0)
                vtable[w->vtable_offset] = method;
            if (w == nullptr && (method->Flags & MethodAbstract) != 0)
                    set_failure(WAVE_EXCEPTION_MISSING_FIELD, 
                        fmt::format(L"method '{0}' mark as OVERRIDE, but parent class '{1}' no contained virtual/abstract method.",
                            toString(method), toString(this->Parent)));
            vtable[method->vtable_offset] = method;
        }
        // check overrides for fields
        if (!this->Fields->empty())
        {
            for (size_t i = 0; i != this->Fields->size(); i++, vtable_offset++)
            {
                auto* field = this->Fields->at(i);

                if ((field->Flags & FIELD_Abstract) != 0 && (this->Flags & CLASS_Abstract) == 0)
                {
                    set_failure(WAVE_EXCEPTION_TYPE_LOAD, 
                        fmt::format(L"Field '{0}' in '{1}' type has invalid mapping.",
                            toString(field), toString(this->Parent)));
                    return;
                }

                vtable[vtable_offset] = field;
                field->vtable_offset = vtable_offset;
                auto* w = this->Parent->FindField(field->FullName);

                if (w != nullptr && (field->Flags & FIELD_Override) != 0)
                    vtable[w->vtable_offset] = field; // so it needed?
                if (w == nullptr && (field->Flags & FIELD_Override) != 0)
                    set_failure(WAVE_EXCEPTION_MISSING_FIELD, 
                        fmt::format(L"field '{0}' mark as OVERRIDE, but parent class '{1}' no contained virtual/abstract field.",
                            toString(field), toString(this->Parent)));
                vtable[field->vtable_offset] = vtable;
            }
        }
    }
};

