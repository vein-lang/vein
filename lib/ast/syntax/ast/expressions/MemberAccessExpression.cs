namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using vein.extensions;


    public class MemberAccessExpressionV2 : BinaryExpressionSyntax, IPositionAware<MemberAccessExpressionV2>
    {
        public ExpressionSyntax Expression
        {
            get => base.Left;
            set => base.Left = value;
        }

        public IdentifierExpression Name
        {
            get => (IdentifierExpression)base.Right;
            set => base.Right = value;
        }

        public MemberAccessExpressionV2(ExpressionSyntax exp, IdentifierExpression name)
        {
            Expression = exp;
            Name = name;
        }

        public new MemberAccessExpressionV2 SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
    [Obsolete]
    public class MemberAccessExpression : ExpressionSyntax, IPositionAware<MemberAccessExpression>
    {
        public MemberAccessExpression(ExpressionSyntax start, IEnumerable<ExpressionSyntax> indexerAccess, IEnumerable<ExpressionSyntax> chain)
        {
            Start = start;
            IndexerAccess = indexerAccess;
            Chain = chain;
        }

        public ExpressionSyntax Start { get; }
        public IEnumerable<ExpressionSyntax> IndexerAccess { get; }
        public IEnumerable<ExpressionSyntax> Chain { get; }

        public new MemberAccessExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
        public IEnumerable<ExpressionSyntax> GetChain()
        {
            var prepacked = new[] {Start}.Concat(IndexerAccess).Concat(Chain).ToArray();
            var result = new List<ExpressionSyntax>();
            foreach (var syntax in prepacked)
            {
                if (syntax is ChainAccessExpression { Start: null } chain1)
                    result.AddRange(chain1.Other);
                else if (syntax is ChainAccessExpression { Start: { } } chain2)
                    result.AddRange(new[] { chain2.Start }.Concat(chain2.Other));
                else
                    result.Add(syntax);
            }
            return result;
        }

        public IEnumerable<ExpressionSyntax> GetNormalizedChain()
        {
            var chain = GetChain().ToArray();
            var normalized = new List<ExpressionSyntax>();


            for (int i = 0; i != chain.Length; i++)
            {
                var current = chain[i];

                if (current is IdentifierExpression ie)
                    normalized.Add(ie);
                else if (current is MethodInvocationExpression me)
                {
                    normalized.RemoveAt(normalized.Count - 1);
                    normalized.Add(new MethodActorExpression(chain[i - 1] as IdentifierExpression, me));
                }
                else
                {
                    throw new NotSupportedException($"'{current.ExpressionString}':{current.GetType().Name} is not support.");
                }
            }

            return normalized;
        }
        public override string ExpressionString
            => $"{GetChain().Select(x => x.ExpressionString).Join('.')}";
    }
}
