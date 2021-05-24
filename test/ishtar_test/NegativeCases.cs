namespace ishtar_test
{
    using ishtar;
    using Xunit;
    using Xunit.Sdk;

    public class NegativeCases : IshtarContext
    {
        [Fact]
        public unsafe void IncorrectPointerCrashTest()
        {
            var invalid = new StrRef {index = 534534};
            StrRef.Unwrap(&invalid);
            Assert.Throws<FalseException>(Validate);
        }


        protected override void StartUp() { }
        protected override void Shutdown() { }
    }
}