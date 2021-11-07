namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using vein.reflection;
    using Microsoft.CodeAnalysis;
    using vein.runtime;
    using static OpCodeValue;
    using static vein.runtime.VeinTypeCode;

    public static unsafe partial class VM
    {
        private static void act<T>(ref T t1, ref T t2, A_OperationDelegate<T> actor) => actor(ref t1, ref t2);

        public static QualityTypeName readTypeName(uint index, VeinModule module)
            => module.types_table.GetValueOrDefault((int)index);

        public static RuntimeIshtarClass GetClass(uint index, VeinModule module, CallFrame frame)
        {
            var name = module.types_table.GetValueOrDefault((int)index);
            Assert(name is not null, WNE.TYPE_LOAD, $"Cant find '{index}' in class_table.", frame);
            var type = module.FindType(name, true, false);
            if (type is UnresolvedVeinClass)
            {
                FastFail(WNE.MISSING_TYPE, $"Cant load '{name.NameWithNS}' in '{name.AssemblyName}'", frame);
                ValidateLastError();
                return null;
            }
            Assert(type is RuntimeIshtarClass, WNE.TYPE_LOAD, $"metadata is corrupted.");
            return type as RuntimeIshtarClass;
        }

        public static RuntimeIshtarMethod GetMethod(uint index, QualityTypeName owner, VeinModule module, CallFrame frame)
        {
            var clazz = module.FindType(owner);
            var name = module.GetConstStringByIndex((int) index);

            var method = clazz.FindMethod(name, m => m.Name.Equals(name));

            if (method is null)
            {
                FastFail(WNE.MISSING_METHOD, $"Method '{name}' not found in '{clazz.FullName.NameWithNS}'", frame);
                ValidateLastError();
                return null;
            }
            Assert(method is RuntimeIshtarMethod, WNE.MISSING_METHOD, $"metadata is corrupted.");
            return (RuntimeIshtarMethod)method;
        }
        public static RuntimeIshtarField GetField(uint index, RuntimeIshtarClass owner, VeinModule module, CallFrame frame)
        {
            var name = module.GetFieldNameByIndex((int) index);
            var field = owner.FindField(name.Name);

            if (field is null)
            {
                FastFail(WNE.MISSING_FIELD, $"Field '{name}' not found in '{owner.FullName.NameWithNS}'", frame);
                ValidateLastError();
                return null;
            }
            return field;
        }

        private static void A_OP(stackval* sp, int a_t, uint* ip, CallFrame frame)
        {
            if (sp[-1].type != sp[0].type)
            {
                FastFail(WNE.TYPE_MISMATCH, $"Currently math operation for '{sp[-1].type}' and '{sp[0].type}' not supported.", frame);
                ValidateLastError();
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
                                FastFail(WNE.ACCESS_VIOLATION, $"YOUR JUST OPEN A BLACKHOLE!!! [DivideByZeroError]", frame);
                                ValidateLastError();
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
}
