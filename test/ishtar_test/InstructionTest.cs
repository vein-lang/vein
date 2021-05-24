namespace ishtar_test
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ishtar;
    using mana.backend.ishtar.light;
    using mana.ishtar.emit;
    using mana.runtime;
    using mana.extensions;
    using Xunit;

    public class InstructionTest : IshtarContext
    {
        [Theory]
        [InlineData(OpCodeValue.LDC_I8_5, ManaTypeCode.TYPE_I8)]
        [InlineData(OpCodeValue.LDC_I4_5, ManaTypeCode.TYPE_I4)]
        [InlineData(OpCodeValue.LDC_I2_5, ManaTypeCode.TYPE_I2)]
        public unsafe void ADD_Test(OpCodeValue op, ManaTypeCode code)
        {
            var result = Execute((gen) =>
            {
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
            var result = Execute((gen) =>
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
            var result = Execute((gen) =>
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
            var result = Execute((gen) =>
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
            var result = Execute((gen) =>
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
        
        protected override void StartUp()
        {
            
        }

        protected override void Shutdown()
        {
        }
    }

    public abstract class IshtarContext : IDisposable
    {
        private static ManaModuleBuilder _module;
        private static ClassBuilder _class;
        protected IshtarContext()
        {
            if (VM.allow_shutdown_process)
            {
                VM.allow_shutdown_process = false;
                IshtarCore.INIT();
                foreach (var @class in ManaCore.All.OfType<RuntimeIshtarClass>()) 
                    @class.init_vtable();
                IshtarGC.INIT();
                FFI.INIT();
                _module = new ManaModuleBuilder("tst");
                _class = _module.DefineClass("global::test/testClass");
                // ReSharper disable once VirtualMemberCallInConstructor
                StartUp();
            }
        }

        public unsafe CallFrame Execute(Action<ILGenerator> ctor, short stack_size = 48, [CallerMemberName] string caller = "")
        {
            var guid = Guid.NewGuid().ToString().Where(char.IsLetter).Join();
            var _method = _class.DefineMethod($"master_{caller}_{guid}", MethodFlags.Public | MethodFlags.Static,
                ManaTypeCode.TYPE_VOID.AsClass());
            var gen = _method.GetGenerator();
            ctor(gen);
            var code = gen.BakeByteArray();
            var args_ = stackalloc stackval[1];
            
            var entry_point = new RuntimeIshtarMethod($"master_{caller}_{guid}", MethodFlags.Public | MethodFlags.Static,
                ManaTypeCode.TYPE_VOID.AsClass()) { Owner = _class };
            
            RuntimeModuleReader.ConstructIL(entry_point, code, stack_size);

            var frame = new CallFrame
            {
                args = args_, 
                method = entry_point, 
                level = 0
            };
            VM.exec_method(frame);
            return frame;
        }
        
        void IDisposable.Dispose()
        {
            Shutdown();
        }

        protected void Validate()
        {
            if (VM.VMException is not null)
                Assert.False(true, $"native exception was thrown.\n\t" +
                                   $"[{VM.VMException.code}]\n\t" +
                                   $"'{VM.VMException.msg}'");
        }
        
        protected abstract void StartUp();
        protected abstract void Shutdown();
    }
}