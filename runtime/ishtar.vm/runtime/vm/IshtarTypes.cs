namespace ishtar;

using runtime;
using vein.runtime;
using collections;
using ishtar;
using vein.reflection;
using static vein.runtime.VeinTypeCode;
using runtime.gc;

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

    public readonly NativeList<RuntimeIshtarClass>* All = IshtarGC.AllocateList<RuntimeIshtarClass>();

    private readonly NativeDictionary<int, RuntimeIshtarClass>* Mapping =
        IshtarGC.AllocateDictionary<int, RuntimeIshtarClass>();


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
        var r = IshtarGC.AllocateImmortal<IshtarTypes>();
        var module = IshtarGC.AllocateImmortal<RuntimeIshtarModule>();


        *module = new RuntimeIshtarModule(vault, "unnamed_types", module, new IshtarVersion(0, 0));
        

        var objectClass = r->create($"std%std/Object".L(), null, module, TYPE_OBJECT);
        var valueTypeClass = r->create($"std%std/ValueType".L(), null, module, TYPE_OBJECT);

        *r = new IshtarTypes(
            objectClass,
            valueTypeClass,
            r->create($"std%std/Void".L(), valueTypeClass, module, TYPE_VOID),
            r->create($"std%std/String".L(), objectClass, module, TYPE_STRING),
            r->create($"std%std/Byte".L(), valueTypeClass, module, TYPE_U1),
            r->create($"std%std/SByte".L(), valueTypeClass, module, TYPE_I1),
            r->create($"std%std/Int16".L(), valueTypeClass, module, TYPE_I2),
            r->create($"std%std/Int32".L(), valueTypeClass, module, TYPE_I4),
            r->create($"std%std/Int64".L(), valueTypeClass, module, TYPE_I8),
            r->create($"std%std/UInt16".L(), valueTypeClass, module, TYPE_U2),
            r->create($"std%std/UInt32".L(), valueTypeClass, module, TYPE_U4),
            r->create($"std%std/UInt64".L(), valueTypeClass,module, TYPE_U8),
            r->create($"std%std/Half".L(), valueTypeClass, module, TYPE_R2),
            r->create($"std%std/Float".L(), valueTypeClass, module, TYPE_R4),
            r->create($"std%std/Double".L(), valueTypeClass, module, TYPE_R8),
            r->create($"std%std/Decimal".L(), valueTypeClass, module, TYPE_R16),
            r->create($"std%std/Boolean".L(), valueTypeClass, module, TYPE_BOOLEAN),
            r->create($"std%std/Char".L(), valueTypeClass, module, TYPE_CHAR),
            r->create($"std%std/Array".L(), objectClass, module, TYPE_ARRAY),
            r->create($"std%std/Exception".L(), objectClass, module, TYPE_CLASS),
            r->create($"std%std/Raw".L(), valueTypeClass, module, TYPE_RAW),
            r->create($"std%std/Aspect".L(), objectClass, module, TYPE_CLASS),
            r->create($"std%std/Function".L(), objectClass, module, TYPE_FUNCTION),
            r->create($"std%std/Range".L(), objectClass, module, TYPE_CLASS)
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
        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();
        *clazz = new RuntimeIshtarClass(name, parent, module, clazz);
        clazz->TypeCode = typeCode;
        clazz->Flags |= ClassFlags.Predefined;
        return clazz;
    }
}
