namespace vein.syntax;

using System.Collections.Generic;
using System.Linq;
using Sprache;

public partial class VeinSyntax
{
    protected internal virtual Parser<TryStatementSyntax> TryStatement =>
        TryCatchFinallyStatement
            .Or(TryFinallyStatement)
            .Or(TryCatchStatement);

    protected internal virtual Parser<TryStatementSyntax> TryCatchStatement =>
        from k in KeywordExpression("try").Positioned().Token()
        from b in Block.Token().Positioned()
        from c in CatchClause.Token().Positioned().AtLeastOnce()
        select new TryStatementSyntax(b, c, null)
            .SetStart(k)
            .SetEnd(c.Last().Block.EndPoint)
            .As<TryStatementSyntax>();

    protected internal virtual Parser<TryStatementSyntax> TryFinallyStatement =>
        from k in KeywordExpression("try").Positioned().Token()
        from b in Block.Token().Positioned()
        from f in FinallyClause.Token().Positioned()
        select new TryStatementSyntax(b, null, f)
            .SetStart(k)
            .SetEnd(f.Block.EndPoint)
            .As<TryStatementSyntax>();

    protected internal virtual Parser<TryStatementSyntax> TryCatchFinallyStatement =>
        from k in KeywordExpression("try").Positioned().Token()
        from b in Block.Token().Positioned()
        from c in CatchClause.Token().Positioned().AtLeastOnce()
        from f in FinallyClause.Token().Positioned()
        select new TryStatementSyntax(b, c, f)
            .SetStart(k.Transform.pos)
            .SetEnd(f.Block.EndPoint)
            .As<TryStatementSyntax>();

    // 'catch' exception_specifier? block
    protected internal virtual Parser<CatchClauseSyntax> CatchClause =>
        from k in KeywordExpression("catch").Positioned().Token()
        from es in ExceptionSpecifierStatement.Optional()
        from b in Block.Positioned()
        select new CatchClauseSyntax(es.GetOrDefault(), b)
            .SetStart(b.StartPoint).SetEnd(b.EndPoint)
            .As<CatchClauseSyntax>();

    // 'finally' block
    protected internal virtual Parser<FinallyClauseSyntax> FinallyClause =>
        from k in KeywordExpression("finally").Positioned().Token()
        from b in Block.Positioned()
        select new FinallyClauseSyntax(b)
            .SetStart(b.StartPoint).SetEnd(b.EndPoint)
            .As<FinallyClauseSyntax>();

    // '(' identifier? ':' type ')'
    protected internal virtual Parser<ExceptionSpecifierSyntax> ExceptionSpecifierStatement =>
        IdentifierExpression
            .Positioned()
            .Optional()
            .Then(x => Parse.Char(':').Token().Return(x))
            .Then(x => TypeExpression.Token().Positioned().Merge(x))
            .Contained(OPENING_PARENTHESIS, CLOSING_PARENTHESIS)
            .Select(ExceptionSpecifierSyntax.Create)
            .Positioned();
}
#nullable enable
public class TryStatementSyntax : StatementSyntax, IAdvancedPositionAware<TryStatementSyntax>
{
    public BlockSyntax TryBlock { get; protected set; }
    public IEnumerable<CatchClauseSyntax>? Catches { get; protected set; }
    public FinallyClauseSyntax? Finally { get; protected set; }

    public TryStatementSyntax(BlockSyntax tryBlock, IEnumerable<CatchClauseSyntax>? catches, FinallyClauseSyntax? @finally)
    {
        TryBlock = tryBlock;
        Catches = catches;
        Finally = @finally;
    }

    public new TryStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
#nullable restore


public class CatchClauseSyntax : StatementSyntax, IAdvancedPositionAware<CatchClauseSyntax>
{
    public ExceptionSpecifierSyntax Specifier { get; private set; }
    public BlockSyntax Block { get; private set; }
    public CatchClauseSyntax(ExceptionSpecifierSyntax specifier, BlockSyntax block)
        => (Specifier, Block) = (specifier, block);

    public new CatchClauseSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}

public class FinallyClauseSyntax : StatementSyntax, IAdvancedPositionAware<FinallyClauseSyntax>
{
    public BlockSyntax Block { get; private set; }
    public FinallyClauseSyntax(BlockSyntax block)
        => (Block) = (block);

    public new FinallyClauseSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}



public class ExceptionSpecifierSyntax : ExpressionSyntax, IPositionAware<ExceptionSpecifierSyntax>
{
    public TypeExpression Type { get; protected set; }
    public IOption<IdentifierExpression> Identifier { get; protected set; }
    public ExceptionSpecifierSyntax(TypeExpression type, IOption<IdentifierExpression> id)
        => (Identifier, Type) = (id, type);
    public static ExceptionSpecifierSyntax Create((TypeExpression type, IOption<IdentifierExpression> id) arg)
        => new ExceptionSpecifierSyntax(arg.type, arg.id);

    public new ExceptionSpecifierSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}


public static class VeinSyntaxEx
{
    public static Parser<(T1, T2)> Merge<T1, T2>(this Parser<T1> parser, T2 t2)
        => parser.Select(z => (z, t2));
}
