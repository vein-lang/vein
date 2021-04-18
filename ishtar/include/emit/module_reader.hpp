#pragma once
#include "AsClass.hpp"
#include "ILLabel.hpp"
#include "interp.hpp"
#include "MethodFlags.hpp"
#include "WaveArgumentRef.hpp"
#include "WaveModue.hpp"
#include "streams/memory_stream.hpp"
#include "streams/binary_reader.hpp"


struct ILWrapper
{
    uint32_t* code;
    uint32_t size;
};
struct DecodedIL
{
    ILWrapper* il;
    dictionary<int, ILLabel>* map = new dictionary<int, ILLabel>();
};
auto decodeLabels(BinaryReader* reader, int offset)
{
    reader->Seek(offset);
    const auto labels_size = reader->Read4();
    if (labels_size == 0)
       return new list_t<int>();
    auto result = new list_t<int>();
    for (auto i = 0; i != labels_size; i++)
    {
        auto p = reader->Read4();
        result->push_back(p);
    }
    return result;
}

auto decodeIL(BinaryReader* reader, int* offset) noexcept(false)
{
    auto* const il_result = new DecodedIL();
    il_result->il = new ILWrapper();
    auto* result = new list_t<uint32_t>();
    
    while (reader->Position() < reader->Length())
    {
        auto opcode = reader->Read2();

        if (opcode == 0xFF00)
        {
            reader->ReadByte();
            opcode = 0xFFFF;
        }

        if (opcode == 0xFFFF)
        {
            *offset = static_cast<int>(reader->Position());
            il_result->il->code = new uint32_t[result->size()];
            il_result->il->size = static_cast<uint32_t>(result->size());
            for (auto i = 0u; i != il_result->il->size; i++)
                il_result->il->code[i] = result->at(i);
            return il_result;
        }
        if (opcode == NOP)
            continue;

        if (opcode >= (uint16_t)LAST)
            throw CorruptILException(fmt::format(L"OpCode '{0}' is not found in metadata.\n{1}", 
                                                 opcode, L"re-run 'gen.csx' for fix this error."));

        const auto* opcode_name = opcodes[opcode];
        auto size = static_cast<int>(opcode_size[opcode]);
        printf("\t\t\t\t\t[ .%s, %d, %d]\n", opcode_name, opcode, size);
        
        auto label = ILLabel {
            static_cast<WaveOpCode>(opcode),
            static_cast<int>(result->size())
        };
        auto ke = static_cast<int>(reader->Position() - sizeof(int16_t));
        il_result->map->insert({ke, label});

        result->push_back(opcode);
        
        switch (size)
        {
            case 0:
                continue;
            case sizeof(char):
                result->push_back(reader->ReadByte());
                continue;
            case sizeof(int16_t):
                result->push_back(reader->Read2());
                continue;
            case sizeof(int32_t):
                {
                    auto s = reader->Read4();
                    auto d = static_cast<uint32_t>(s);
                    result->push_back(d);
                }
                continue;
            case sizeof(int64_t):
                result->push_back(reader->Read4());
                result->push_back(reader->Read4());
                continue;
            case 16:
                result->push_back(reader->Read4());
                result->push_back(reader->Read4());
                result->push_back(reader->Read4());
                result->push_back(reader->Read4());
                continue;
            default:
                switch (opcode)
                {
                    case CALL:
                        result->push_back(reader->ReadByte());
                        result->push_back(reader->Read4());
                        result->push_back(reader->Read4());
                        result->push_back(reader->Read4());
                        result->push_back(reader->Read4());
                    continue;
                    case LOC_INIT_X:
                        result->push_back(reader->Read4());
                        result->push_back(reader->Read4());
                        result->push_back(reader->Read4());
                    continue;
                }
                
                throw CorruptILException(fmt::format(L"OpCode '{0}' has invalid size [{1}].\n{2}", opcode, size,
                        L"Check 'opcodes.def' and re-run 'gen.csx' for fix this error."));
        }
    }
    il_result->il->code = new uint32_t[result->size()];
    il_result->il->size = static_cast<uint32_t>(result->size());
    for (auto i = 0u; i != il_result->il->size; i++)
        il_result->il->code[i] = result->at(i);
    return il_result;
}


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
        auto* tName = reader->ReadTypeName(wn);
        auto* result = new WaveArgumentRef();

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
    const auto flags = static_cast<MethodFlags>(reader->ReadByte());
    const auto bodySize = reader->Read4();
    const auto stackSize = reader->ReadByte();
    const auto locals = reader->ReadByte();
    const auto retTypeName = reader->ReadTypeName(wn);
    auto* args = readArguments(reader, m);
    auto* body = reader->ReadBytes(bodySize);

    const auto name = m->GetConstByIndex(idx);
    auto* retType = m->FindType(retTypeName, true);

    auto* method = new WaveMethod(name, flags, retType, clazz, args);

    printf("\t\t\t\t{\n");
    printf("\t\t\t\tidx: %d, stack: %d, locals: %d\n", idx, bodySize, stackSize, locals);
    printf("\t\t\t\tmethod name: %ws\n", name.c_str());
    printf("\t\t\t\treturn type: %ws\n", retTypeName->FullName.c_str());

    method->LocalsSize = locals;
    method->StackSize = stackSize;

    if ((flags & MethodExtern) != 0)
    {
        method->data.piinfo = new WaveMethodPInvokeInfo();
        method->data.piinfo->addr = nullptr; // TODO
        printf("\t\t\t\t\t[native code]\n");
    }
    else
    {
        method->data.header = new MetaMethodHeader();

        auto* const ilMem = new MemoryStream(body, bodySize);
        auto* const ilReader = new BinaryReader(ilMem);

        auto l_offset = 0;
        auto* const decoded = decodeIL(ilReader, &l_offset);
        auto* const il = decoded->il;

        ilMem->Seek(0);
        auto* const labels = decodeLabels(ilReader, l_offset);

        method->data.header->labels_map = decoded->map;
        method->data.header->labels = labels;
        method->data.header->code_size = il->size;
        method->data.header->max_stack = method->StackSize;
        method->data.header->code = &*il->code;
        //delete ilMem;
    }
    printf("\t\t\t\t},\n");
    return method;
}

[[nodiscard]]
WaveClass* readClass(BinaryReader* reader, WaveModule* m) noexcept(false)
{
    function<wstring(int z)> wn = [m](const int w) {
        return m->GetConstByIndex(w);
    };

    auto* const className = reader->ReadTypeName(wn);
    const auto flags = static_cast<ClassFlags>(reader->ReadByte());

    auto* const parentName = reader->ReadTypeName(wn);
    const auto len = reader->Read4();
        
    auto* const parent = m->FindType(parentName, true);
    auto* clazz = new WaveClass(className, AsClass(parent));
    clazz->Flags = flags;
    printf("\t\t\tclass name: '%ws'\n", className->FullName.c_str());
    printf("\t\t\tclass parent: '%ws'\n", parent->get_full_name()->FullName.c_str());
    printf("\t\t\t[construct method table] size: %d\n", len);
    for (auto i = 0; i != len; i++)
    {
        const auto size = reader->Read4();
        auto* const body = reader->ReadBytes(size);
        auto* const methodMem = new MemoryStream(body, size);
        auto* const methodReader = new BinaryReader(methodMem);
        auto* const method = readMethod(methodReader, clazz, m);
        clazz->Methods->push_back(method);
        delete body;
    }

    const auto fsize = reader->Read4();
    for (auto i = 0; i != fsize; i++)
    {
        const auto fnameIdx = reader->Read8();
        auto* fname = FieldName::construct(fnameIdx, &wn);
        const auto tname = reader->ReadTypeName(wn);
        auto* const return_type = m->FindType(tname, true);
        const auto fflags  = static_cast<FieldFlags>(reader->ReadByte());
        //auto litVal = reader->ReadBytes(0);
        auto* field = new WaveField(clazz, fname, fflags, return_type);
        clazz->Fields->push_back(field);
    }
    clazz->owner_module = m;
    clazz->init_vtable();

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
    printf("\t[construct const string table] size: %d\n", ssize);
    for (auto i = 0; i != ssize; i++)
    {
        auto key = reader->Read4();
        auto str = reader->ReadInsomniaString();
        target->strings->insert({key, str});
        printf("\t\t{'%d'}: '%ws'\n", key, str.c_str());
    }
    const auto csize = reader->Read4();
    printf("\t[construct class table] size: %d\n", csize);
    for (auto i = 0; i != csize; i++)
    {
        const auto size = reader->Read4();
        auto* body = reader->ReadBytes(size);
        auto* const classMem = new MemoryStream(body, size);
        auto* const classReader = new BinaryReader(classMem);
        printf("\t\t{\n");
        auto* clazz = readClass(classReader, target);
        printf("\t\t}\n;");
        target->classList->push_back(clazz);
        //delete classReader;
        //delete classMem;
        delete body;
    }
    target->name = target->GetConstByIndex(idx);
    printf("\tload '%ws' module success\n", target->name.c_str());

    
    return target;
}
