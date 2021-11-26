namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Sprache;

    public abstract class LiteralExpressionSyntax : ExpressionSyntax, IPositionAware<LiteralExpressionSyntax>
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
        public new LiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public sealed class StringLiteralExpressionSyntax : LiteralExpressionSyntax, IPositionAware<StringLiteralExpressionSyntax>
    {
        public StringLiteralExpressionSyntax(string value)
        {
            this.Token = value[1..^1];
            this.LiteralType = LiteralType.String;
        }

        public string Value => Token;


        public override string ToString() => Value;

        public new StringLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public sealed class NullLiteralExpressionSyntax : LiteralExpressionSyntax, IPositionAware<NullLiteralExpressionSyntax>
    {
        public NullLiteralExpressionSyntax()
        {
            this.Token = "null";
            this.LiteralType = LiteralType.Null;
        }

        public new NullLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public sealed class TrueLiteralExpressionSyntax : BoolLiteralExpressionSyntax, IPositionAware<TrueLiteralExpressionSyntax>
    {
        public TrueLiteralExpressionSyntax() : base("true") { }
        public new TrueLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => this.Token;
    }

    public sealed class FalseLiteralExpressionSyntax : BoolLiteralExpressionSyntax, IPositionAware<FalseLiteralExpressionSyntax>
    {
        public FalseLiteralExpressionSyntax() : base("false") { }
        public new FalseLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => this.Token;
    }

    public class BoolLiteralExpressionSyntax : LiteralExpressionSyntax
    {
        public BoolLiteralExpressionSyntax(string value)
        {
            this.LiteralType = LiteralType.Boolean;
            this.Token = value;
            this.Value = value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }
        public bool Value { get; protected set; }
    }
    public abstract class NumericLiteralExpressionSyntax : LiteralExpressionSyntax, IPositionAware<NumericLiteralExpressionSyntax>
    {
        protected NumericLiteralExpressionSyntax() => this.LiteralType = LiteralType.Numeric;

        public new NumericLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

    public class UndefinedIntegerNumericLiteral : NumericLiteralExpressionSyntax, IPassiveParseTransition
    {
        public string Value { get; set; }

        public UndefinedIntegerNumericLiteral(string val) => this.Value = this.Token = this.ExpressionString = val;
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

    public sealed class HalfLiteralExpressionSyntax : NumericLiteralExpressionSyntax<float>
    {
        public HalfLiteralExpressionSyntax(float value) : base(value) { }
    }
    public sealed class ByteLiteralExpressionSyntax : NumericLiteralExpressionSyntax<byte>
    {
        public ByteLiteralExpressionSyntax(byte value) : base(value) { }
    }
    public sealed class SByteLiteralExpressionSyntax : NumericLiteralExpressionSyntax<sbyte>
    {
        public SByteLiteralExpressionSyntax(sbyte value) : base(value) { }
    }

    public sealed class Int16LiteralExpressionSyntax : NumericLiteralExpressionSyntax<short>
    {
        public Int16LiteralExpressionSyntax(short value) : base(value) { }
    }
    public sealed class UInt16LiteralExpressionSyntax : NumericLiteralExpressionSyntax<ushort>
    {
        public UInt16LiteralExpressionSyntax(ushort value) : base(value) { }
    }
    public sealed class Int32LiteralExpressionSyntax : NumericLiteralExpressionSyntax<int>
    {
        public Int32LiteralExpressionSyntax(int value) : base(value) { }
    }
    public sealed class UInt32LiteralExpressionSyntax : NumericLiteralExpressionSyntax<uint>
    {
        public UInt32LiteralExpressionSyntax(uint value) : base(value) { }
    }
    public sealed class Int64LiteralExpressionSyntax : NumericLiteralExpressionSyntax<long>
    {
        public Int64LiteralExpressionSyntax(long value) : base(value) { }
    }
    public sealed class UInt64LiteralExpressionSyntax : NumericLiteralExpressionSyntax<ulong>
    {
        public UInt64LiteralExpressionSyntax(ulong value) : base(value) { }
    }

    public sealed class InfinityLiteralExpressionSyntax : NumericLiteralExpressionSyntax<float>, IPositionAware<InfinityLiteralExpressionSyntax>
    {
        public InfinityLiteralExpressionSyntax() : base(float.PositiveInfinity) { }
        public new InfinityLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public sealed class NegativeInfinityLiteralExpressionSyntax : NumericLiteralExpressionSyntax<float>, IPositionAware<NegativeInfinityLiteralExpressionSyntax>
    {
        public NegativeInfinityLiteralExpressionSyntax() : base(float.NegativeInfinity) { }
        public new NegativeInfinityLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public sealed class NaNLiteralExpressionSyntax : NumericLiteralExpressionSyntax<float>, IPositionAware<NaNLiteralExpressionSyntax>
    {
        public NaNLiteralExpressionSyntax() : base(float.NaN) { }

        public new NaNLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
