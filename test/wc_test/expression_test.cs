namespace wc_test
{
    using System;
    using insomnia.emit;
    using insomnia.extensions;
    using insomnia.syntax;
    using Xunit;
    using Xunit.Abstractions;

    public class expression_test
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public expression_test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact(DisplayName = "(40 + 50)")]
        public void F00()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var result = WaveExpression.Add(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I4, type.TypeCode);
        }
        [Fact(DisplayName = "((40 + 50) - 50)")]
        public void F01()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var f3 = WaveExpression.Add(f1, f2);

            var result = WaveExpression.Sub(f3, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I4, type.TypeCode);
        }
        [Fact(DisplayName = "(((40 - (40 + 50)) / (40 + 50)) - ((40 - (40 + 50)) / (40 + 50)))")]
        public void F02()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var f3 = WaveExpression.Add(f1, f2);
            var f4 = WaveExpression.Sub(f1, f3);
            var f5 = WaveExpression.Div(f4, f3);

            var result = WaveExpression.Sub(f5, f5);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I4, type.TypeCode);
        }

        [Fact(DisplayName = "(((40 - (40 + 50)) / (40 + 50)) && ((40 - (40 + 50)) / (40 + 50)))")]
        public void F03()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);

            var f3 = WaveExpression.Add(f1, f2);
            var f4 = WaveExpression.Sub(f1, f3);
            var f5 = WaveExpression.Div(f4, f3);

            var result = WaveExpression.AndAlso(f5, f5);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void F04()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I8, 40);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);
            
            var result = WaveExpression.AndAlso(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_BOOLEAN, type.TypeCode);
        }

        [Fact]
        public void F05()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_I8, long.MaxValue - 200);
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);
            
            var result = WaveExpression.Sub(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());

            var type = result.DetermineType(null);

            Assert.Equal(WaveTypeCode.TYPE_I8, type.TypeCode);
        }

        [Fact]
        public void F06()
        {
            var f1 = WaveExpression.Const(WaveTypeCode.TYPE_STRING, "Foo");
            var f2 = WaveExpression.Const(WaveTypeCode.TYPE_I4, 50);
            
            var result = WaveExpression.Sub(f1, f2);

            _testOutputHelper.WriteLine(result.ToString());


            Assert.Throws<NotImplementedException>(() => result.DetermineType(null));
        }
    }
}