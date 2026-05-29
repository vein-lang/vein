namespace ishtar;

using System.Numerics;
using runtime;
using static OpCodeValue;
using static vein.runtime.VeinTypeCode;

public unsafe partial struct VirtualMachine
{
    public delegate void A_OperationDelegate<T>(ref T t1, ref T t2);

    private void act<T>(ref T t1, ref T t2, A_OperationDelegate<T> actor) => actor(ref t1, ref t2);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private stackval _comparer(in stackval first, in stackval second, OpCodeValue operation, CallFrame* frame)
    {
        if (operation is not EQL_F and not EQL_T && first.type != second.type)
        {
            switch (first.type)
            {
                case TYPE_I4 when second.type is TYPE_BOOLEAN:
                    break;
                case TYPE_BOOLEAN when second.type is TYPE_I4:
                    break;
                default:
                    FastFail(WNE.TYPE_MISMATCH, "", frame);
                    return default;
            }
        }

        var result = (first.type) switch
        {
            TYPE_I4 or TYPE_BOOLEAN or TYPE_CHAR
                => comparer(first.data.i, second.data.i, operation),
            TYPE_I1 => comparer(first.data.b, second.data.b, operation),
            TYPE_U1 => comparer(first.data.ub, second.data.ub, operation),
            TYPE_I2 => comparer(first.data.s, second.data.s, operation),
            TYPE_U2 => comparer(first.data.us, second.data.us, operation),
            TYPE_U4 => comparer(first.data.ui, second.data.ui, operation),
            TYPE_I8 => comparer(first.data.l, second.data.l, operation),
            TYPE_U8 => comparer(first.data.ul, second.data.ul, operation),
            TYPE_R2 => comparer(first.data.hf, second.data.hf, operation),
            TYPE_R4 => comparer(first.data.f_r4, second.data.f_r4, operation),
            TYPE_R8 => comparer(first.data.f, second.data.f, operation),
            TYPE_R16 => comparer(first.data.d, second.data.d, operation),
            TYPE_RAW => comparer(first.data.p, second.data.p, operation),
            TYPE_NULL => comparer(first.data.p, second.data.p, operation),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (result is -1)
        {
            FastFail(WNE.STATE_CORRUPT, "", frame);
            return default;
        }

        return new stackval()
        {
            data = { i = result },
            type = TYPE_BOOLEAN
        };
    }

    private const int _true = 1;
    private const int _false = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string debug_comparer_get_symbol(in stackval first, in stackval second, OpCodeValue operation)
    {
        return (first.type) switch
        {
            TYPE_I4 or TYPE_BOOLEAN or TYPE_CHAR
                => debug_comparer_get_symbol(first.data.i, second.data.i, operation),
            TYPE_I1 => debug_comparer_get_symbol(first.data.b, second.data.b, operation),
            TYPE_U1 => debug_comparer_get_symbol(first.data.ub, second.data.ub, operation),
            TYPE_I2 => debug_comparer_get_symbol(first.data.s, second.data.s, operation),
            TYPE_U2 => debug_comparer_get_symbol(first.data.us, second.data.us, operation),
            TYPE_U4 => debug_comparer_get_symbol(first.data.ui, second.data.ui, operation),
            TYPE_I8 => debug_comparer_get_symbol(first.data.l, second.data.l, operation),
            TYPE_U8 => debug_comparer_get_symbol(first.data.ul, second.data.ul, operation),
            TYPE_R2 => debug_comparer_get_symbol(first.data.hf, second.data.hf, operation),
            TYPE_R4 => debug_comparer_get_symbol(first.data.f_r4, second.data.f_r4, operation),
            TYPE_R8 => debug_comparer_get_symbol(first.data.f, second.data.f, operation),
            TYPE_R16 => debug_comparer_get_symbol(first.data.d, second.data.d, operation),
            TYPE_RAW => debug_comparer_get_symbol(first.data.p, second.data.p, operation),
            TYPE_NULL => debug_comparer_get_symbol(first.data.p, second.data.p, operation),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string debug_comparer_get_symbol<TNumber>(TNumber first, TNumber second, OpCodeValue operation)
        where TNumber : INumber<TNumber>
        => (operation) switch
        {
            EQL_F => $"{first} == {TNumber.Zero}",
            EQL_H => $"{first} > {second}",
            EQL_L => $"{first} < {second}",
            EQL_T => $"{first} == {TNumber.One}",
            EQL_NN => $"{first} != {second}",
            EQL_HQ => $"{first} >= {second}",
            EQL_LQ => $"{first} <= {second}",
            EQL_NQ => $"{first} == {second}",
            _ => "INVALID"
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int comparer<TNumber>(
        TNumber first,
        TNumber second,
        OpCodeValue operation)
        where TNumber : INumber<TNumber> =>
        (operation) switch
        {

            EQL_F when first == TNumber.Zero => _true,
            EQL_F => _false,

            EQL_H when first > second => _true,
            EQL_H => _false,

            EQL_L when first < second => _true,
            EQL_L => _false,

            EQL_T when first == TNumber.One => _true,
            EQL_T => _false,

            EQL_NN when first != second => _true,
            EQL_NN => _false,

            EQL_HQ when first >= second => _true,
            EQL_HQ => _false,

            EQL_LQ when first <= second => _true,
            EQL_LQ => _false,

            EQL_NQ when first == second => _true,
            EQL_NQ => _false,

            _ => -1
        };


    public RuntimeQualityTypeName* readTypeName(uint index, RuntimeIshtarModule* module, CallFrame* frame)
    {
        if (module->types_table->TryGetValue((int)index, out var result))
            return result;
        FastFail(WNE.TYPE_LOAD, $"no found type by {index} idx in {module->Name} module", frame);
        return null;
    }

    public RuntimeIshtarClass* GetClass(uint index, RuntimeIshtarModule* module, CallFrame* frame)
    {
        if (!module->types_table->TryGetValue((int)index, out var name))
            Assert(false, WNE.TYPE_LOAD, $"Cant find '{index}' in class_table.", frame);

        var type = module->FindType(name, true, false);
        if (type->IsUnresolved)
        {
            FastFail(WNE.MISSING_TYPE, $"Cant load '{name->NameWithNS}' in '{name->AssemblyName}'", frame);
            return null;
        }
        return type;
    }

    public RuntimeIshtarMethod* GetMethod(uint index, RuntimeQualityTypeName* owner, RuntimeIshtarModule* module, CallFrame* frame)
    {
        var clazz = module->FindType(owner, true);
        var name = module->GetConstStringByIndex((int) index);

        if (clazz is null)
            throw new Exception($"Class by index {index} not found in {module->Name} module");

        var method = clazz->FindMethod(name, m => m->Name.Equals(name));

        if (method is null)
        {
            FastFail(WNE.MISSING_METHOD, $"Method '{name}' not found in '{clazz->FullName->NameWithNS}'", frame);
            return null;
        }
        return method;
    }
    public RuntimeIshtarField* GetField(uint index, RuntimeIshtarClass* owner, RuntimeIshtarModule* module, CallFrame* frame)
    {
        var name = module->GetFieldNameByIndex((int) index);
        var field = owner->FindField(name->Name);

        if (field is null)
        {
            FastFail(WNE.MISSING_FIELD, $"Field '{name->Name}' not found in '{owner->FullName->NameWithNS}'", frame);
            return null;
        }
        return field;
    }

    private void A_OP(stackval* sp, int a_t, uint* ip, CallFrame* frame)
    {
        if (sp[-1].type != sp[0].type)
        {
            FastFail(WNE.TYPE_MISMATCH, $"Currently math operation for '{sp[-1].type}' and '{sp[0].type}' not supported.", frame);
            return;
        }

        if (sp->type == TYPE_I4) /*first check int32, most frequent type*/
            act(ref sp[-1].data.i, ref sp[0].data.i, (ref int i1, ref int i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        if (i2 == 0)
                        {
                            // TODO
                            frame->vm->FastFail(WNE.ACCESS_VIOLATION, $"YOUR JUST OPEN A BLACKHOLE!!! [DivideByZeroError]", frame);
                        }
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_I8)
            act(ref sp[-1].data.l, ref sp[0].data.l, (ref long i1, ref long i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_I2)
            act(ref sp[-1].data.s, ref sp[0].data.s, (ref short i1, ref short i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_I1)
            act(ref sp[-1].data.b, ref sp[0].data.b, (ref sbyte i1, ref sbyte i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_U4)
            act(ref sp[-1].data.ui, ref sp[0].data.ui, (ref uint i1, ref uint i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_U1)
            act(ref sp[-1].data.ub, ref sp[0].data.ub, (ref byte i1, ref byte i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_U2)
            act(ref sp[-1].data.us, ref sp[0].data.us, (ref ushort i1, ref ushort i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_U8)
            act(ref sp[-1].data.ul, ref sp[0].data.ul, (ref ulong i1, ref ulong i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_R8)
            act(ref sp[-1].data.f, ref sp[0].data.f, (ref double i1, ref double i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_R4)
            act(ref sp[-1].data.f_r4, ref sp[0].data.f_r4, (ref float i1, ref float i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp->type == TYPE_R16)
            act(ref sp[-1].data.d, ref sp[0].data.d, (ref decimal i1, ref decimal i2) =>
            {
                switch (a_t)
                {
                    case 0:
                        i1 += i2;
                        break;
                    case 1:
                        i1 -= i2;
                        break;
                    case 2:
                        i1 *= i2;
                        break;
                    case 3:
                        i1 /= i2;
                        break;
                    case 4:
                        i1 %= i2;
                        break;
                }
            });
        else if (sp[-1].type == TYPE_NONE)
            FastFail(WNE.TYPE_MISMATCH, $"@{(OpCodeValue)(*(ip - 1))} 'sp[-1]' incorrect stack type: {sp[-1].type}",
                frame);
    }
}
