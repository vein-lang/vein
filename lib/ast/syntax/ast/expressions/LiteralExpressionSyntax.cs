namespace vein.syntax
{
    using Sprache;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Numerics;

    public abstract class LiteralExpressionSyntax(string token = null, LiteralType type = LiteralType.Null)
        : ExpressionSyntax, IPositionAware<LiteralExpressionSyntax>
    {
        public override SyntaxType Kind => SyntaxType.LiteralExpression;

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public string Token { get; set; } = token;

        public LiteralType LiteralType { get; set; } = type;

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

    public sealed class TrueLiteralExpressionSyntax()
        : BoolLiteralExpressionSyntax("true"), IPositionAware<TrueLiteralExpressionSyntax>
    {
        public new TrueLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => this.Token;
    }

    public sealed class FalseLiteralExpressionSyntax()
        : BoolLiteralExpressionSyntax("false"), IPositionAware<FalseLiteralExpressionSyntax>
    {
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

    public class UndefinedIntegerNumericLiteral : NumericLiteralExpressionSyntax
    {
        public string Value { get; set; }

        public UndefinedIntegerNumericLiteral(string val) => this.Value = this.Token = this.ExpressionString = val;
    }
    public abstract class NumericLiteralExpressionSyntax<T> : NumericLiteralExpressionSyntax
        where T : IFormattable, IComparable<T>, IEquatable<T>, IComparable, INumber<T>
    {
        protected NumericLiteralExpressionSyntax(T value)
        {
            if (value is IConvertible convertible)
                this.Token = convertible.ToString(CultureInfo.InvariantCulture);
            else 
                this.Token = value.ToString("", CultureInfo.InvariantCulture);
            this.Value = value;
        }

        public T Value { get; private set; }
    }
    public sealed class DoubleLiteralExpressionSyntax(double value) : NumericLiteralExpressionSyntax<double>(value);
    public sealed class SingleLiteralExpressionSyntax(float value) : NumericLiteralExpressionSyntax<float>(value);
    public sealed class DecimalLiteralExpressionSyntax(decimal value) : NumericLiteralExpressionSyntax<decimal>(value);

    public sealed class HalfLiteralExpressionSyntax(float value) : NumericLiteralExpressionSyntax<float>(value);
    public sealed class ByteLiteralExpressionSyntax(byte value) : NumericLiteralExpressionSyntax<byte>(value);
    public sealed class SByteLiteralExpressionSyntax(sbyte value) : NumericLiteralExpressionSyntax<sbyte>(value);

    public sealed class Int16LiteralExpressionSyntax(short value) : NumericLiteralExpressionSyntax<short>(value);
    public sealed class UInt16LiteralExpressionSyntax(ushort value) : NumericLiteralExpressionSyntax<ushort>(value);
    public sealed class Int32LiteralExpressionSyntax(int value) : NumericLiteralExpressionSyntax<int>(value);
    public sealed class UInt32LiteralExpressionSyntax(uint value) : NumericLiteralExpressionSyntax<uint>(value);
    public sealed class Int64LiteralExpressionSyntax(long value) : NumericLiteralExpressionSyntax<long>(value);
    public sealed class UInt64LiteralExpressionSyntax(ulong value) : NumericLiteralExpressionSyntax<ulong>(value);
    public sealed class Int128LiteralExpressionSyntax(ulong value) : NumericLiteralExpressionSyntax<Int128>(value);
    public sealed class UInt128LiteralExpressionSyntax(ulong value) : NumericLiteralExpressionSyntax<UInt128>(value);

    public sealed class InfinityLiteralExpressionSyntax()
        : NumericLiteralExpressionSyntax<float>(float.PositiveInfinity), IPositionAware<InfinityLiteralExpressionSyntax>
    {
        public new InfinityLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public sealed class NegativeInfinityLiteralExpressionSyntax()
        : NumericLiteralExpressionSyntax<float>(float.NegativeInfinity),
            IPositionAware<NegativeInfinityLiteralExpressionSyntax>
    {
        public new NegativeInfinityLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    public sealed class NaNLiteralExpressionSyntax() : NumericLiteralExpressionSyntax<float>(float.NaN),
        IPositionAware<NaNLiteralExpressionSyntax>
    {
        public new NaNLiteralExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
