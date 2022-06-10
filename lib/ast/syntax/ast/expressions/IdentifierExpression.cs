namespace vein.syntax
{
    using System;
    using Sprache;

    public class IdentifierExpression : ExpressionSyntax, IPositionAware<IdentifierExpression>, IEquatable<IdentifierExpression>
    {
        public IdentifierExpression(string name) : base(name)
            => this.ExpressionString = name;

        public new IdentifierExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public bool Equals(IdentifierExpression other)
        {
            if (other is null)
                return false;
            return this.ExpressionString == other.ExpressionString;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IdentifierExpression)obj);
        }

        public override int GetHashCode()
            => this.ExpressionString.GetHashCode();

        public static bool operator ==(IdentifierExpression left, IdentifierExpression right)
            => Equals(left, right);

        public static bool operator !=(IdentifierExpression left, IdentifierExpression right)
            => !Equals(left, right);

        public override string ToString() =>
            ExpressionString;

        public static implicit operator string(IdentifierExpression i) => i.ToString();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            throw new NotImplementedException();
        }
    }
}
