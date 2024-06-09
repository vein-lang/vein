namespace ishtar_test
{
    using ishtar;
    using vein.runtime;
    using NUnit.Framework;

    [TestFixture]
    public unsafe class FECallTest : IshtarTestBase
    {
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void Call_FE_Console_Println()
        {
            using var scope = CreateScope();
            scope.OnClassBuild((x, storage) => {
                var type = x.Owner.FindType("std%global::vein/lang/Out", true);

                storage.method = type.FindMethod("@_println");
            });

            scope.OnCodeBuild((gen, storage) =>
            {
                gen.Emit(OpCodes.LDC_STR, "foo");
                gen.Emit(OpCodes.CALL, (VeinMethod)storage.method);
                gen.Emit(OpCodes.RET);
            });

            scope.Compile().Execute().Validate();
        }
    }
}
