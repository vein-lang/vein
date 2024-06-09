namespace ishtar
{
    using System.Collections.Generic;
    using runtime;
    using vein.reflection;
    using vein.runtime;
    using static vein.runtime.VeinTypeCode;

    public unsafe partial class VirtualMachine
    {
        private void act<T>(ref T t1, ref T t2, A_OperationDelegate<T> actor) => actor(ref t1, ref t2);

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
                                FastFail(WNE.ACCESS_VIOLATION, $"YOUR JUST OPEN A BLACKHOLE!!! [DivideByZeroError]", frame);
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
