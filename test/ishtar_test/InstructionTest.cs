namespace ishtar_test
{
    using System;
    using ishtar;
    using mana.ishtar.emit;
    using mana.runtime;
    using Xunit;
    using static mana.runtime.FieldFlags;

    public class InstructionTest : IshtarTestBase
    {
        [Theory]
        [InlineData(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [InlineData(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [InlineData(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        public unsafe void ADD_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                if (gen == null)
                    throw new Exception("GENERATOR IS NULL");
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.ADD);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.Equal(code, (*result.returnValue).type);
            Assert.Equal(10, (*result.returnValue).data.l);
        }

        [Theory]
        [InlineData(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [InlineData(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [InlineData(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        public unsafe void SUB_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.SUB);
                gen.Emit(OpCodes.SUB);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.Equal(code, (*result.returnValue).type);
            Assert.Equal(5, (*result.returnValue).data.l);
        }

        [Theory]
        [InlineData(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [InlineData(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [InlineData(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        public unsafe void MUL_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.MUL);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.Equal(code, (*result.returnValue).type);
            Assert.Equal(5 * 5, (*result.returnValue).data.l);
        }

        [Theory]
        [InlineData(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [InlineData(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [InlineData(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        public unsafe void DIV_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.DIV);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.Equal(code, (*result.returnValue).type);
            Assert.Equal(5 / 5, (*result.returnValue).data.l);
        }

        [Theory]
        [InlineData(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [InlineData(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [InlineData(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        public unsafe void DUP_Test(OpCodeValue op, ManaTypeCode code)
        {
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.all[op]);
                gen.Emit(OpCodes.DUP);
                gen.Emit(OpCodes.MUL);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.Equal(code, (*result.returnValue).type);
            Assert.Equal(5 * 5, (*result.returnValue).data.l);
        }

        [Fact]
        public unsafe void LDSTR_Test()
        {
            var targetStr = "the string";
            using var ctx = CreateContext();
            var result = ctx.Execute((gen, _) =>
            {
                gen.Emit(OpCodes.LDC_STR, targetStr);
                gen.Emit(OpCodes.RET);
            });
            Validate();
            Assert.Equal(ManaTypeCode.TYPE_STRING, (*result.returnValue).type);

            var obj = (IshtarObject*) result.returnValue->data.p;
            var @class = obj->DecodeClass();
            var p = (StrRef*)obj->vtable[@class.Field["!!value"].vtable_offset];
            var str = StrRef.Unwrap(p);
            Assert.Equal(targetStr, str);
        }


        [Fact]
        public unsafe void LDF_Test()
        {
            using var ctx = CreateContext();

            ctx.OnClassBuild((@class, o) =>
            {
                o.field = @class.DefineField("TEST_FIELD", Public | Static, ManaTypeCode.TYPE_I4.AsClass());
            });

            var result = ctx.Execute((gen, storage) =>
            {
                gen.Emit(OpCodes.LDC_I4_S, 142);
                gen.Emit(OpCodes.STSF, (RuntimeIshtarField)storage.field);
                gen.Emit(OpCodes.LDSF, (RuntimeIshtarField)storage.field);
                gen.Emit(OpCodes.RET);
            });

            Assert.Equal(142, result.returnValue->data.l);
        }

        
        protected override void StartUp()
        {
            
        }

        protected override void Shutdown()
        {
        }
    }
}