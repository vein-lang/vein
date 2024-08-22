namespace ishtar;

using runtime;
using vein.runtime;
using collections;
using static vein.runtime.VeinTypeCode;
using runtime.gc;

[CTypeExport("ishtar_types_t")]
public readonly unsafe struct IshtarTypes(
    RuntimeIshtarClass* objectClass,
    RuntimeIshtarClass* valueTypeClass,
    RuntimeIshtarClass* voidClass,
    RuntimeIshtarClass* stringClass,
    RuntimeIshtarClass* byteClass,
    RuntimeIshtarClass* sByteClass,
    RuntimeIshtarClass* int32Class,
    RuntimeIshtarClass* int16Class,
    RuntimeIshtarClass* int64Class,
    RuntimeIshtarClass* uInt32Class,
    RuntimeIshtarClass* uInt16Class,
    RuntimeIshtarClass* uInt64Class,
    RuntimeIshtarClass* halfClass,
    RuntimeIshtarClass* floatClass,
    RuntimeIshtarClass* doubleClass,
    RuntimeIshtarClass* decimalClass,
    RuntimeIshtarClass* boolClass,
    RuntimeIshtarClass* charClass,
    RuntimeIshtarClass* arrayClass,
    RuntimeIshtarClass* exceptionClass,
    RuntimeIshtarClass* rawClass,
    RuntimeIshtarClass* aspectClass,
    RuntimeIshtarClass* functionClass,
    RuntimeIshtarClass* rangeClass)
{
    public readonly RuntimeIshtarClass* ObjectClass = objectClass;
    public readonly RuntimeIshtarClass* ValueTypeClass = valueTypeClass;
    public readonly RuntimeIshtarClass* VoidClass = voidClass;
    public readonly RuntimeIshtarClass* StringClass = stringClass;
    public readonly RuntimeIshtarClass* ByteClass = byteClass;
    public readonly RuntimeIshtarClass* SByteClass = sByteClass;
    public readonly RuntimeIshtarClass* Int32Class = int32Class;
    public readonly RuntimeIshtarClass* Int16Class = int16Class;
    public readonly RuntimeIshtarClass* Int64Class = int64Class;
    public readonly RuntimeIshtarClass* UInt32Class = uInt32Class;
    public readonly RuntimeIshtarClass* UInt16Class = uInt16Class;
    public readonly RuntimeIshtarClass* UInt64Class = uInt64Class;
    public readonly RuntimeIshtarClass* HalfClass = halfClass;
    public readonly RuntimeIshtarClass* FloatClass = floatClass;
    public readonly RuntimeIshtarClass* DoubleClass = doubleClass;
    public readonly RuntimeIshtarClass* DecimalClass = decimalClass;
    public readonly RuntimeIshtarClass* BoolClass = boolClass;
    public readonly RuntimeIshtarClass* CharClass = charClass;
    public readonly RuntimeIshtarClass* ArrayClass = arrayClass;
    public readonly RuntimeIshtarClass* ExceptionClass = exceptionClass;
    public readonly RuntimeIshtarClass* RawClass = rawClass;
    public readonly RuntimeIshtarClass* AspectClass = aspectClass;
    public readonly RuntimeIshtarClass* FunctionClass = functionClass;
    public readonly RuntimeIshtarClass* RangeClass = rangeClass;

    public readonly NativeList<RuntimeIshtarClass>* All = IshtarGC.AllocateList<RuntimeIshtarClass>(objectClass);

    private readonly NativeDictionary<int, RuntimeIshtarClass>* Mapping =
        IshtarGC.AllocateDictionary<int, RuntimeIshtarClass>(objectClass);


    public RuntimeIshtarClass* ByTypeCode(VeinTypeCode code)
    {
        if (Mapping->TryGetValue((int)code, out var r))
            return r;
        VirtualMachine.Assert(false, WNE.TYPE_LOAD, $"cannot load type for TypeCode '{code}'");
        return null;
    }

    public RuntimeIshtarClass* ByQualityName(RuntimeQualityTypeName* name)
    {
        var result = All->FirstOrNull(x => RuntimeQualityTypeName.Eq(x->FullName, name));
        return result;
    }


    public static IshtarTypes* Create(AppVault vault)
    {
        using var tag = Profiler.Begin("vm:types:create");
        var r = IshtarGC.AllocateImmortal<IshtarTypes>(vault.vm->InternalModule);
        var module = IshtarGC.AllocateImmortal<RuntimeIshtarModule>(vault.vm->InternalModule);


        *module = new RuntimeIshtarModule(vault, "std", module, new IshtarVersion(0, 0))
        {
            IsPredefined = true
        };




        var objectClass = r->create(module->TypeName("Object", "std"), null, module, TYPE_OBJECT);
        var valueTypeClass = r->create(module->TypeName("ValueType", "std"), null, module, TYPE_OBJECT);

        *r = new IshtarTypes(
            objectClass,
            valueTypeClass,
            r->create(module->TypeName("Void", "std"), valueTypeClass, module, TYPE_VOID),
            r->create(module->TypeName("String", "std"), objectClass, module, TYPE_STRING),
            r->create(module->TypeName("Byte", "std"), valueTypeClass, module, TYPE_U1),
            r->create(module->TypeName("SByte", "std"), valueTypeClass, module, TYPE_I1),
            r->create(module->TypeName("Int16", "std"), valueTypeClass, module, TYPE_I2),
            r->create(module->TypeName("Int32", "std"), valueTypeClass, module, TYPE_I4),
            r->create(module->TypeName("Int64", "std"), valueTypeClass, module, TYPE_I8),
            r->create(module->TypeName("UInt16", "std"), valueTypeClass, module, TYPE_U2),
            r->create(module->TypeName("UInt32", "std"), valueTypeClass, module, TYPE_U4),
            r->create(module->TypeName("UInt64", "std"), valueTypeClass,module, TYPE_U8),
            r->create(module->TypeName("Half", "std"), valueTypeClass, module, TYPE_R2),
            r->create(module->TypeName("Float", "std"), valueTypeClass, module, TYPE_R4),
            r->create(module->TypeName("Double", "std"), valueTypeClass, module, TYPE_R8),
            r->create(module->TypeName("Decimal", "std"), valueTypeClass, module, TYPE_R16),
            r->create(module->TypeName("Boolean", "std"), valueTypeClass, module, TYPE_BOOLEAN),
            r->create(module->TypeName("Char", "std"), valueTypeClass, module, TYPE_CHAR),
            r->create(module->TypeName("Array", "std"), objectClass, module, TYPE_ARRAY),
            r->create(module->TypeName("Exception", "std"), objectClass, module, TYPE_CLASS),
            r->create(module->TypeName("Raw", "std"), valueTypeClass, module, TYPE_RAW),
            r->create(module->TypeName("Aspect", "std"), objectClass, module, TYPE_CLASS),
            r->create(module->TypeName("Function", "std"), objectClass, module, TYPE_FUNCTION),
            r->create(module->TypeName("Range", "std"), objectClass, module, TYPE_CLASS)
        );
        r->Add(objectClass, false);
        r->Add(r->VoidClass, false);
        r->Add(r->ByteClass);
        r->Add(r->SByteClass);
        r->Add(r->Int32Class);
        r->Add(r->Int16Class);
        r->Add(r->Int64Class);
        r->Add(r->UInt32Class);
        r->Add(r->UInt16Class);
        r->Add(r->UInt64Class);
        r->Add(r->HalfClass);
        r->Add(r->FloatClass);
        r->Add(r->DoubleClass);
        r->Add(r->DecimalClass);
        r->Add(r->BoolClass);
        r->Add(r->CharClass);
        r->Add(r->ArrayClass);
        r->Add(r->RawClass, false);
        r->Add(r->FunctionClass, false);
        r->Add(r->StringClass);


        r->All->Add(r->ExceptionClass);
        r->All->Add(r->AspectClass);
        r->All->Add(r->ValueTypeClass);
        r->All->Add(r->RangeClass);

        /*if (clazz->TypeCode is not TYPE_OBJECT and not TYPE_CLASS)
            Mapping->Add((int)clazz->TypeCode, clazz);

        Add(clazz);*/
        /*if (Mapping is null) Mapping 
        if (All is null) All = */
        return r;
    }

    public void Add(in RuntimeIshtarClass* clazz, bool defineTransitField = true)
    {
        All->Add(clazz);
        Mapping->Add((int)clazz->TypeCode, clazz);

        //if (defineTransitField)
        //{
        //    var field = clazz->DefineField("_value", FieldFlags.Special, valueTypeClass);
        //    var aspect = IshtarGC.AllocateImmortal<RuntimeAspect>();

        //    var args = IshtarGC.AllocateList<RuntimeAspectArgument>();
        //    var arg = IshtarGC.AllocateImmortal<RuntimeAspectArgument>();

        //    *arg = new RuntimeAspectArgument(aspect, 0, (IshtarObject*)0x14);

        //    args->Add(arg);

        //    *aspect = new RuntimeAspect("native", args, AspectTarget.Field, aspect);

        //    aspect->Union.FieldAspect.FieldName = StringStorage.Intern("!!value");
        //    aspect->Union.FieldAspect.ClassName = StringStorage.Intern(clazz->Name);

        //    field->Aspects->Add(aspect);
        //}
    }

    private RuntimeIshtarClass* create(RuntimeQualityTypeName* name, RuntimeIshtarClass* parent, RuntimeIshtarModule* module, VeinTypeCode typeCode, bool autoAdd = true)
    {
        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>(module);
        *clazz = new RuntimeIshtarClass(name, parent, module, clazz);
        clazz->TypeCode = typeCode;
        clazz->Flags |= ClassFlags.Predefined;
        return clazz;
    }
}
