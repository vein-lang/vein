namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public abstract class LiteralExpressionSyntax : ExpressionSyntax
    {
        protected LiteralExpressionSyntax(string token = null, LiteralType type = LiteralType.Null)
        {
            Token = token;
            LiteralType = type;
        }

        public override SyntaxType Kind => SyntaxType.LiteralExpression;

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public string Token { get; set; }

        public LiteralType LiteralType { get; set; }
        
        public override string ExpressionString => Token;
    }
    
    public sealed class StringLiteralExpressionSyntax : LiteralExpressionSyntax
    {
        public StringLiteralExpressionSyntax(string value)
        {
            this.Token = value[1..^1];
            this.LiteralType = LiteralType.String;
        }

        public string Value => Token;
    }
    
    public sealed class NullLiteralExpressionSyntax : LiteralExpressionSyntax
    {
        public NullLiteralExpressionSyntax()
        {
            this.Token = "null";
            this.LiteralType = LiteralType.Null;
        }
    }
    
    public sealed class BoolLiteralExpressionSyntax : LiteralExpressionSyntax
    {
        public BoolLiteralExpressionSyntax(string value)
        {
            this.LiteralType = LiteralType.Null;
            this.Token = value;
            this.Value = value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }
        public bool Value { get; private set; }
    }
    public abstract class NumericLiteralExpressionSyntax : LiteralExpressionSyntax
    {
        protected NumericLiteralExpressionSyntax() => this.LiteralType = LiteralType.Numeric;
    }
    public abstract class NumericLiteralExpressionSyntax<T> : NumericLiteralExpressionSyntax 
        where T : IFormattable, IConvertible, IComparable<T>, IEquatable<T>, IComparable
    {
        protected NumericLiteralExpressionSyntax(T value)
        {
            this.Token = value.ToString(CultureInfo.InvariantCulture);
            this.Value = value;
        }

        public T Value { get; private set; }
    }
    public sealed class DoubleLiteralExpressionSyntax : NumericLiteralExpressionSyntax<double>
    {
        public DoubleLiteralExpressionSyntax(double value) : base(value) { }
    }
    public sealed class SingleLiteralExpressionSyntax : NumericLiteralExpressionSyntax<float>
    {
        public SingleLiteralExpressionSyntax(float value) : base(value) { }
    }
    public sealed class DecimalLiteralExpressionSyntax : NumericLiteralExpressionSyntax<decimal>
    {
        public DecimalLiteralExpressionSyntax(decimal value) : base(value) { }
    }
    public sealed class Int16LiteralExpressionSyntax : NumericLiteralExpressionSyntax<short>
    {
        public Int16LiteralExpressionSyntax(short value) : base(value) { }
    }
    public sealed class Int32LiteralExpressionSyntax : NumericLiteralExpressionSyntax<int>
    {
        public Int32LiteralExpressionSyntax(int value) : base(value) { }
    }
    public sealed class Int64LiteralExpressionSyntax : NumericLiteralExpressionSyntax<long>
    {
        public Int64LiteralExpressionSyntax(long value) : base(value) { }
    }
}