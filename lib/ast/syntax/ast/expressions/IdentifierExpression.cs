namespace mana.syntax
{
    using System;
    using Sprache;

    public class IdentifierExpression : ExpressionSyntax, IPositionAware<IdentifierExpression>, IEquatable<IdentifierExpression>
    {
        public IdentifierExpression(string name) : base(name)
            => this.ExpressionString = Normalize(name);

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


        // fuk it
        private string Normalize(string x) => x switch
        {
            "int64" => "Int64",
            "int32" => "Int32",
            "int16" => "Int16",
            "byte" => "Byte",
            "string" => "String",
            "bool" => "Boolean",
            "boolean" => "Boolean",
            "uint64" => "UInt64",
            "uint32" => "UInt32",
            "uint16" => "UInt16",
            "sbyte" => "SByte",
            "half" => "Half",
            "float" => "Float",
            "double" => "Double",
            "decimal" => "Decimal",
            "char" => "Char",
            "void" => "Void",
            _ => x
        };
    }
}
