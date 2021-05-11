namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using extensions;
    using Sprache;
    using stl;

    public class FieldDeclarationSyntax : MemberDeclarationSyntax
    {
        public FieldDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.Field;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(Type)).Concat(new List<BaseSyntax> { Field }).Where(n => n != null);

        public override MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName)
        {
            Type = typeAndName.Type;

            var identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier;
            Field.Identifier = identifier;

            return this;
        }
        
        public override MemberDeclarationSyntax WithName(IdentifierExpression name)
        {
            Field.Identifier = name;
            return this;
        }

        public TypeSyntax Type { get; set; }

        public FieldDeclaratorSyntax Field { get; set; }

        public ClassDeclarationSyntax OwnerClass { get; set; }
    }
    
    public class ClassInitializerSyntax : MemberDeclarationSyntax
    {
        public ClassInitializerSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.ClassInitializer;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(Body));

        public BlockSyntax Body { get; set; }

        public bool IsStatic => Modifiers.EmptyIfNull().Any(m => m.ModificatorKind == ModificatorKind.Static);
    }
    public class InterfaceDeclarationSyntax : ClassDeclarationSyntax
    {
        public InterfaceDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public InterfaceDeclarationSyntax(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody)
            : base(heading, classBody)
        {
        }

        public override SyntaxType Kind => SyntaxType.Interface;

        public override bool IsInterface => true;
    }
    public class PropertyDeclarationSyntax : MemberDeclarationSyntax
    {
        public PropertyDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public PropertyDeclarationSyntax(IEnumerable<AccessorDeclarationSyntax> accessors, MemberDeclarationSyntax heading = null)
            : this(heading)
        {
            Accessors = accessors.ToList();
        }

        public override SyntaxType Kind => SyntaxType.Property;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(Type, Getter, Setter));

        public TypeSyntax Type { get; set; }

        public IdentifierExpression Identifier { get; set; }

        public List<AccessorDeclarationSyntax> Accessors { get; set; } = new();

        public AccessorDeclarationSyntax Getter => Accessors.FirstOrDefault(a => a.IsGetter);

        public AccessorDeclarationSyntax Setter => Accessors.FirstOrDefault(a => a.IsSetter);

        public override MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName)
        {
            Type = typeAndName.Type;
            Identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier;
            return this;
        }
    }
    public class AccessorDeclarationSyntax : MemberDeclarationSyntax
    {
        public AccessorDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.Accessor;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Body);

        public bool IsGetter { get; set; }

        public bool IsSetter => !IsGetter;

        public BlockSyntax Body { get; set; }

        public bool IsEmpty => Body == null;
    }
    
    public class FieldDeclaratorSyntax : BaseSyntax
    {
        public override SyntaxType Kind => SyntaxType.FieldDeclarator;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public IdentifierExpression Identifier { get; set; }

        public ExpressionSyntax Expression
        {
            get => _expression;
            set => _expression = value is UnaryExpressionSyntax
            {
                OperatorType: ExpressionType.Negate, 
                Operand: UndefinedIntegerNumericLiteral numeric
            } ? RedefineIntegerExpression(numeric, true) : 
                TransformationWalkerExpression(value);
        }


        private ExpressionSyntax _expression;

        private ExpressionSyntax TransformationWalkerExpression(ExpressionSyntax exp)
        {
            if (exp is UndefinedIntegerNumericLiteral numeric)
                return RedefineIntegerExpression(numeric, false);
            return exp; // TODO
        }

        internal static ExpressionSyntax RedefineIntegerExpression(NumericLiteralExpressionSyntax integer, bool isNegate)
        {
            var token = integer.Token;
            var (pos, len) = integer.Transform;

            if (integer.Transform is null)
                throw new ArgumentNullException($"{nameof(integer.Transform)} in '{integer}' has null.");

            if (isNegate)
                token = $"-{token}";
            if (!isNegate && token.StartsWith("-"))
                isNegate = true;

            if (isNegate && !long.TryParse(token, out _))
                throw new ParseException("not valid integer.");
            if (!isNegate && !ulong.TryParse(token, out _))
                throw new ParseException("not valid integer.");

            

            if (!isNegate)
            {
                if (byte.TryParse(token, out _))
                    return new ByteLiteralExpressionSyntax(byte.Parse(token)).SetPos(pos, len);
                if (short.TryParse(token, out _))
                    return new Int16LiteralExpressionSyntax(short.Parse(token)).SetPos(pos, len);
                if (ushort.TryParse(token, out _))
                    return new UInt16LiteralExpressionSyntax(ushort.Parse(token)).SetPos(pos, len);
                if (int.TryParse(token, out _))
                    return new Int32LiteralExpressionSyntax(int.Parse(token)).SetPos(pos, len);
                if (uint.TryParse(token, out _))
                    return new UInt32LiteralExpressionSyntax(uint.Parse(token)).SetPos(pos, len);
                if (long.TryParse(token, out _))
                    return new Int64LiteralExpressionSyntax(long.Parse(token)).SetPos(pos, len);
                if (ulong.TryParse(token, out _))
                    return new UInt64LiteralExpressionSyntax(ulong.Parse(token)).SetPos(pos, len);
            }
            else
            {
                if (sbyte.TryParse(token, out _))
                    return new SByteLiteralExpressionSyntax(sbyte.Parse(token)).SetPos(pos, len);
                if (short.TryParse(token, out _))
                    return new Int16LiteralExpressionSyntax(short.Parse(token)).SetPos(pos, len);
                if (int.TryParse(token, out _))
                    return new Int32LiteralExpressionSyntax(int.Parse(token)).SetPos(pos, len);
                if (long.TryParse(token, out _))
                    return new Int64LiteralExpressionSyntax(long.Parse(token)).SetPos(pos, len);
            }
           
            throw new ParseException($"too big number '{token}'"); // TODO custom exception
        }
    }
}