namespace ishtar;

using runtime;
using vein.runtime;
using collections;
using static vein.runtime.VeinTypeCode;

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
    RuntimeIshtarClass* functionClass)
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

    public readonly DirectNativeList<RuntimeIshtarClass>* All = DirectNativeList<RuntimeIshtarClass>.New(32);

    private readonly DirectNativeDictionary<int, RuntimeIshtarClass>* Mapping = DirectNativeDictionary<int, RuntimeIshtarClass>.New();


    public RuntimeIshtarClass* ByTypeCode(VeinTypeCode code)
    {
        if (Mapping->TryGetValue((int)code, out var r))
            return r;
        VirtualMachine.Assert(false, WNE.TYPE_LOAD, $"cannot load type for TypeCode '{code}'");
        return null;
    }

    public RuntimeIshtarClass* ByQualityName(RuntimeQualityTypeName* name)
        => All->FirstOrNull(x => x->FullName->Equal(name));


    public static IshtarTypes* Create(AppVault vault)
    {
        var r = IshtarGC.AllocateImmortal<IshtarTypes>();
        var module = IshtarGC.AllocateImmortal<RuntimeIshtarModule>();


        *module = new RuntimeIshtarModule(vault, "unnamed_types", module);
        

        var objectClass = r->create($"std%global::vein/lang/Object".L(), null, module, TYPE_OBJECT);
        var valueTypeClass = r->create($"std%global::vein/lang/ValueType".L(), null, module, TYPE_OBJECT);

        *r = new IshtarTypes(
            objectClass,
            valueTypeClass,
            r->create($"std%global::vein/lang/Void".L(), valueTypeClass, module, TYPE_VOID),
            r->create($"std%global::vein/lang/String".L(), objectClass, module, TYPE_STRING),
            r->create($"std%global::vein/lang/Byte".L(), valueTypeClass, module, TYPE_U1),
            r->create($"std%global::vein/lang/SByte".L(), valueTypeClass, module, TYPE_I1),
            r->create($"std%global::vein/lang/Int16".L(), valueTypeClass, module, TYPE_I2),
            r->create($"std%global::vein/lang/Int32".L(), valueTypeClass, module, TYPE_I4),
            r->create($"std%global::vein/lang/Int64".L(), valueTypeClass, module, TYPE_I8),
            r->create($"std%global::vein/lang/UInt16".L(), valueTypeClass, module, TYPE_U2),
            r->create($"std%global::vein/lang/UInt32".L(), valueTypeClass, module, TYPE_U4),
            r->create($"std%global::vein/lang/UInt64".L(), valueTypeClass,module, TYPE_U8),
            r->create($"std%global::vein/lang/Half".L(), valueTypeClass, module, TYPE_R2),
            r->create($"std%global::vein/lang/Float".L(), valueTypeClass, module, TYPE_R4),
            r->create($"std%global::vein/lang/Double".L(), valueTypeClass, module, TYPE_R8),
            r->create($"std%global::vein/lang/Decimal".L(), valueTypeClass, module, TYPE_R16),
            r->create($"std%global::vein/lang/Boolean".L(), valueTypeClass, module, TYPE_BOOLEAN),
            r->create($"std%global::vein/lang/Char".L(), valueTypeClass, module, TYPE_CHAR),
            r->create($"std%global::vein/lang/Array".L(), objectClass, module, TYPE_ARRAY),
            r->create($"std%global::vein/lang/Exception".L(), objectClass, module, TYPE_CLASS),
            r->create($"std%global::vein/lang/Raw".L(), valueTypeClass, module, TYPE_RAW),
            r->create($"std%global::vein/lang/Aspect".L(), objectClass, module, TYPE_CLASS),
            r->create($"std%global::vein/lang/Function".L(), objectClass, module, TYPE_FUNCTION)
        );
        r->Add(objectClass);
        r->Add(r->VoidClass);
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
        r->Add(r->RawClass);
        r->Add(r->FunctionClass);

        /*if (clazz->TypeCode is not TYPE_OBJECT and not TYPE_CLASS)
            Mapping->Add((int)clazz->TypeCode, clazz);

        Add(clazz);*/
        /*if (Mapping is null) Mapping 
        if (All is null) All = */
        return r;
    }

    public void Add(in RuntimeIshtarClass* clazz)
    {
        All->Add(clazz);
        Mapping->Add((int)clazz->TypeCode, clazz);
    }

    private RuntimeIshtarClass* create(RuntimeQualityTypeName* name, RuntimeIshtarClass* parent, RuntimeIshtarModule* module, VeinTypeCode typeCode, bool autoAdd = true)
    {
        var clazz = IshtarGC.AllocateImmortal<RuntimeIshtarClass>();
        *clazz = new RuntimeIshtarClass(name, parent, module, clazz);
        clazz->TypeCode = typeCode;
        return clazz;
    }
}
