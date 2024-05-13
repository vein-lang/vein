namespace ishtar.runtime;

using collections;
using emit;
using emit.extensions;
using ishtar;
using runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Xml.Linq;
using vein.exceptions;
using vein.extensions;
using vein.reflection;
using vein.runtime;


public unsafe struct RuntimeIshtarModule
{
    public WeakRef<AppVault>* Vault { get; }
    public VirtualMachine vm => Vault->Value.vm;
    public ushort ID { get; internal set; }
    public WeakRef<VeinModule>* Original { get; }

    private RuntimeIshtarModule* _selfRef;


    public RuntimeIshtarModule(AppVault vault, string name, RuntimeIshtarModule* self)
    {
        Original = WeakRef<VeinModule>.Create(new VeinModule(name, new Version(1, 0), new VeinCore()));
        Vault = WeakRef<AppVault>.Create(vault);
        _name = StringStorage.Intern(name);
        _selfRef = self;
    }

    

    public DirectNativeList<RuntimeIshtarClass>* class_table { get; }
        = DirectNativeList<RuntimeIshtarClass>.New(64);
    public DirectNativeList<RuntimeIshtarModule>* deps_table { get; }
        = DirectNativeList<RuntimeIshtarModule>.New(64);
    public DirectNativeList<RuntimeAspect>* aspects_table { get; }
        = DirectNativeList<RuntimeAspect>.New(64);
    public DirectNativeDictionary<int, RuntimeQualityTypeName>* types_table { get; }
        = DirectNativeDictionary<int, RuntimeQualityTypeName>.New();
    public DirectNativeDictionary<int, RuntimeFieldName>* fields_table { get; }
        = DirectNativeDictionary<int, RuntimeFieldName>.New();
    public DirectNativeDictionary<int, InternedString>* string_table { get; }
        = DirectNativeDictionary<int, InternedString>.New();



    private InternedString* _name;

    public string Name
    {
        get => StringStorage.GetStringUnsafe(_name);
        set => _name = StringStorage.Intern(value);
    }


    public RuntimeIshtarClass* FindType(RuntimeToken type, bool findExternally = false)
    {
        var result = class_table->FirstOrNull(filter);
        if (result is not null)
            return result;

        if (!findExternally)
            return null;

        using var enumerator = deps_table->_ref->GetEnumerator();

        while (enumerator.MoveNext())
        {
            var module = (RuntimeIshtarModule*)enumerator.Current;
            result = module->FindType(type, true);
            if (result is not null)
                return result;
        }

        return null;

        bool filter(RuntimeIshtarClass* x) => x->runtime_token == type;
    }

    public RuntimeFieldName* GetFieldNameByIndex(int idx)
    {
        if (fields_table->TryGetValue(idx, out var result))
            return result;
        return null;
    }

    public RuntimeIshtarClass* FindType(RuntimeQualityTypeName* type, bool findExternally = false)
    {
        var result = class_table->FirstOrNull(filter);
        if (result is not null)
            return result;

        if (!findExternally)
            return null;

        using var enumerator = deps_table->_ref->GetEnumerator();

        while (enumerator.MoveNext())
        {
            var module = (RuntimeIshtarModule*)enumerator.Current;
            result = module->FindType(type, true);
            if (result is not null)
                return result;
        }

        return null;

        bool filter(RuntimeIshtarClass* x) => x->FullName == type;
    }

    

    public RuntimeIshtarClass* FindType(QualityTypeName type, bool findExternally, bool dropUnresolvedException)
    {
        if (!findExternally)
            findExternally = this.Name != type.AssemblyName;
        var result = class_table->FirstOrNull(filter);

        if (result is not null)
            return result;

        bool filter(RuntimeIshtarClass* x)
        {
            return (*x->FullName).T().Equals(type);
        }


        RuntimeIshtarClass* createResult(RuntimeIshtarModule* @this)
        {
            if (dropUnresolvedException || findExternally)
                throw new TypeNotFoundException($"'{type.NameWithNS}' not found in modules and dependency assemblies.");
            return @this->DefineUnresolvedClass(type.T());
        }

        fixed (RuntimeIshtarModule* m = &this)
        {
            if (!findExternally)
                return createResult(m);

            var dm = deps_table->FirstOrNull(x => x->FindType(type, true, dropUnresolvedException) is not null);


            if (dm is not null)
                return dm->FindType(type, true, dropUnresolvedException);

            return createResult(m);
        }
    }

    public RuntimeIshtarClass* FindType(RuntimeQualityTypeName* type, bool findExternally, bool dropUnresolvedException)
    {
        if (!findExternally)
            findExternally = this.Name != type->AssemblyName;
        var result = class_table->FirstOrNull(filter);

        if (result is not null)
            return result;

        bool filter(RuntimeIshtarClass* x) => x->FullName->Equal(type);

        RuntimeIshtarClass* createResult(RuntimeIshtarModule* @this)
        {
            if (dropUnresolvedException /*|| findExternally*/)
                throw new TypeNotFoundException($"'{type->NameWithNS}' not found in modules and dependency assemblies.");
            return @this->DefineUnresolvedClass(type);
        }

        fixed (RuntimeIshtarModule* m = &this)
        {
            if (!findExternally)
                return createResult(m);

            var dm = deps_table->FirstOrNull(x => x->FindType(type, true, dropUnresolvedException) is not null);


            if (dm is not null)
                return dm->FindType(type, true, dropUnresolvedException);

            return createResult(m);
        }
    }



    public RuntimeIshtarClass* DefineClass(RuntimeQualityTypeName* fullName, RuntimeIshtarClass* parent)
    {
        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();

        *clazz = new RuntimeIshtarClass(fullName, parent, _selfRef, clazz);

        class_table->Add(clazz);

        return clazz;
    }

    public RuntimeIshtarClass* DefineUnresolvedClass(RuntimeQualityTypeName* fullName)
    {
        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();

        *clazz = new RuntimeIshtarClass(fullName, null, _selfRef, clazz);

        clazz->Original.Flags |= ClassFlags.Unresolved;

        class_table->Add(clazz);

        return clazz;
    }

    public RuntimeIshtarMethod* GetEntryPoint()
    {
        var clazz = class_table->FirstOrNull(x => x->IsStatic && x->FindMethod("master()") is not null);

        if (clazz is null)
            return null;

        return clazz->FindMethod("master()");
    }

    public static RuntimeIshtarModule* Read(AppVault vault, byte[] arr, DirectNativeList<RuntimeIshtarModule>* deps, ModuleResolverCallback resolver)
    {
        var module = IshtarGC.AllocateImmortal<RuntimeIshtarModule>();

        *module = new RuntimeIshtarModule(vault, "unnamed", module);


        using var mem = new MemoryStream(arr);
        using var reader = new BinaryReader(mem);

        module->deps_table->AddRange(deps);
        deps->ForEach(x => module->Original->Value.Deps.Add(x->Original->Value));

        var idx = reader.ReadInt32(); // name index
        var vdx = reader.ReadInt32(); // version index
        var ilVersion = reader.ReadInt32();

        if (ilVersion != OpCodes.SetVersion)
        {
            var exp = new ILCompatibleException(ilVersion, OpCodes.SetVersion);

            vault.vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, $"Unable to load assembly: '{exp.Message}'.", vault.vm.Frames.ModuleLoaderFrame);
            return null;
        }

        // read strings table
        foreach (var _ in ..reader.ReadInt32())
        {
            var key = reader.ReadInt32();
            var value = reader.ReadIshtarString();

            vault.vm.println($"read string: [{key}] '{value}'");

            module->Original->Value.strings_table.Add(key, value);
            module->string_table->Add(key, StringStorage.Intern(value));
        }
        // read types table
        foreach (var _ in ..reader.ReadInt32())
        {
            var key = reader.ReadInt32();
            var asmName = reader.ReadIshtarString();
            var ns = reader.ReadIshtarString();
            var name = reader.ReadIshtarString();
            var t = new QualityTypeName(asmName, name, ns);

            vault.vm.println($"read typename: [{key}] '{t}'");

            module->Original->Value.types_table.Add(key, t);
            module->types_table->Add(key, t.T());
        }
        // read fields table
        foreach (var _ in ..reader.ReadInt32())
        {
            var key = reader.ReadInt32();
            var name = reader.ReadIshtarString();
            var clazz = reader.ReadIshtarString();
            var f = new FieldName(name, clazz);
            module->Original->Value.fields_table.Add(key, f);

            vault.vm.println($"read fieldname: [{key}] '{f}'");

            var field = IshtarGC.AllocateImmortal<RuntimeFieldName>();
            *field = new RuntimeFieldName(name, clazz);
            module->fields_table->Add(key, field);
        }

        // read deps refs
        foreach (var _ in ..reader.ReadInt32())
        {
            var name = reader.ReadIshtarString();
            var ver = Version.Parse(reader.ReadIshtarString());

            vault.vm.println($"read dep: [{name}@{ver}] ");

            if (module->Original->Value.Deps.Any(x => x.Version.Equals(ver) && x.Name.Equals(name)))
                continue;
            var dep = resolver(name, ver);
            module->Original->Value.Deps.Add(dep->Original->Value);
            module->deps_table->Add(dep);

        }
        // read class storage
        foreach (var _ in ..reader.ReadInt32())
        {
            var body = reader.ReadBytes(reader.ReadInt32());
            var @class = module->DecodeClass(body, module);

            vault.vm.println($"read class: [{@class->FullName->NameWithNS}] ");

            module->class_table->Add(@class);
            module->Original->Value.class_table.Add(@class->Original);
        }

        // restore unresolved types
        module->class_table->ForEach(@class =>
        {
            var parent = @class->Parent;
            if (!parent->IsUnresolved)
                return;

            vault.vm.println($"resolve class: [{parent->FullName->NameWithNS}] ");

            @class->ReplaceParent(parent->FullName != @class->FullName
                ? module->FindType(parent->FullName, true)
                : null);
        });

        module->class_table->ForEach(@class =>
        {
            @class->Methods->ForEach(method =>
            {
                if (!method->ReturnType->IsUnresolved)
                    return;
                vault.vm.println($"resolve method.retType: [{method->ReturnType->FullName->NameWithNS}] ");

                method->ReplaceReturnType(module->FindType(method->ReturnType->FullName, true));
            });

            @class->Methods->ForEach(method =>
            {
                if (method->Arguments->Length == 0)
                    return;
                method->Arguments->ForEach(arg =>
                {
                    if (!arg->Type->IsUnresolved)
                        return;
                    vault.vm.println($"resolve method.arg[]: [{arg->Type->FullName->NameWithNS}] ");
                    arg->ReplaceType(module->FindType(arg->Type->FullName, true));
                });
            });

            @class->Fields->ForEach(field => {
                if (!field->FieldType->IsUnresolved)
                    return;
                vault.vm.println($"resolve field.type[]: [{field->FieldType->FullName->NameWithNS}] ");

                field->ReplaceType(module->FindType(field->FieldType->FullName, true));
            });
        });

        var const_body_len = reader.ReadInt32();
        var const_body = reader.ReadBytes(const_body_len);

        module->Original->Value.const_table = const_body.ToConstStorage();

        module->Name = module->GetConstStringByIndex(idx);

        vault.vm.println($"Read {module->Name} module success");


        module->Original->Value.Version = Version.Parse(module->GetConstStringByIndex(vdx));
        module->aspects_table->AddRange(RuntimeAspect.Deconstruct(module->Original->Value.const_table.storage, vault.vm.Types));

        module->DefineBootstrapper();

        DistributionAspects(module);
        ValidateRuntimeTokens(module);
        module->LinkFFIMethods(module);
        InitVTables(module);

        return module;
    }
    [Conditional("VALIDATE_RUNTIME_TOKEN")]
    public static void ValidateRuntimeTokens(RuntimeIshtarModule* module) =>
        module->class_table->ForEach(@class =>
            VirtualMachine.Assert(@class->runtime_token != RuntimeToken.Default, WNE.TYPE_LOAD,
                $"Detected non-inited runtime token. type: '{@class->FullName->NameWithNS}'"));

    public static void InitVTables(RuntimeIshtarModule* ishtarModule)
        => ishtarModule->class_table->ForEach(x => x->init_vtable(ishtarModule->vm));


    // shit, todo: refactoring
    public static void DistributionAspects(RuntimeIshtarModule* module)
    {
        var errors = new StringBuilder();
        var classes = module->class_table;

        var class_eq = (RuntimeIshtarClass* x, string clazz) => x->Name.Equals(clazz);

        module->aspects_table->ForEach(aspect =>
        {
            switch (aspect->Original)
            {
                case AspectOfClass classAspect:
                {
                    var @class = classes->FirstOrNull(x => class_eq(x, classAspect.ClassName));
                    if (@class is not null)
                        @class->Aspects->Add(aspect);
                    else
                        errors.AppendLine($"Aspect '{classAspect.Name}': class '{classAspect.ClassName}' not found.");
                    break;
                }
                case AspectOfMethod ma:
                {
                    throw new NotImplementedException();

                    //var method = classes
                    //    .Where(x => class_eq(x, ma.ClassName))
                    //    .SelectMany(x => x.Methods)
                    //    .FirstOrDefault(method => method.Name.Equals(ma.MethodName));
                    //if (method is not null)
                    //    method.Aspects.Add(aspect);
                    //else
                    //    errors.AppendLine($"Aspect '{ma.Name}': method '{ma.ClassName}/{ma.MethodName}' not found.");
                    //break;
                }
                case AspectOfField fa when !fa.IsNative(): // currently ignoring native aspect, todo
                {
                    throw new NotImplementedException();

                    //var field = classes
                    //    .Where(x => class_eq(x, fa.ClassName))
                    //    .SelectMany(@class => @class.Fields)
                    //    .FirstOrDefault(field => field.Name.Equals(fa.FieldName));
                    //if (field is not null)
                    //    field.Aspects.Add(aspect);
                    //else
                    //    errors.AppendLine($"Aspect '{fa.Name}': field '{fa.ClassName}/{fa.FieldName}' not found.");
                    //break;
                }
            }
        });

        throw new NotImplementedException();
        //if (errors.Length != 0)
        //    module.Vault.vm.FastFail(WNE.TYPE_LOAD, $"\n{errors}", module.sys_frame);
    }

    public RuntimeIshtarClass* DecodeClass(byte[] arr, RuntimeIshtarModule* ishtarModule)
    {
        using var mem = new MemoryStream(arr);
        using var binary = new BinaryReader(mem);
        var className = binary.ReadTypeName(ishtarModule);
        var flags = (ClassFlags)binary.ReadInt16();

        var parentLen = binary.ReadInt16();

        var @class = default(RuntimeIshtarClass*);

        if (flags.HasFlag(ClassFlags.Special))
        {
            @class = ishtarModule->vm.Types->ByQualityName(className);
        }


        /*if (!@class->IsSpecial) continue;
           if (vault.vm.Types->All->Any(x => x->FullName == @class->FullName))
               RuntimeTypeForwarder.Indicate(vault.vm.Types, @class);
else
    vault.vm.FastFail(WNE.TYPE_LOAD, "Special type defined but forward director not found.",
        vault.vm.Frames.ModuleLoaderFrame);*/



        //if (parentLen > 1)
        //    throw new NotImplementedException();

        if (parentLen != 0)
        {
            var parentIdx = binary.ReadTypeName(ishtarModule);
            var parent = ishtarModule->FindType(parentIdx, true, false);

            if (@class is null)
                @class = ishtarModule->DefineClass(className, parent);

            

            if (parentLen > 1)
            {
                foreach (var _ in ..(parentLen - 1))
                {
                    var _1 = binary.ReadTypeName(ishtarModule);
                    var _2 = ishtarModule->FindType(parentIdx, true, false);
                }
            }
        }
        

        var len = binary.ReadInt32();
        
        


        @class->Original.Flags = flags;

        foreach (var _ in ..len)
        {
            var body =
                binary.ReadBytes(binary.ReadInt32());
            DecodeAndDefineMethod(body, @class, ishtarModule);
        }

        DecodeField(binary, @class, ishtarModule);

        return @class;
    }

    public void DecodeField(BinaryReader binary, RuntimeIshtarClass* @class, RuntimeIshtarModule* ishtarModule)
    {
        foreach (var _ in ..binary.ReadInt32())
        {
            var name = RuntimeFieldName.Resolve(binary.ReadInt32(), ishtarModule);
            var type_name = binary.ReadTypeName(ishtarModule);
            var type = ishtarModule->FindType(type_name, true, false);
            var flags = (FieldFlags) binary.ReadInt16();
            @class->DefineField(name, flags, type);
        }
    }

    public unsafe RuntimeIshtarMethod* DecodeAndDefineMethod(byte[] arr, RuntimeIshtarClass* @class, RuntimeIshtarModule* ishtarModule)
    {
        using var mem = new MemoryStream(arr);
        using var binary = new BinaryReader(mem);
        var idx = binary.ReadInt32();
        var flags = (MethodFlags)binary.ReadInt16();
        var bodysize = binary.ReadInt32();
        var stacksize = binary.ReadByte();
        var locals = binary.ReadByte();
        var retType = binary.ReadTypeName(ishtarModule);
        var args = ReadArguments(binary, ishtarModule);
        var body = binary.ReadBytes(bodysize);

        var methodName = ishtarModule->GetConstStringByIndex(idx);
        var returnType = ishtarModule->FindType(retType, true, false);

        var mth = @class->DefineMethod(methodName, returnType, flags, args);

        if (mth->IsExtern)
            return mth;

        ConstructIL(mth, body, stacksize, ishtarModule);

        return mth;
    }

    public RuntimeQualityTypeName* GetTypeNameByIndex(int idx, CallFrame frame)
    {
        if (types_table->TryGetValue(idx, out var result))
            return result;
        vm.FastFail(WNE.TYPE_LOAD, $"No found type by '{idx}'", frame);
        return null;
    }

    public string GetConstStringByIndex(int idx)
        => Original->Value.GetConstStringByIndex(idx);

    public void LinkFFIMethods(RuntimeIshtarModule* module) =>
        module->class_table->ForEach(x =>
        {
            x->Methods->ForEach(z =>
            {
                module->LinkFFIMethod(z);
            });
        });


    public void LinkFFIMethod(RuntimeIshtarMethod* method)
    {
        const string InternalTarget = "__internal__";
        if (!method->IsExtern)
            return;

        var aspect = method->Original.Aspects.FirstOrDefault(x => x.IsNative());
        var name = method->Name;
        if (aspect is null)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x1) Extern function without native aspect. [{name}]", sys_frame);
            return;
        }

        if (aspect.Arguments.Count != 2)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x1) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        if (aspect.Arguments[0].Value is not string importTarget)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        if (aspect.Arguments[1].Value is not string importFn)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        if (importTarget == InternalTarget)
        {
            name = VeinMethodBase.GetFullName(importFn, method->Original.Arguments);
            LinkInternalNative(name, method);
            return;
        }

        ForeignFunctionInterface.LinkExternalNativeLibrary(importTarget, importFn, method);
    }

    public void LinkFFIMethods(RuntimeIshtarMethod*[] methods)
    {
        const string InternalTarget = "__internal__";
        foreach (var method in methods) LinkFFIMethod(method);
    }
    private void LinkInternalNative(string name, RuntimeIshtarMethod* method)
    {
        var m = vm.FFI.GetMethod(name);

        if (m is null)
        {
            vm.FastFail(WNE.TYPE_LOAD,
                method->Name != name
                    ? $"Extern '{method->Name} -> {name}' method not found in native mapping."
                    : $"Extern '{method->Name}' method not found in native mapping.", sys_frame);

            vm.FFI.DisplayDefinedMapping();
            return;
        }

        if (m->PIInfo is { Addr: null, iflags: 0 })
        {
            vm.FastFail(WNE.TYPE_LOAD, $"Extern '{method->Name}' method has nul PIInfo", sys_frame);
            vm.FFI.DisplayDefinedMapping();
            return;
        }

        method->PIInfo = m->PIInfo;
    }


    private static DirectNativeList<ProtectedZone>* DeconstructExceptions(byte[] arr, int offset, RuntimeIshtarModule* module)
    {
        if (arr.Length == 0)
            return null;

        using var mem = new MemoryStream(arr);
        using var bin = new BinaryReader(mem);

        mem.Seek(offset, SeekOrigin.Begin);

        if (arr.Length == offset)
            return null;

        var magic = bin.ReadInt16();

        if (magic != -0xFF1)
            return null;

        var size = bin.ReadInt32();

        if (size == 0)
            return null;
        var result = DirectNativeList<ProtectedZone>.New(size);

        foreach (var _ in ..size)
        {
            var startAddr = bin.ReadInt32();
            var tryEndLabel = bin.ReadInt32();
            var endAddr = bin.ReadInt32();
            var filterAddr = bin.ReadIntArray().ToNative();
            var catchAddr = bin.ReadIntArray().ToNative();
            var catchClass = bin.ReadTypesArray(module->Original->Value).ToNative();
            var types = bin.ReadSpecialByteArray<ExceptionMarkKind>().ToNative();

            var item = IshtarGC.AllocateImmortal<ProtectedZone>();

            *item = new ProtectedZone(
                (uint)startAddr,
                (uint)endAddr,
                tryEndLabel,
                filterAddr,
                catchAddr,
                catchClass,
                types);

            result->Add(item);
        }

        return result;
    }
    internal static void ConstructIL(RuntimeIshtarMethod* method, byte[] body, short stacksize, RuntimeIshtarModule* module)
    {
        var offset = 0;
        var body_r = ILReader.Deconstruct(body, &offset, *method);
        var labels = ILReader.DeconstructLabels(body, &offset).ToArray().ToNative();
        var exceptions = DeconstructExceptions(body, offset, module);

        var header = IshtarGC.AllocateImmortal<MetaMethodHeader>();

        header->max_stack = stacksize;
        header->exception_handler_list = exceptions;


        header->code = IshtarGC.AllocateImmortal<uint>(body_r.opcodes.Count);

        for (var i = 0; i != body_r.opcodes.Count; i++)
            header->code[i] = body_r.opcodes[i];


        header->code_size = (uint)body_r.opcodes.Count;
        header->labels = labels;
        header->labels_map = body_r.map.ToNative();
    }


    private static DirectNativeList<RuntimeMethodArgument>* ReadArguments(BinaryReader binary, RuntimeIshtarModule* ishtarModule)
    {
        var args = DirectNativeList<RuntimeMethodArgument>.New(4);
        foreach (var _ in ..binary.ReadInt32())
        {
            var nIdx = binary.ReadInt32();
            var type = binary.ReadTypeName(ishtarModule);

            var a = RuntimeMethodArgument.Create(ishtarModule->vm.Types,
                ishtarModule->GetConstStringByIndex(nIdx),
                ishtarModule->FindType(type, true, false));
            args->Add(a);
        }
        return args;
    }

    private void DefineBootstrapper()
        => Bootstrapper = this.DefineClass(new QualityTypeName(Name, "boot", "<sys>").T(), null);

    public RuntimeIshtarClass* Bootstrapper { get; private set; }

    public CallFrame sys_frame => Vault->Value.vm.Frames.ModuleLoaderFrame;
}
public static unsafe class QualityTypeEx
{
    public static RuntimeQualityTypeName* ReadTypeName(this BinaryReader bin, RuntimeIshtarModule* module)
    {
        var typeIndex = bin.ReadInt32();


        if (!module->types_table->TryGetValue(typeIndex, out var result))
            throw new Exception($"TypeName by index '{typeIndex}' not found in '{module->Name}' module.");

        return result;
    }

    public static DirectNativeList<RuntimeQualityTypeName>* ToNative(this QualityTypeName[] types)
    {
        var t = DirectNativeList<RuntimeQualityTypeName>.New(types.Length);

        foreach (var name in types) t->Add(name.T());

        return t;
    }

    public static UnsafeDictionary<int, ILLabel>* ToNative(this Dictionary<int, (int pos, OpCodeValue opcode)> dictionary)
    {
        var dt = UnsafeDictionary<int, ILLabel>.New(dictionary.Count);

        foreach (var (idx, (pos, opcode)) in dictionary)
        {
            dt->Add(idx, new ILLabel(pos, opcode));
        }

        return dt;
    }

    public static NativeList<T> ToNative<T>(this T[] types) where T : unmanaged
    {
        var t = NativeList<T>.New(types.Length);

        foreach (var v in types) t.Add(v);

        return t;
    }
}

public unsafe struct RuntimeAspectArgument
{
    public RuntimeAspect* Owner { get; }
    public uint Index { get; }
    public stackval* Value { get; }

    public static RuntimeAspectArgument* Create(AspectArgument arg) => throw new NotImplementedException();
}

public unsafe struct RuntimeAspect
{
    private void* _vein_aspect_ref;
    private InternedString* _name;

    public Aspect Original
    {
        get => IshtarUnsafe.AsRef<Aspect>(_vein_aspect_ref);
        set => _vein_aspect_ref = IshtarUnsafe.AsPointer(ref value);
    }

    public string Name
    {
        get => StringStorage.GetStringUnsafe(_name);
        set => _name = StringStorage.Intern(value);
    }


    public RuntimeAspect(string name, List<AspectArgument> args, AspectTarget target, IshtarTypes* types)
    {
        Target = target;
        Name = name;

        foreach (var @ref in args)
            Arguments->Add(RuntimeAspectArgument.Create(@ref));
    }

    public static DirectNativeList<RuntimeAspect>* Deconstruct(Dictionary<FieldName, object> data, IshtarTypes* types)
    {
        var result = Aspect.Deconstruct(data);
        var lst = DirectNativeList<RuntimeAspect>.New(result.Length);

        foreach (var aspect in result)
        {
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            *asp = new RuntimeAspect(aspect.Name, aspect.Arguments, aspect.Target, types);

            lst->Add(asp);
        }

        return lst;
    }


    public AspectTarget Target { get; }

    public DirectNativeList<RuntimeAspectArgument>* Arguments { get; } = DirectNativeList<RuntimeAspectArgument>.New(4);

    public bool IsAlias() => Original.Name.Equals("alias", StringComparison.InvariantCultureIgnoreCase);
    public bool IsNative() => Original.Name.Equals("native", StringComparison.InvariantCultureIgnoreCase);
    public bool IsSpecial() => Original.Name.Equals("special", StringComparison.InvariantCultureIgnoreCase);
}
