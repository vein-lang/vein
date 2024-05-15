namespace ishtar.runtime;

using collections;
using emit;
using emit.extensions;
using ishtar;
using vm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using vein.exceptions;
using vein.extensions;
using vein.reflection;
using vein.runtime;
using System.Runtime.InteropServices;

public unsafe struct RuntimeIshtarModule(AppVault vault, string name, RuntimeIshtarModule* self, IshtarVersion version)
{
    public WeakRef<AppVault>* Vault { get; } = WeakRef<AppVault>.Create(vault);
    public VirtualMachine vm => Vault->Value.vm;
    public ushort ID { get; internal set; }

    private IshtarVersion _version = version;


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
        = DirectNativeDictionary<int, InternedString>.New(false);
    public RuntimeConstStorage* ConstStorage { get; set; }


    private InternedString* _name = StringStorage.Intern(name);

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

        using var enumerator = deps_table->GetEnumerator();

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

        using var enumerator = deps_table->GetEnumerator();

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

        if (!findExternally)
            return createResult(self);

        var dm = deps_table->FirstOrNull(x => x->FindType(type, true, dropUnresolvedException) is not null);


        if (dm is not null)
            return dm->FindType(type, true, dropUnresolvedException);

        return createResult(self);
    }

    public RuntimeIshtarClass* FindType(RuntimeQualityTypeName* type, bool findExternally, bool dropUnresolvedException)
    {
        if (!findExternally)
            findExternally = this.Name != type->AssemblyName;
        var result = class_table->FirstOrNull(filter);

        if (result is not null)
            return result;

        bool filter(RuntimeIshtarClass* x) => x->FullName->Equals(type);

        RuntimeIshtarClass* createResult(RuntimeIshtarModule* @this)
        {
            if (dropUnresolvedException /*|| findExternally*/)
                throw new TypeNotFoundException($"'{type->NameWithNS}' not found in modules and dependency assemblies.");
            return @this->DefineUnresolvedClass(type);
        }

        if (!findExternally)
            return createResult(self);

        var dm = deps_table->FirstOrNull(x => x->FindType(type, true, dropUnresolvedException) is not null);


        if (dm is not null)
            return dm->FindType(type, true, dropUnresolvedException);

        return createResult(self);
    }



    public RuntimeIshtarClass* DefineClass(RuntimeQualityTypeName* fullName, RuntimeIshtarClass* parent)
    {
        var exist = class_table->FirstOrNull(x
            => x->FullName->NameWithNS.Equals(fullName->NameWithNS));
        
        if (exist is not null)
            return exist;

        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();

        *clazz = new RuntimeIshtarClass(fullName, parent, self, clazz);

        
        class_table->Add(clazz);

        return clazz;
    }

    public RuntimeIshtarClass* DefineUnresolvedClass(RuntimeQualityTypeName* fullName)
    {
        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();

        *clazz = new RuntimeIshtarClass(fullName, null, self, clazz);

        clazz->Flags |= ClassFlags.Unresolved;

        if (class_table->Any(x => x->FullName->NameWithNS.Equals(fullName->NameWithNS)))
            throw new ClassAlreadyDefined("");
        class_table->Add(clazz);

        return clazz;
    }

    public RuntimeIshtarMethod* GetEntryPoint()
    {
        var clazz = class_table->FirstOrNull(x => x->GetEntryPoint() is not null);

        if (clazz is null)
            return null;

        return clazz->GetEntryPoint();
    }

    public static RuntimeIshtarModule* Read(AppVault vault, byte[] arr, DirectNativeList<RuntimeIshtarModule>* deps, ModuleResolverCallback resolver)
    {
        var module = IshtarGC.AllocateImmortal<RuntimeIshtarModule>();

        *module = new RuntimeIshtarModule(vault, "unnamed", module, new IshtarVersion());


        using var mem = new MemoryStream(arr);
        using var reader = new BinaryReader(mem);

        module->deps_table->AddRange(deps);
        deps->ForEach(x => module->deps_table->Add(x));

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

           // vault.vm.println($"read string: [{key}] '{value}'");

            module->string_table->Add(key, StringStorage.Intern(value));
        }

        module->Name = module->GetConstStringByIndex(idx);

        // read types table
        foreach (var _ in ..reader.ReadInt32())
        {
            var key = reader.ReadInt32();
            var asmName = reader.ReadIshtarString();
            var ns = reader.ReadIshtarString();
            var name = reader.ReadIshtarString();
            var t = new QualityTypeName(asmName, name, ns);

            //vault.vm.println($"read typename: [{key}] '{t}'");

            module->types_table->Add(key, t.T());
        }
        // read fields table
        foreach (var _ in ..reader.ReadInt32())
        {
            var key = reader.ReadInt32();
            var name = reader.ReadIshtarString();
            var clazz = reader.ReadIshtarString();
            var f = new FieldName(name, clazz);

            //vault.vm.println($"read fieldname: [{key}] '{f}'");

            var field = IshtarGC.AllocateImmortal<RuntimeFieldName>();
            *field = new RuntimeFieldName(name, clazz);
            module->fields_table->Add(key, field);
        }

        // read deps refs
        foreach (var _ in ..reader.ReadInt32())
        {
            var name = reader.ReadIshtarString();
            var ver = IshtarVersion.Parse(reader.ReadIshtarString());

            //vault.vm.println($"read dep: [{name}@{ver}] ");

            if (module->deps_table->Any(x => x->_version.Equals(ver) && x->Name.Equals(name)))
                continue;
            var dep = resolver(name, ver);
            module->deps_table->Add(dep);

        }
        // read class storage
        foreach (var _ in ..reader.ReadInt32())
        {
            var body = reader.ReadBytes(reader.ReadInt32());
            var @class = module->DecodeClass(body, module);

            bool filter(RuntimeIshtarClass* x) => @class->FullName->Equals(x->FullName);


            var clazz = module->class_table->FirstOrNull(filter);

            if (clazz is null)
                module->class_table->Add(@class);
            else if (clazz->IsUnresolved)
                module->class_table->ReplaceTo(clazz, @class);
            else
                module->class_table->ReplaceTo(clazz, @class);
            //throw new ClassAlreadyDefined($"Class '{clazz->FullName->ToString()}' already defined in '{module->Name}' module");
        }

        // restore unresolved types
        module->class_table->ForEach(@class =>
        {
            var parent = @class->Parent;
            if (parent is null)
                return;
            if (!parent->IsUnresolved)
                return;

            //vault.vm.println($"resolve class: [{parent->FullName->NameWithNS}] ");

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
                //vault.vm.println($"resolve method.retType: [{method->ReturnType->FullName->NameWithNS}] ");

                method->ReplaceReturnType(module->FindType(method->ReturnType->FullName, true, true));
            });

            @class->Methods->ForEach(method =>
            {
                if (method->Arguments->Length == 0)
                    return;
                method->Arguments->ForEach(arg =>
                {
                    if (!arg->Type->IsUnresolved)
                        return;
                    //vault.vm.println($"resolve method.arg[]: [{arg->Type->FullName->NameWithNS}] ");
                    arg->ReplaceType(module->FindType(arg->Type->FullName, true, true));
                });
            });

            @class->Fields->ForEach(field => {
                if (!field->FieldType->IsUnresolved)
                    return;
                //vault.vm.println($"resolve field.type[]: [{field->FieldType->FullName->NameWithNS}] ");

                field->ReplaceType(module->FindType(field->FieldType->FullName, true, true));
            });
        });

        var const_body_len = reader.ReadInt32();
        var const_body = reader.ReadBytes(const_body_len);

        module->ConstStorage = IshtarGC.AllocateImmortal<RuntimeConstStorage>();
        *module->ConstStorage = new RuntimeConstStorage();

        FillConstStorage(module->ConstStorage, const_body, module->vm.Frames.ModuleLoaderFrame);


        vault.vm.println($"Read {module->Name} module success");


        module->_version = IshtarVersion.Parse(module->GetConstStringByIndex(vdx));
        module->aspects_table->AddRange(RuntimeAspect.Deconstruct(module->ConstStorage, module->vm.Frames.ModuleLoaderFrame, vault.vm.Types));

        module->DefineBootstrapper();

        DistributionAspects(module);
        ValidateRuntimeTokens(module);
        module->LinkFFIMethods(module);
        InitVTables(module);

        return module;
    }

    public static void FillConstStorage(RuntimeConstStorage* storage, byte[] arr, CallFrame frame)
    {
        using var mem = new MemoryStream(arr);
        using var bin = new BinaryReader(mem);

        foreach (var _ in ..bin.ReadInt32())
        {
            var type_code = (VeinTypeCode)bin.ReadInt32();
            var fullname = bin.ReadIshtarString();
            var value = bin.ReadIshtarString();
            var fn = IshtarGC.AllocateImmortal<RuntimeFieldName>();

            *fn = new RuntimeFieldName(StringStorage.Intern(fullname));

            var go = frame.GetGC().ToIshtarObject_Raw(Convert.ChangeType(value, type_code.ToCLRTypeCode()), frame);

            storage->Stage(fn, go);
        }
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
        var classes = module->class_table;

        var listD = new List<RuntimeIshtarClass>();
        module->class_table->ForEach(x =>
        {
            if (x->IsValid)
                listD.Add(*x);
        });

        var class_eq = (RuntimeIshtarClass* x, InternedString* clazz) =>
        {
            return x->Name.Equals(StringStorage.GetStringUnsafe(clazz));
        };

        module->aspects_table->ForEach(aspect =>
        {
            switch (aspect->Target)
            {
                case AspectTarget.Class:
                {
                    var classAspect = aspect->Union.ClassAspect;
                    var @class = classes->FirstOrNull(x => class_eq(x, classAspect.ClassName));
                    if (@class is not null)
                        @class->Aspects->Add(aspect);
                    else
                        module->vm.println($"Aspect '{aspect->Name}': class '{StringStorage.GetStringUnsafe(classAspect.ClassName)}' not found.");
                    break;
                }
                case AspectTarget.Method:
                {
                    var ma = aspect->Union.MethodAspect;
                    var @class = classes->FirstOrNull(x => class_eq(x, ma.ClassName));
                    if (@class is null)
                    {
                        module->vm.println($"Aspect '{aspect->Name}': method '{StringStorage.GetStringUnsafe(ma.ClassName)}/{StringStorage.GetStringUnsafe(ma.MethodName)}' not found. [no class found]");
                        return;
                    }
                    var method = @class->Methods->FirstOrNull(m => m->Name.Equals(StringStorage.GetStringUnsafe(ma.MethodName)));
                    if (method is not null)
                        method->Aspects->Add(aspect);
                    else
                        module->vm.println($"Aspect '{aspect->Name}': method '{StringStorage.GetStringUnsafe(ma.ClassName)}/{StringStorage.GetStringUnsafe(ma.MethodName)}' not found.");
                    break;
                }
                case AspectTarget.Field when !aspect->IsNative(): // currently ignoring native aspect, todo
                {
                    var fa = aspect->Union.FieldAspect;
                    var @class = classes->FirstOrNull(x => class_eq(x, fa.ClassName));

                    if (@class is null)
                    {
                        module->vm.println($"Aspect '{aspect->Name}': field '{StringStorage.GetStringUnsafe(fa.ClassName)}/{StringStorage.GetStringUnsafe(fa.FieldName)}' not found. [no class found]");
                        return;
                    }
                    var field = @class->Fields->FirstOrNull(m => m->Name.Equals(StringStorage.GetStringUnsafe(fa.FieldName)));
                    if (field is not null)
                        field->Aspects->Add(aspect);
                    else
                        module->vm.println($"Aspect '{aspect->Name}': field '{StringStorage.GetStringUnsafe(fa.ClassName)}/{StringStorage.GetStringUnsafe(fa.FieldName)}' not found.");
                    break;
                }
            }
        });
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

            if (@class is null)
                vm.println($"No found registered special '{className->NameWithNS}' type for forwarding");
        }

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
        else if (@class is null)
            @class = ishtarModule->DefineClass(className, null);
        

        var len = binary.ReadInt32();
        
        
        @class->Flags = flags;

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

    public string GetConstStringByIndex(int index)
    {
        if (string_table->TryGetValue(index, out var value))
            return StringStorage.GetStringUnsafe(value);
        throw new AggregateException($"String by index  '{index}' not found in module '{Name}'.");
    }

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

        var aspect = method->Aspects->FirstOrNull(x => x->IsNative());
        var name = method->Name;
        if (aspect is null)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x1) Extern function without native aspect. [{name}]", sys_frame);
            return;
        }

        if (aspect->Arguments->Length != 2)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x1) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        var import_tg = aspect->Arguments->Get(0);

        if (import_tg->Value->clazz->TypeCode is not VeinTypeCode.TYPE_STRING)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        var import_fn = aspect->Arguments->Get(1);

        if (import_fn->Value->clazz->TypeCode is not VeinTypeCode.TYPE_STRING)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        var importTarget = IshtarMarshal.ToDotnetString(aspect->Arguments->Get(0)->Value, vm.Frames.NativeLoader);
        var importFn = IshtarMarshal.ToDotnetString(aspect->Arguments->Get(1)->Value, vm.Frames.NativeLoader);

        if (importTarget == InternalTarget)
        {
            var args = new List<(string argName, string typeName)>();


            method->Arguments->ForEach(x =>
            {
                args.Add((StringStorage.GetStringUnsafe(x->Name), x->Type->Name));
            });

            name = VeinMethodBase.GetFullName(importFn, args);
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
                method->RawName != name
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
            var catchClass = bin.ReadTypesArray((x) => (nint)module->GetTypeNameByIndex(x, module->vm.Frames.ModuleLoaderFrame)).ToNative<RuntimeQualityTypeName>();
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

        method->Header = IshtarGC.AllocateImmortal<MetaMethodHeader>();

        *method->Header = new MetaMethodHeader();

        method->Header->max_stack = stacksize;
        method->Header->exception_handler_list = exceptions;


        method->Header->code = IshtarGC.AllocateImmortal<uint>(body_r.opcodes.Count);

        for (var i = 0; i != body_r.opcodes.Count; i++)
            method->Header->code[i] = body_r.opcodes[i];


        method->Header->code_size = (uint)body_r.opcodes.Count;
        method->Header->labels = labels;
        method->Header->labels_map = body_r.map.ToNative();
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

    public static DirectNativeList<T>* ToNative<T>(this List<nint> types) where T : unmanaged
    {
        var t = DirectNativeList<T>.New(types.Count);

        foreach (var name in types) t->Add((T*)name);

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

public readonly unsafe struct RuntimeAspectArgument(RuntimeAspect* owner, uint index, IshtarObject* val)
{
    public RuntimeAspect* Owner { get; } = owner;
    public uint Index { get; } = index;
    public IshtarObject* Value { get; } = val;

    public static RuntimeAspectArgument* Create(AspectArgument arg, IshtarObject* val, RuntimeAspect* self)
    {
        var t = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();

        *t = new RuntimeAspectArgument(self, (uint)arg.Index, val);

        return t;
    }

    public static DirectNativeList<RuntimeAspectArgument>* Create(RuntimeAspectArgument* arr, int len)
    {
        var t = DirectNativeList<RuntimeAspectArgument>.New(len);

        for (var i = 0; i < len; i++)
        {
            t->Add(arr);
            arr++;
        }

        return t;
    }
}

/*public class AspectArgument
   {
       public Aspect Owner { get; }
       public object Value { get; }
       public int Index { get; }
   
       public AspectArgument(Aspect aspect, object value, int index)
       {
           Owner = aspect;
           Value = value;
           Index = index;
       }
   }
   
   public class AspectOfClass : Aspect
   {
       public string ClassName { get; }
       public AspectOfClass(string name, string className) : base(name, AspectTarget.Class)
           => ClassName = className;
       public override string ToString() => $"Aspect '{Name}' for '{ClassName}' class";
   }
   
   public class AspectOfMethod : Aspect
   {
       public string ClassName { get; }
       public string MethodName { get; }
       public AspectOfMethod(string name, string className, string methodName) : base(name, AspectTarget.Method)
       {
           ClassName = className;
           MethodName = methodName;
       }
       public override string ToString() => $"Aspect '{Name}' for '{ClassName}/{MethodName}(..)' method";
   }
   public class AspectOfField : Aspect
   {
       public string ClassName { get; }
       public string FieldName { get; }
       public AspectOfField(string name, string className, string fieldName) : base(name, AspectTarget.Field)
       {
           ClassName = className;
           FieldName = fieldName;
       }
       public override string ToString() => $"Aspect '{Name}' for '{ClassName}/{FieldName}' field";
   }
   
   public class AliasAspect
   {
       public AliasAspect(Aspect aspect)
       {
           Debug.Assert(aspect.IsAlias());
           Debug.Assert(aspect.Arguments.Count == 1);
           Name = (string)aspect.Arguments.Single().Value;
       }
   
       public string Name { get; }
   }*/
[StructLayout(LayoutKind.Explicit)]
public unsafe struct RuntimeAspect_Union
{
    [FieldOffset(0)]
    public RuntimeAspect_Class ClassAspect;

    [FieldOffset(0)]
    public RuntimeAspect_Method MethodAspect;

    [FieldOffset(0)]
    public RuntimeAspect_Field FieldAspect;
}

public unsafe struct RuntimeAspect_Class
{
    public InternedString* ClassName;
}
public unsafe struct RuntimeAspect_Method
{
    public InternedString* ClassName;
    public InternedString* MethodName;
}

public unsafe struct RuntimeAspect_Field
{
    public InternedString* ClassName;
    public InternedString* FieldName;
}

public unsafe struct RuntimeAspect
{
    private readonly RuntimeAspect* _self;
    private InternedString* _name;
    public RuntimeAspect_Union Union;
    public AspectTarget Target { get; }

    public DirectNativeList<RuntimeAspectArgument>* Arguments { get; } = DirectNativeList<RuntimeAspectArgument>.New(4);


    public string Name
    {
        get => StringStorage.GetStringUnsafe(_name);
        set => _name = StringStorage.Intern(value);
    }


    public RuntimeAspect(string name, DirectNativeList<RuntimeAspectArgument>* args, AspectTarget target, RuntimeAspect* self)
    {
        _self = self;
        Target = target;
        Union = default;
        Name = name;
        Arguments->AddRange(args);
    }

    public static DirectNativeList<RuntimeAspect>* Deconstruct(RuntimeConstStorage* data, CallFrame frame, IshtarTypes* types)
        => InternalDeconstruct(data);

    private static unsafe DirectNativeList<RuntimeAspect>* InternalDeconstruct(RuntimeConstStorage* data)
    {
        static AspectTarget getTarget(FieldName name)
        {
            if (name.fullName.Contains("/method/"))
                return AspectTarget.Method;
            if (name.fullName.Contains("/field/"))
                return AspectTarget.Field;
            if (name.fullName.Contains("/class/"))
                return AspectTarget.Class;

            throw new UnknownAspectTargetException(name);
        }

        


        var asp_methods = data->RawGetWithFilter(x => x->Class.Contains("aspect/") && x->Class.Contains("/method/"))
            .Select(x => (*((RuntimeFieldName*)x.field),x.obj)).ToList();
        var asp_fields = data->RawGetWithFilter(x => x->Class.Contains("aspect/") && x->Class.Contains("/field/"))
            .Select(x => (*((RuntimeFieldName*)x.field),x.obj)).ToList();
        var asp_class = data->RawGetWithFilter(x => x->Class.Contains("aspect/") && x->Class.Contains("/class/") && !x->Class.Contains("/method/"))
            .Select(x => (*((RuntimeFieldName*)x.field),x.obj)).ToList();


        var aspects = DirectNativeList<RuntimeAspect>.New(asp_methods.Count + asp_fields.Count + asp_class.Count);

        // rly shit
        var groupClasses = asp_class.GroupBy(x =>
                x.Item1.Fullname.Replace("aspect/", "")
                    .Replace("/class", "")
                    .Split('.').First());
        var groupMethods = asp_methods.GroupBy(x =>
                x.Item1.Fullname.Replace("aspect/", "")
                    .Replace("/class", "")
                    .Replace("/method", "")
                    .Split('.').First());
        var groupFields = asp_fields.GroupBy(x =>
                x.Item1.Fullname.Replace("aspect/", "")
                    .Replace("/class", "")
                    .Replace("/field", "")
                    .Split('.').First());

        foreach (var groupClass in groupClasses)
        {
            var lst = groupClass.ToList();
            var aspectName = groupClass.Key.Split('/').First();
            var aspectClass = groupClass.Key.Split('/').Last();

            var args = DirectNativeList<RuntimeAspectArgument>.New(lst.Count);
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            foreach (var (key, value) in lst)
            {
                var index = key.Fullname.Split("._").Last();

                var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();
                *arg = new RuntimeAspectArgument(asp, uint.Parse(index), (IshtarObject*)value);
                args->Add(arg);
            }
            
            *asp = new RuntimeAspect(aspectName, args, AspectTarget.Class, asp);

            asp->Union.ClassAspect.ClassName = StringStorage.Intern(aspectClass);

            aspects->Add(asp);
        }
        foreach (var groupMethod in groupMethods)
        {
            var lst = groupMethod.ToList();
            var aspectName = groupMethod.Key.Split('/')[0];
            var aspectClass = groupMethod.Key.Split('/')[1];
            var aspectMethod = groupMethod.Key.Split('/')[2];

            var args = DirectNativeList<RuntimeAspectArgument>.New(lst.Count);
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            foreach (var (key, value) in groupMethod)
            {
                var index = key.Fullname.Split("._").Last();

                var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();
                *arg = new RuntimeAspectArgument(asp, uint.Parse(index), (IshtarObject*)value);
                args->Add(arg);
            }
            *asp = new RuntimeAspect(aspectName, args, AspectTarget.Method, asp);

            asp->Union.MethodAspect.ClassName = StringStorage.Intern(aspectClass);
            asp->Union.MethodAspect.MethodName = StringStorage.Intern(aspectMethod);


            aspects->Add(asp);
        }
        foreach (var groupMethod in groupFields)
        {
            var lst = groupMethod.ToList();
            var aspectName = groupMethod.Key.Split('/')[0];
            var aspectClass = groupMethod.Key.Split('/')[1];
            var aspectField = groupMethod.Key.Split('/')[2];


            var args = DirectNativeList<RuntimeAspectArgument>.New(lst.Count);
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            foreach (var (key, value) in groupMethod)
            {
                var index = key.Fullname.Split("._").Last();

                var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();
                *arg = new RuntimeAspectArgument(asp, uint.Parse(index), (IshtarObject*)value);
                args->Add(arg);
            }
            *asp = new RuntimeAspect(aspectName, args, AspectTarget.Field, asp);

            asp->Union.FieldAspect.ClassName = StringStorage.Intern(aspectClass);
            asp->Union.FieldAspect.FieldName = StringStorage.Intern(aspectField);

            aspects->Add(asp);
        }

        return aspects;
    }


    public bool IsAlias() => Name.Equals("alias", StringComparison.InvariantCultureIgnoreCase);
    public bool IsNative() => Name.Equals("native", StringComparison.InvariantCultureIgnoreCase);
    public bool IsSpecial() => Name.Equals("special", StringComparison.InvariantCultureIgnoreCase);
}
