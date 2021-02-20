#pragma once
#include "compatibility.hpp"
#include "internal.hpp"
#include "emit/WaveArgumentRef.hpp"
#include "types/WaveCore.hpp"

#define root_namespace "wave/lang"
#define class_name(namespace, class_name) "global::"#namespace#class_name

inline WaveType* createType(wstring addr, WaveTypeCode code)
{
    auto n = new TypeName(addr);
    auto a = new WaveTypeImpl(n, code);
    return dynamic_cast<WaveType*>(a);
}

inline void processStrings(WaveType* type, WaveModule* m)
{
    m->GetTypeConstant(type->get_full_name());
    for(auto* a : *type->Members)
    {
        switch (a->GetKind())
        {
            case WaveMemberKind::Field:
            {
                auto* f = dynamic_cast<WaveField*>(a);
                m->GetTypeConstant(f->FullName);
                m->GetTypeConstant(f->Type->get_full_name());
            }
            break;
            case WaveMemberKind::Method:
            {
                auto* f = dynamic_cast<WaveMethod*>(a);
                m->GetStringConstant(f->Name);
                m->GetTypeConstant(f->ReturnType->get_full_name());
                m->GetTypeConstant(f->Owner->FullName);
                for(auto* arg : *f->Arguments)
                {
                    m->GetStringConstant(arg->Name);
                    m->GetTypeConstant(arg->Type->get_full_name());
                }
            }
            break;
            default:
                printf("unknown kind.");
            break;
        }
    }
}

inline void processStrings(WaveClass* type, WaveModule* m)
{
    m->GetTypeConstant(type->FullName);
    for(auto* a : *type->Fields)
    {
        m->GetTypeConstant(a->FullName);
        m->GetTypeConstant(a->Type->get_full_name());
    }
    for(auto* a : *type->Methods)
    {
        m->GetStringConstant(a->Name);
        m->GetTypeConstant(a->ReturnType->get_full_name());
        for(auto* arg : *a->Arguments)
        {
            m->GetStringConstant(arg->Name);
            m->GetTypeConstant(arg->Type->get_full_name());
        }
    }
}

inline void init_default()
{
    auto* const corlib = new WaveModule(L"corlib");
    
    wave_core->object_type = createType(L"global::wave/lang/Object", TYPE_OBJECT);
    wave_core->object_class = new WaveClass(wave_core->object_type, nullptr);

    wave_core->void_type = createType(L"global::wave/lang/Void", TYPE_VOID);
    wave_core->void_class = new WaveClass(wave_core->void_type, wave_core->object_class);

    wave_core->native_type = createType(L"global::wave/lang/Native", TYPE_CLASS);
    wave_core->native_class = new WaveClass(wave_core->native_type, wave_core->object_class);

    wave_core->string_type = createType(L"global::wave/lang/String", TYPE_STRING);
    wave_core->string_class = new WaveClass(wave_core->string_type, wave_core->object_class);

    processStrings(wave_core->object_type, corlib);
    processStrings(wave_core->object_class, corlib);

    processStrings(wave_core->void_type, corlib);
    processStrings(wave_core->void_class, corlib);


    corlib->classList->push_back(wave_core->object_class);
    corlib->classList->push_back(wave_core->void_class);
    corlib->classList->push_back(wave_core->native_class);
    corlib->classList->push_back(wave_core->string_class);

    wave_core->corlib = corlib;
}

#define EMPTY_LIST_(T) new list_t<T>()

inline void init_tables()
{
    for (auto i = 0; i < internal_last; i++)
    {
        auto* const f = new WaveMethod(
        internal_call_names[i],
        static_cast<MethodFlags>(MethodPublic | MethodExtern | MethodStatic),
            wave_core->void_type, wave_core->native_class, EMPTY_LIST_(WaveArgumentRef*));
        f->data.piinfo = new WaveMethodPInvokeInfo();
        f->data.piinfo->addr = internal_call_functions[i];
    }
}