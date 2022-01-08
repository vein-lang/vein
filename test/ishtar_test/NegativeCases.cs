namespace ishtar_test
{
    using ishtar;
    using NUnit.Framework;

    public class NegativeCases : IshtarTestBase
    {
        [Test, Ignore("So, fucking CI has randomly crash other test")]
        [Parallelizable(ParallelScope.None)]
        public unsafe void IncorrectPointerCrashTest() =>
            Assert.Throws<WatchDogEffluentException>(() =>
            {
                var invalid = (StrRef*)ulong.MaxValue;
                StringStorage.GetString(invalid, null);
            });
    }
}
