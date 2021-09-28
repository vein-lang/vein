namespace ishtar_test
{
    using ishtar;
    using NUnit.Framework;

    public unsafe class MarshalTest : IshtarTestBase
    {
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void Int16Test()
        {
            short clr = short.MaxValue / 2;

            var v = IshtarMarshal.ToIshtarObject(clr);
            var r = IshtarMarshal.ToDotnetInt16(v, null);

            Assert.AreEqual(clr, r);
        }
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void Int32Test()
        {
            int clr = int.MaxValue / 2;

            var v = IshtarMarshal.ToIshtarObject(clr);
            var r = IshtarMarshal.ToDotnetInt32(v, null);

            Assert.AreEqual(clr, r);
        }
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void Int64Test()
        {
            long clr = long.MaxValue / 2;

            var v = IshtarMarshal.ToIshtarObject(clr);
            var r = IshtarMarshal.ToDotnetInt64(v, null);

            Assert.AreEqual(clr, r);
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public void StringTest()
        {
            var clr = "long.MaxValue / 2";

            var v = IshtarMarshal.ToIshtarObject(clr);
            var r = IshtarMarshal.ToDotnetString(v, null);

            Assert.AreEqual(clr, r);
        }
    }
}
