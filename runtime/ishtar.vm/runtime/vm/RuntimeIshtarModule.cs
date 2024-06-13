namespace ishtar.runtime;

using collections;
using emit;
using ishtar;
using gc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using vein.exceptions;
using vein.extensions;
using vein.reflection;
using vein.runtime;

public unsafe struct RuntimeIshtarModule : IEq<RuntimeIshtarModule>, IDisposable
{
    public WeakRef<AppVault>* Vault { get; private set; }
    public VirtualMachine vm => Vault->Value.vm;
    public uint ID { get; internal set; }

    public IshtarVersion Version { get; internal set; }


    public NativeList<RuntimeIshtarClass>* class_table { get; private set; }
        = IshtarGC.AllocateList<RuntimeIshtarClass>();
    public NativeList<RuntimeIshtarModule>* deps_table { get; private set; }
        = IshtarGC.AllocateList<RuntimeIshtarModule>();
    public NativeList<RuntimeAspect>* aspects_table { get; private set; }
        = IshtarGC.AllocateList<RuntimeAspect>();

    public NativeDictionary<int, RuntimeQualityTypeName>* types_table { get; private set; }
        = IshtarGC.AllocateDictionary<int, RuntimeQualityTypeName>();

    public NativeDictionary<int, RuntimeFieldName>* fields_table { get; private set; }
        = IshtarGC.AllocateDictionary<int, RuntimeFieldName>();
    public NativeDictionary<int, InternedString>* string_table { get; private set; }
        = IshtarGC.AllocateDictionary<int, InternedString>();
    public RuntimeConstStorage* ConstStorage { get; set; }


    public void Dispose()
    {
        if (_name is null)
            return;

        var name = Name;
        VirtualMachine.GlobalPrintln($"Disposed module '{name}'");

        class_table->ForEach(x => x->Dispose());
        class_table->ForEach(IshtarGC.FreeImmortal);

        aspects_table->ForEach(x => x->Dispose());
        aspects_table->ForEach(IshtarGC.FreeImmortal);

        types_table->Clear();
        class_table->Clear();
        deps_table->Clear();
        aspects_table->Clear();

        IshtarGC.FreeDictionary(types_table);
        IshtarGC.FreeDictionary(fields_table);
        IshtarGC.FreeDictionary(string_table);
        IshtarGC.FreeList(class_table);
        IshtarGC.FreeList(deps_table);
        IshtarGC.FreeList(aspects_table);

        WeakRef<AppVault>.Free(Vault);
        
        class_table = null;
        deps_table = null;
        aspects_table = null;
        types_table = null;
        fields_table = null;
        string_table = null;
        _self = null;
        _name = null;
        Vault = null;
        if (ConstStorage is not null)
        {
            ConstStorage->Dispose();
            IshtarGC.FreeImmortal(ConstStorage);
        }
        VirtualMachine.GlobalPrintln($"end disposed module '{name}'");
    }


    private InternedString* _name;
    private RuntimeIshtarModule* _self;

    public RuntimeIshtarModule(AppVault vault, string name, RuntimeIshtarModule* self, IshtarVersion version)
    {
        _self = self;
        Vault = WeakRef<AppVault>.Create(vault);
        Version = version;
        _name = StringStorage.Intern(name);
    }

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

        bool filter(RuntimeIshtarClass* x) => RuntimeQualityTypeName.Eq(x->FullName, type);
    }

    
    public RuntimeIshtarClass* FindType(RuntimeQualityTypeName* type, bool findExternally, bool dropUnresolvedException)
    {
        if (!findExternally)
            findExternally = this.Name != type->AssemblyName;
        var result = class_table->FirstOrNull(filter);

        if (result is not null)
            return result;

        bool filter(RuntimeIshtarClass* x) => RuntimeQualityTypeName.Eq(x->FullName, type);
        
        if (!findExternally)
            throw new TypeNotFoundException($"'{type->NameWithNS}' not found in modules and dependency assemblies.");

        var dm = deps_table->FirstOrNull(x => x->FindType(type, true, dropUnresolvedException) is not null);


        if (dm is not null)
            return dm->FindType(type, true, dropUnresolvedException);

        throw new TypeNotFoundException($"'{type->NameWithNS}' not found in modules and dependency assemblies.");
    }



    public RuntimeIshtarClass* DefineClass(RuntimeQualityTypeName* fullName, RuntimeIshtarClass* parent)
    {
        var exist = class_table->FirstOrNull(x
            => x->FullName->NameWithNS.Equals(fullName->NameWithNS));
        
        if (exist is not null)
            return exist;

        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();

        *clazz = new RuntimeIshtarClass(fullName, parent, _self, clazz);

        
        class_table->Add(clazz);

        return clazz;
    }

    //public RuntimeIshtarClass* DefineUnresolvedClass(RuntimeQualityTypeName* fullName)
    //{
    //    var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();

    //    *clazz = new RuntimeIshtarClass(fullName, null, self, clazz);

    //    clazz->Flags |= ClassFlags.Unresolved;

    //    if (class_table->Any(x => RuntimeQualityTypeName.Eq(x->FullName, fullName)))
    //        throw new ClassAlreadyDefined("");
    //    class_table->Add(clazz);

    //    return clazz;
    //}

    public RuntimeIshtarMethod* GetEntryPoint()
    {
        var clazz = class_table->FirstOrNull(x => x->GetEntryPoint() is not null);

        if (clazz is null)
            return null;

        return clazz->GetEntryPoint();
    }

    public RuntimeIshtarMethod* GetSpecialEntryPoint(string name)
    {
        var clazz = class_table->FirstOrNull(x => x->GetSpecialEntryPoint(name) is not null);

        if (clazz is null)
            return null;

        return clazz->GetSpecialEntryPoint(name);
    }

    public static RuntimeIshtarModule* Read(AppVault vault, byte[] arr, NativeList<RuntimeIshtarModule>* deps, ModuleResolverCallback resolver)
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

            VirtualMachine.GlobalPrintln($"read string: [{key}] '{value}'");

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
            var t = new QualityTypeName(asmName, name, ns).T();

            var predefined = module->vm.Types->ByQualityName(t);
            if (predefined is not null)
            {
                VirtualMachine.GlobalPrintln($"read typename: [{key}] '{t->NameWithNS}' and linked by predefined type");
                module->class_table->Add(predefined);
            }
            else if (asmName.Equals(module->Name))
            {
                var c = module->DefineClass(t, module->vm.Types->ObjectClass);
                c->Flags |= ClassFlags.NotCompleted;
                VirtualMachine.GlobalPrintln($"read typename: [{key}] '{t->NameWithNS}' and set not completed");
            }



            module->types_table->Add(key, t);
        }
        // read fields table
        foreach (var _ in ..reader.ReadInt32())
        {
            var key = reader.ReadInt32();
            var name = reader.ReadIshtarString();
            var clazz = reader.ReadIshtarString();
            var f = new FieldName(name, clazz);

            VirtualMachine.GlobalPrintln($"read fieldname: [{key}] '{f}'");

            var field = IshtarGC.AllocateImmortal<RuntimeFieldName>();
            *field = new RuntimeFieldName(name, clazz);
            module->fields_table->Add(key, field);
        }

        // read deps refs
        foreach (var _ in ..reader.ReadInt32())
        {
            var name = reader.ReadIshtarString();
            var ver = IshtarVersion.Parse(reader.ReadIshtarString());

            VirtualMachine.GlobalPrintln($"read dep: [{name}@{ver}] ");

            if (module->deps_table->Any(x => x->Version.Equals(ver) && x->Name.Equals(name)))
                continue;
            var dep = resolver(name, ver);
            module->deps_table->Add(dep);

        }

        var deferClassBodies = new List<DeferClassBodyData>();
        // read class storage
        foreach (var _ in ..reader.ReadInt32())
        {
            var body = reader.ReadBytes(reader.ReadInt32());
            var metadata = module->DecodeClass(body, module);
            deferClassBodies.Add(metadata);

            //throw new ClassAlreadyDefined($"Class '{clazz->FullName->ToString()}' already defined in '{module->Name}' module");
        }

        // restore unresolved types
        module->class_table->ForEach(@class => {
            var parent = @class->Parent;
            if (parent is null)
                return;
            if (!parent->IsUnresolved)
                return;

            VirtualMachine.GlobalPrintln($"resolve class: [{parent->FullName->NameWithNS}] ");

            @class->ReplaceParent(parent->FullName != @class->FullName
                ? module->FindType(parent->FullName, true)
                : null);
        });

        module->class_table->ForEach(@class => {
            @class->Methods->ForEach(method => {
                if (!method->ReturnType->IsUnresolved)
                    return;
                VirtualMachine.GlobalPrintln($"resolve method.retType: [{method->ReturnType->FullName->NameWithNS}] ");

                method->ReplaceReturnType(module->FindType(method->ReturnType->FullName, true, true));
            });

            @class->Methods->ForEach(method => {
                if (method->Arguments->Length == 0)
                    return;
                method->Arguments->ForEach(arg => {
                    if (!arg->Type->IsUnresolved)
                        return;
                    VirtualMachine.GlobalPrintln($"resolve method.arg[]: [{arg->Type->FullName->NameWithNS}] ");
                    arg->ReplaceType(module->FindType(arg->Type->FullName, true, true));
                });
            });

            @class->Fields->ForEach(field => {
                if (!field->FieldType->IsUnresolved)
                    return;
                VirtualMachine.GlobalPrintln($"resolve field.type[]: [{field->FieldType->FullName->NameWithNS}] ");

                field->ReplaceType(module->FindType(field->FieldType->FullName, true, true));
            });
        });

        foreach (var body in deferClassBodies)
        {
            var clazz = body.Clazz;

            foreach (byte[] methodBody in body.Methods)
            {
                var method = DecodeAndDefineMethod(methodBody, clazz, module);
                VirtualMachine.GlobalPrintln($"constructed method: [{method->Name} at {method->Owner->FullName->NameWithNS}] ");
            }

            foreach (var bodyField in body.Fields)
            {
                var field = clazz->DefineField(bodyField.FieldName, bodyField.Flags,
                    module->FindType(bodyField.FieldType, true, true));
                VirtualMachine.GlobalPrintln($"constructed field: [{field->Name} at {field->Owner->FullName->NameWithNS}] ");
            }

            clazz->Flags &= ~ClassFlags.NotCompleted;
            VirtualMachine.GlobalPrintln($"Marked '{clazz->Name}' as completed");
            // todo restore parent
        }

        var const_body_len = reader.ReadInt32();
        var const_body = reader.ReadBytes(const_body_len);

        module->ConstStorage = IshtarGC.AllocateImmortal<RuntimeConstStorage>();
        *module->ConstStorage = new RuntimeConstStorage();

        FillConstStorage(module->ConstStorage, const_body, module->vm.Frames.ModuleLoaderFrame);


        VirtualMachine.GlobalPrintln($"Read {module->Name} module success");


        module->Version = IshtarVersion.Parse(module->GetConstStringByIndex(vdx));
        module->aspects_table->AddRange(RuntimeAspect.Deconstruct(module->ConstStorage, module->vm.Frames.ModuleLoaderFrame, vault.vm.Types));

        module->DefineBootstrapper();

        DistributionAspects(module);
        ValidateRuntimeTokens(module);
        module->LinkFFIMethods(module);
#if DEBUG
        module->vm.Jitter.GetExecutionModule().PrintToFile($"{module->Name}_ffi.ll");
#endif
        InitVTables(module);
        

        return module;
    }



    public static void FillConstStorage(RuntimeConstStorage* storage, byte[] arr, CallFrame* frame)
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

            stackval a = IshtarMarshal.LegacyBoxing(frame, type_code, value);
            
            storage->Stage(fn, a);
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

                    if (@class is not null && @class->IsUnresolved)
                        throw new UnresolvedIshtarClassDetected(@class->FullName);

                    if (@class is not null)
                    {
                        var exist = @class->Aspects->FirstOrNull(x => RuntimeAspect.Eq(x, aspect));

                        if (exist is not null)
                        {
                            if (exist == aspect)
                                break;
                            @class->Aspects->Remove(exist);
                        }

                        @class->Aspects->Add(aspect);
                    }
                    else
                        module->vm.println($"Aspect '{aspect->Name}': class '{StringStorage.GetStringUnsafe(classAspect.ClassName)}' not found.");
                    break;
                }
                case AspectTarget.Method:
                {
                    var ma = aspect->Union.MethodAspect;
                    var @class = classes->FirstOrNull(x => class_eq(x, ma.ClassName));

                    if (@class is not null && @class->IsUnresolved)
                        throw new UnresolvedIshtarClassDetected(@class->FullName);

                    if (@class is null)
                    {
                        module->vm.println($"Aspect '{aspect->Name}': method '{StringStorage.GetStringUnsafe(ma.ClassName)}/{StringStorage.GetStringUnsafe(ma.MethodName)}' not found. [no class found]");
                        return;
                    }
                    var method = @class->Methods->FirstOrNull(m => m->Name.Equals(StringStorage.GetStringUnsafe(ma.MethodName)));
                    if (method is not null)
                        method->Aspects->Add(aspect);
                    else
                    {
                        var methods = @class->Methods->ToList();
                        module->vm.println($"Aspect '{aspect->Name}': method '{StringStorage.GetStringUnsafe(ma.ClassName)}/{StringStorage.GetStringUnsafe(ma.MethodName)}' not found.");
                    }
                    break;
                }
                case AspectTarget.Field when !aspect->IsNative(): // currently ignoring native aspect, todo
                {
                    var fa = aspect->Union.FieldAspect;
                    var @class = classes->FirstOrNull(x => class_eq(x, fa.ClassName));

                    if (@class is not null && @class->IsUnresolved)
                        throw new UnresolvedIshtarClassDetected(@class->FullName);

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

    public DeferClassBodyData DecodeClass(byte[] arr, RuntimeIshtarModule* ishtarModule)
    {
        using var mem = new MemoryStream(arr);
        using var binary = new BinaryReader(mem);
        var className = binary.ReadTypeName(ishtarModule);
        var flags = (ClassFlags)binary.ReadInt16();

        var parentLen = binary.ReadInt16();

        
        bool filter(RuntimeIshtarClass* x) => RuntimeQualityTypeName.Eq(className, x->FullName);


        var @class = ishtarModule->class_table->FirstOrNull(filter);

       
        if (parentLen != 0)
        {
            var parentIdx = binary.ReadTypeName(ishtarModule);
            var parent = ishtarModule->FindType(parentIdx, true, false);

            if (@class is null)
                @class = ishtarModule->DefineClass(className, parent);
            else
                @class->ReplaceParent(parent);

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


        @class->Flags |= flags;


        var len = binary.ReadInt32();
        var methods = new List<byte[]>();
        foreach (var _ in ..len)
        {
            var body =
                binary.ReadBytes(binary.ReadInt32());

            methods.Add(body);
            //DecodeAndDefineMethod(body, @class, ishtarModule);
        }

        var fieldsData = DecodeField(binary, @class, ishtarModule);

        return new DeferClassBodyData(@class, methods, fieldsData);
    }

    public class DeferClassBodyData(RuntimeIshtarClass* clazz, List<byte[]> methods, List<DeferClassFieldData> fields)
    {
        public RuntimeIshtarClass* Clazz { get; } = clazz;
        public List<byte[]> Methods { get; } = methods;
        public List<DeferClassFieldData> Fields { get; } = fields;
    }

    public class DeferClassFieldData(RuntimeFieldName* fieldName, RuntimeQualityTypeName* fieldType, FieldFlags flags)
    {
        public RuntimeFieldName* FieldName { get; } = fieldName;
        public RuntimeQualityTypeName* FieldType { get; } = fieldType;
        public FieldFlags Flags { get; } = flags;
    }

    public List<DeferClassFieldData> DecodeField(BinaryReader binary, RuntimeIshtarClass* @class, RuntimeIshtarModule* ishtarModule)
    {
        var lst = new List<DeferClassFieldData>();
        foreach (var _ in ..binary.ReadInt32())
        {
            var name = RuntimeFieldName.Resolve(binary.ReadInt32(), ishtarModule);
            var type_name = binary.ReadTypeName(ishtarModule);
            //var type = ishtarModule->FindType(type_name, true, false);
            var flags = (FieldFlags) binary.ReadInt16();

            lst.Add(new DeferClassFieldData(name, type_name, flags));

            //@class->DefineField(name, flags, type);
        }

        return lst;
    }

    public static unsafe RuntimeIshtarMethod* DecodeAndDefineMethod(byte[] arr, RuntimeIshtarClass* @class, RuntimeIshtarModule* ishtarModule)
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

    public RuntimeQualityTypeName* GetTypeNameByIndex(int idx, CallFrame* frame)
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

        if (import_tg->Value.type is not VeinTypeCode.TYPE_STRING)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        var import_fn = aspect->Arguments->Get(1);

        if (import_fn->Value.type is not VeinTypeCode.TYPE_STRING)
        {
            vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{name}]", sys_frame);
            return;
        }

        var importTarget = StringStorage.GetString((InternedString*)aspect->Arguments->Get(0)->Value.data.p, sys_frame);
        var importFn = StringStorage.GetString((InternedString*)aspect->Arguments->Get(1)->Value.data.p, sys_frame);

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

        //if (m->PIInfo is { Addr: null, iflags: 0 })
        //{
        //    vm.FastFail(WNE.TYPE_LOAD, $"Extern '{method->Name}' method has nul PIInfo", sys_frame);
        //    vm.FFI.DisplayDefinedMapping();
        //    return;
        //}

        method->PIInfo = m->PIInfo;
    }


    private static NativeList<ProtectedZone>* DeconstructExceptions(byte[] arr, int offset, RuntimeIshtarModule* module)
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
        var result = IshtarGC.AllocateList<ProtectedZone>(size);
        
        foreach (var _ in ..size)
        {
            var startAddr = bin.ReadInt32();
            var tryEndLabel = bin.ReadInt32();
            var endAddr = bin.ReadInt32();
            var filterAddr = bin.ReadIntArray().ToNative();
            var catchAddr = bin.ReadIntArray().ToNative();
            var catchClass = bin.ReadTypesArray((x) => (nint)module->GetTypeNameByIndex(x, module->vm.Frames.ModuleLoaderFrame)).ToNative<RuntimeQualityTypeName>();
            var types = bin.ReadSpecialDirectByteArray().ToNative();

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


    private static NativeList<RuntimeMethodArgument>* ReadArguments(BinaryReader binary, RuntimeIshtarModule* ishtarModule)
    {
        var args = IshtarGC.AllocateList<RuntimeMethodArgument>();
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

    public CallFrame* sys_frame => Vault->Value.vm.Frames.ModuleLoaderFrame;
    public static bool Eq(RuntimeIshtarModule* p1, RuntimeIshtarModule* p2) => p1->ID == p2->ID;
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

    public static NativeList<T>* ToNative<T>(this List<nint> types) where T : unmanaged, IEq<T>
    {
        var t = IshtarGC.AllocateList<T>(types.Count);

        foreach (var name in types) t->Add((T*)name);

        return t;
    }

    public static AtomicNativeDictionary<int, ILLabel>* ToNative(this Dictionary<int, (int pos, OpCodeValue opcode)> dictionary)
    {
        var dt = IshtarGC.AllocateAtomicDictionary<int, ILLabel>(dictionary.Count);

        foreach (var (idx, (pos, opcode)) in dictionary)
        {
            dt->Add(idx, new ILLabel(pos, opcode));
        }

        return dt;
    }

    public static AtomicNativeList<T>* ToNative<T>(this T[] types) where T : unmanaged, IEquatable<T>
    {
        var t = IshtarGC.AllocateAtomicList<T>(types.Length);

        foreach (var v in types) t->Add(v);

        return t;
    }
}

public readonly unsafe struct RuntimeAspectArgument(RuntimeAspect* owner, uint index, stackval val, RuntimeAspectArgument* self) : IEq<RuntimeAspectArgument>, IDisposable
{
    public RuntimeAspect* Owner { get; } = owner;
    public uint Index { get; } = index;
    public stackval Value { get; } = val;
    public RuntimeAspectArgument* Self { get; } = self;


    public void Dispose() => IshtarGC.FreeImmortal(Self);


    public static RuntimeAspectArgument* Create(AspectArgument arg, stackval val, RuntimeAspect* owner)
    {
        var t = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();

        *t = new RuntimeAspectArgument(owner, (uint)arg.Index, val, t);

        return t;
    }

    public static NativeList<RuntimeAspectArgument>* Create(RuntimeAspectArgument* arr, int len)
    {
        var t = IshtarGC.AllocateList<RuntimeAspectArgument>(len);

        for (var i = 0; i < len; i++)
        {
            t->Add(arr);
            arr++;
        }

        return t;
    }

    public static bool Eq(RuntimeAspectArgument* p1, RuntimeAspectArgument* p2)
    {
        var d1 = p1->Value;
        var d2 = p2->Value;
        return p1->Index == p2->Index && stackval.Eq(ref d1, ref d2);
    }
}

public unsafe class UnresolvedIshtarClassDetected(RuntimeQualityTypeName* fullName)
    : Exception($"Detected unresolved '{fullName->ToString()}'");

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

[DebuggerDisplay("{_debugString}")]
public unsafe struct RuntimeAspect : IEq<RuntimeAspect>, IDisposable
{
    private RuntimeAspect* _self;
    private InternedString* _name;
    public RuntimeAspect_Union Union;
    public AspectTarget Target { get; }


    private string _debugString => $"[{StringStorage.GetStringUnsafe(_name)}] {Target}";

    public NativeList<RuntimeAspectArgument>* Arguments { get; } = IshtarGC.AllocateList<RuntimeAspectArgument>();

    public void Dispose()
    {
        if (_self is null)
            return;
        var n = StringStorage.GetStringUnsafe(_name);
        VirtualMachine.GlobalPrintln($"@@@@ Disposed aspect '{n}'");
        _name = null;
        Union = default;
        if (Arguments is not null)
        {
            Arguments->ForEach(x => x->Dispose());
            IshtarGC.FreeList(Arguments);
        }

        _self = null;

        VirtualMachine.GlobalPrintln($"@@@@ end dispose aspect '{n}'");
    }


    public string Name
    {
        get => StringStorage.GetStringUnsafe(_name);
        set => _name = StringStorage.Intern(value);
    }


    public RuntimeAspect(string name, NativeList<RuntimeAspectArgument>* args, AspectTarget target, RuntimeAspect* self)
    {
        _self = self;
        Target = target;
        Union = default;
        Name = name;
        Arguments->AddRange(args);
    }

    public static NativeList<RuntimeAspect>* Deconstruct(RuntimeConstStorage* data, CallFrame* frame, IshtarTypes* types)
        => InternalDeconstruct(data);

    private static unsafe NativeList<RuntimeAspect>* InternalDeconstruct(RuntimeConstStorage* data)
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
        var asp_class = data->RawGetWithFilter(x => x->Class.Contains("aspect/") && x->Class.Contains("/class/") && !x->Class.Contains("/method/") && !x->Class.Contains("/field/"))
            .Select(x => (*((RuntimeFieldName*)x.field),x.obj)).ToList();


        var aspects = IshtarGC.AllocateList<RuntimeAspect>(asp_methods.Count + asp_fields.Count + asp_class.Count);

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

            
            var args = IshtarGC.AllocateList<RuntimeAspectArgument>(lst.Count);
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            foreach (var (key, value) in lst)
            {
                var index = key.Fullname.Split("._").Last();

                var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();
                *arg = new RuntimeAspectArgument(asp, uint.Parse(index), value, arg);
                args->Add(arg);
            }
            
            *asp = new RuntimeAspect(aspectName, args, AspectTarget.Class, asp);

            asp->Union.ClassAspect.ClassName = StringStorage.Intern(aspectClass);

            if (aspects->FirstOrNull(x => RuntimeAspect.Eq(x, asp)) is not null)
            {
                var aspects2 = aspects->ToList();
            }
            
            aspects->Add(asp);
        }
        foreach (var groupMethod in groupMethods)
        {
            var lst = groupMethod.ToList();
            var aspectName = groupMethod.Key.Split('/')[0];
            var aspectClass = groupMethod.Key.Split('/')[1];
            var aspectMethod = groupMethod.Key.Split('/')[2];

            var args = IshtarGC.AllocateList<RuntimeAspectArgument>(lst.Count);
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            foreach (var (key, value) in groupMethod)
            {
                var index = key.Fullname.Split("._").Last();

                var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();
                *arg = new RuntimeAspectArgument(asp, uint.Parse(index), value, arg);
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


            var args = IshtarGC.AllocateList<RuntimeAspectArgument>(lst.Count);
            var asp = IshtarGC.AllocateImmortal<RuntimeAspect>();

            foreach (var (key, value) in groupMethod)
            {
                var index = key.Fullname.Split("._").Last();

                var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();
                *arg = new RuntimeAspectArgument(asp, uint.Parse(index), value, arg);
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
    public static bool Eq(RuntimeAspect* p1, RuntimeAspect* p2)
    {
        var eqOther = p1->Name.Equals(p2->Name) &&
                      p1->Target == p2->Target &&
                      p1->Arguments->Length == p2->Arguments->Length &&
                      _list_eq(p1->Arguments, p2->Arguments);

        if (!eqOther)
            return false;

        return p1->Target switch
        {
            AspectTarget.Field =>
                InternedString.Eq(p1->Union.FieldAspect.FieldName, p2->Union.FieldAspect.FieldName) &&
                InternedString.Eq(p1->Union.FieldAspect.ClassName, p2->Union.FieldAspect.ClassName),
            AspectTarget.Method =>
                InternedString.Eq(p1->Union.MethodAspect.MethodName, p2->Union.MethodAspect.MethodName) &&
                InternedString.Eq(p1->Union.MethodAspect.ClassName, p2->Union.MethodAspect.ClassName),
            AspectTarget.Class =>
                InternedString.Eq(p1->Union.ClassAspect.ClassName, p2->Union.ClassAspect.ClassName),
            _ => false
        };
    }

    private static bool _list_eq(NativeList<RuntimeAspectArgument>* list1, NativeList<RuntimeAspectArgument>* list2)
    {
        if (list1->Count != list2->Count)
            return false;

        var flag = true;

        for (int i = 0; i < list1->Count; i++)
        {
            var i1 = list1->Get(i);
            var i2 = list2->Get(i);
            flag &= RuntimeAspectArgument.Eq(i1, i2);
        }

        return flag;
    }
}
