namespace ishtar_test;

[TestFixture]
public unsafe class NotCompletedValidateCallTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void ValidateNotCompletedClasses()
    {
        using var scope = CreateScope();
        scope.OnClassBuild((x, storage) => {
            var type = x.Owner.FindType("std%vein/lang/Out", true);

            storage.method = type.FindMethod("@_println");
        });

        scope.OnCodeBuild((gen, storage) => {
            gen.Emit(OpCodes.LDC_STR, "foo");
            gen.Emit(OpCodes.CALL, (VeinMethod)storage.method);
            gen.Emit(OpCodes.RET);
        });

        var ctx = scope.Compile();
        var errors = new StringBuilder();

        ctx.VM.Vault.Modules->ForEach(x =>
        {
            x->class_table->ForEach(z =>
            {
                if ((z->Flags & ClassFlags.NotCompleted) != 0)
                {
                    errors.AppendLine($"Class {z->Name} in {x->Name} module marked as not completed type!");
                }
            });
        });

        Assert.AreEqual("", errors.ToString());
    }
}
