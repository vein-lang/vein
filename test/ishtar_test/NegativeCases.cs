namespace ishtar_test
{
    using System;
    using ishtar;
    using Xunit;
    using Xunit.Sdk;

    public class NegativeCases : IshtarTestBase
    {
        [Fact, TestPriority(9999)]
        public unsafe void IncorrectPointerCrashTest()
        {
            Assert.Throws<WatchDogEffluentException>(() =>
            {
                var invalid = (StrRef*)ulong.MaxValue;
                return StringStorage.GetString(invalid);
            });
        }


        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}