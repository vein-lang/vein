#pragma once
#include "AsClass.hpp"
#include "MethodFlags.hpp"
#include "WaveArgumentRef.hpp"
#include "WaveModue.hpp"
#include "streams/memory_stream.hpp"
#include "streams/binary_reader.hpp"


[[nodiscard]]
auto* readArguments(BinaryReader* reader, WaveModule* m) noexcept
{
    auto* args = new list_t<WaveArgumentRef*>();
    auto size = reader->Read4();

    function<wstring(int z)> wn = [m](int w) {
        return m->GetConstByIndex(w);
    };

    for (auto i = 0; i != size; i++)
    {
        const auto nIdx = reader->Read4();
        const auto tIdx = reader->Read8();
        auto* result = new WaveArgumentRef();
        auto* tName = TypeName::construct(tIdx, &wn);

        result->Type = m->FindType(tName, true);
        result->Name = m->GetConstByIndex(nIdx);
        args->push_back(result);
    }
    return args;
}

[[nodiscard]]
WaveMethod* readMethod(BinaryReader* reader, WaveClass* clazz, WaveModule* m) noexcept
{
    function<wstring(int z)> wn = [m](const int w) {
        return m->GetConstByIndex(w);
    };

    const auto idx = reader->Read4();
    const auto flags = static_cast<MethodFlags>(reader->ReadU2());
    const auto bodySize = reader->Read4();
    const auto stackSize = reader->ReadU2();
    const auto locals = reader->ReadU2();
    const auto retTypeIdx = reader->Read8();
    auto* args = readArguments(reader, m);
    auto* body = reader->ReadBytes(bodySize);
    auto* retTypeName = TypeName::construct(retTypeIdx, &wn);

    const auto name = m->GetConstByIndex(idx);
    auto* retType = m->FindType(retTypeName, true);

    auto* method = new WaveMethod(name, flags, retType, clazz, args);

    method->LocalsSize = locals;
    method->StackSize = stackSize;

    if ((flags & MethodExtern) != 0)
    {
        method->data.piinfo = new WaveMethodPInvokeInfo();
        method->data.piinfo->addr = nullptr; // TODO
    }
    else
    {
        method->data.header = new MetaMethodHeader();
        method->data.header->code_size = bodySize;
        method->data.header->max_stack = method->StackSize;
        //method->data.header->code = body; TODO
    }
    return method;
}

union bytes {
    unsigned char c[8];
    uint64_t l;
} myb;

[[nodiscard]]
WaveClass* readClass(BinaryReader* reader, WaveModule* m) noexcept(false)
{
    const auto idx = reader->Read4();
    const auto nsidx = reader->Read4();
    const auto flags = static_cast<ClassFlags>(reader->ReadU2());

    const auto parentIdx = reader->Read8();
    const auto len = reader->Read4();

    function<wstring(int z)> wn = [m](const int w) {
        return m->GetConstByIndex(w);
    };

    auto* const typeName = TypeName::construct(parentIdx, &wn);
    auto* const parent = m->FindType(typeName, true);
    auto* className = TypeName::construct(nsidx, idx, &wn);
    auto* clazz = new WaveClass(className, AsClass(parent));
    clazz->Flags = flags;
    for (auto i = 0; i != len; i++)
    {
        const auto size = reader->Read4();
        auto* const body = reader->ReadBytes(size);
        auto* const methodMem = new MemoryStream(body, size);
        auto* const methodReader = new BinaryReader(methodMem);
        auto* const method = readMethod(methodReader, clazz, m);
        clazz->Methods->push_back(method);
        delete methodReader;
        delete methodMem;
        delete body;
    }

    const auto fsize = reader->Read4();
    for (auto i = 0; i != fsize; i++)
    {
        const auto fnameIdx = reader->Read8();
        auto* fname = FieldName::construct(fnameIdx, &wn);
        const auto tnameIdx = reader->Read8();
        auto* tname = TypeName::construct(tnameIdx, &wn);
        auto* const return_type = m->FindType(tname, true);
        const auto fflags  = static_cast<FieldFlags>(reader->ReadU2());
        //auto litVal = reader->ReadBytes(0);
        auto* field = new WaveField(clazz, fname, fflags, return_type);
        clazz->Fields->push_back(field);
    }
    return clazz;
}


[[nodiscard]]
WaveModule* readModule(char* arr, size_t sz, list_t<WaveModule*>* deps) noexcept
{
    auto* target = new WaveModule(L"<temporary>");
    auto* mem = new MemoryStream(arr, sz);
    auto* reader = new BinaryReader(mem);

    target->deps = deps;
    
    const auto idx = reader->Read4();

    const auto ssize = reader->Read4();
    for (auto i = 0; i != ssize; i++)
    {
        auto key = reader->Read4();
        auto str = reader->ReadInsomniaString();
        target->strings->insert({key, str});
    }
    const auto csize = reader->Read4();
    for (auto i = 0; i != csize; i++)
    {
        const auto size = reader->Read4();
        auto* body = reader->ReadBytes(size);
        auto* const classMem = new MemoryStream(body, size);
        auto* const classReader = new BinaryReader(classMem);
        auto* clazz = readClass(classReader, target);
        target->classList->push_back(clazz);
        delete classReader;
        delete classMem;
        delete body;
    }

    target->name = target->GetConstByIndex(idx);
    return target;
}
