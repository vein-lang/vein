#pragma once
#include "compatibility.hpp"
#include "internal.hpp"
#include "collections/map2args.hpp"
#include "emit/WaveArgumentRef.hpp"
#include "types/WaveCore.hpp"

inline void processStrings(WaveType* type, WaveModule* m)
{
    m->StageTypeConstant(type->get_full_name());
    for(auto* a : *type->Members)
    {
        switch (a->GetKind())
        {
            case WaveMemberKind::Field:
            {
                auto* f = dynamic_cast<WaveField*>(a);
                m->GetFieldConstant(f->FullName);
                m->StageTypeConstant(f->Type->get_full_name());
            }
            break;
            case WaveMemberKind::Method:
            {
                auto* f = dynamic_cast<WaveMethod*>(a);
                m->GetStringConstant(f->Name);
                m->StageTypeConstant(f->ReturnType->get_full_name());
                m->StageTypeConstant(f->Owner->FullName);
                for(auto* arg : *f->Arguments)
                {
                    m->GetStringConstant(arg->Name);
                    m->StageTypeConstant(arg->Type->get_full_name());
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
    m->StageTypeConstant(type->FullName);
    for(auto* a : *type->Fields)
    {
        m->GetFieldConstant(a->FullName);
        m->StageTypeConstant(a->Type->get_full_name());
    }
    for(auto* a : *type->Methods)
    {
        m->GetStringConstant(a->Name);
        m->StageTypeConstant(a->ReturnType->get_full_name());
        for(auto* arg : *a->Arguments)
        {
            m->GetStringConstant(arg->Name);
            m->StageTypeConstant(arg->Type->get_full_name());
        }
    }
}

map<wstring, WaveClass*> classes_ref;
#define CREATE_REF(T) {wave_core->T->FullName->FullName, wave_core->T}

// ORDER 1
inline void init_default()
{
    auto* const corlib = new WaveModule(L"corlib");
    
    wave_core->object_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Object"), nullptr);
    wave_core->object_class->TypeCode = TYPE_OBJECT;

    wave_core->value_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/ValueType"), wave_core->value_class);
    
    wave_core->void_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Void"), wave_core->object_class);
    wave_core->void_class->TypeCode = TYPE_VOID;
    
    wave_core->string_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/String"), wave_core->object_class);
    wave_core->string_class->TypeCode = TYPE_STRING;
    
    wave_core->console_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Console"), wave_core->object_class);
    wave_core->native_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Native"), wave_core->object_class);
    
    wave_core->i4_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Int32"), wave_core->value_class);
    wave_core->i4_class->TypeCode = TYPE_I4;

    wave_core->i8_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Int64"), wave_core->value_class);
    wave_core->i8_class->TypeCode = TYPE_I8;

    wave_core->exception_class = new WaveClass(new TypeName(L"corlib%global::wave/lang/Exception"), wave_core->object_class);
    
    corlib->classList->push_back(wave_core->object_class);
    corlib->classList->push_back(wave_core->value_class);
    corlib->classList->push_back(wave_core->void_class);
    corlib->classList->push_back(wave_core->native_class);
    corlib->classList->push_back(wave_core->string_class);
    corlib->classList->push_back(wave_core->console_class);
    corlib->classList->push_back(wave_core->i4_class);
    corlib->classList->push_back(wave_core->i8_class);
    corlib->classList->push_back(wave_core->exception_class);

    classes_ref = {
        CREATE_REF(object_class),
        CREATE_REF(value_class),
        CREATE_REF(void_class),
        CREATE_REF(native_class),
        CREATE_REF(string_class),
        CREATE_REF(console_class),
        CREATE_REF(i4_class),
        CREATE_REF(i8_class),
        CREATE_REF(exception_class),
    };
    wave_core->corlib = corlib;
}

#define EMPTY_LIST_(T) new list_t<T>()

inline void init_strings_phase_1()
{
    for(auto* clazz : *wave_core->corlib->classList)
        processStrings(clazz, wave_core->corlib);
}
// ORDER 2
inline void init_types() // TODO resolve members problem with AsClass casting this types...
{
    wave_core->object_type = AsType(wave_core->object_class);
    wave_core->value_type = AsType(wave_core->value_class);
    wave_core->void_type = AsType(wave_core->void_class);
    wave_core->string_type = AsType(wave_core->string_class);
    wave_core->console_type = AsType(wave_core->console_class);
    wave_core->native_type = AsType(wave_core->native_class);
    wave_core->i4_type = AsType(wave_core->i4_class);
    wave_core->i8_type = AsType(wave_core->i8_class);
    wave_core->exception_type = AsType(wave_core->exception_class);



    wave_core->exception_class->DefineField(L"message", static_cast<FieldFlags>(FIELD_Public | FIELD_Virtual), wave_core->string_type);
}
// ORDER 3
inline void init_tables()
{
    for (auto i = 0; i < internal_last; i++)
    {
        const auto* const name = internal_call_names[i];
        void* ref = internal_call_functions[i];
        auto arg_size = internal_call_function_args_size[i];
        auto* raw_args = (map<wstring, wstring>*)internal_call_function_args_refs[i];
        auto direction = internal_call_functions_direction[i];
        auto* args = convert_map2list_args(raw_args, wave_core->corlib);

        auto* const f = new WaveMethod(
        name,
        static_cast<MethodFlags>(MethodPublic | MethodExtern | MethodStatic),
            wave_core->void_type, classes_ref.at(direction), args);
        f->data.piinfo = new WaveMethodPInvokeInfo();
        f->data.piinfo->addr = internal_call_functions[i];

        classes_ref.at(direction)->Methods->push_back(f);
    }
}
// ORDER 4
inline void init_strings_phase_2()
{
    for(auto* clazz : *wave_core->corlib->classList)
        processStrings(clazz, wave_core->corlib);
}