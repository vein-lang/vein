namespace ishtar_test
{
    using ishtar;
    using NUnit.Framework;

    public class NegativeCases : IshtarTestBase
    {
        [Test]
        [Parallelizable(ParallelScope.None)]
        public unsafe void IncorrectPointerCrashTest()
        {
            using var ctx = CreateContext();

            Assert.Throws<WatchDogEffluentException>(() =>
            {
                var invalid = (StrRef*)ulong.MaxValue;
                StringStorage.GetString(invalid, ctx.vm.Frames.EntryPoint);
            });
        }
    }
}
