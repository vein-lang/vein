namespace ishtar_test
{
    using ishtar;
    using Xunit;

    public class FECallTest : IshtarTestBase
    {
        [Fact]
        public void Call_FE_Console_Println()
        {
            using var ctx = CreateContext();
            ctx.OnClassBuild((x, storage) =>
            {
                var type = x.Owner.FindType("corlib%global::mana/lang/Out", true);

                storage.method = type.FindMethod("@_println") as RuntimeIshtarMethod;
            });

            ctx.Execute((gen, storage) =>
            {
                gen.Emit(OpCodes.LDC_STR, "foo");
                gen.Emit(OpCodes.CALL, (RuntimeIshtarMethod) storage.method);
                gen.Emit(OpCodes.RET);
            });
        }
    }
}