namespace ishtar;

using System;
using System.Collections.Generic;
using System.Threading;
using emit;
using vein.runtime;
using vein.syntax;

public class VeinScope
{
    public VeinScope TopScope { get; }
    public List<VeinScope> Scopes { get; } = new();
    public GeneratorContext Context { get; }
    public ulong ScopeId { get; }

    public Dictionary<IdentifierExpression, VeinComplexType> variables { get; } = new();
    public Dictionary<IdentifierExpression, int> locals_index { get; } = new();

    private static ulong _id;

    public VeinScope(GeneratorContext gen, VeinScope owner = null)
    {
        ScopeId = Interlocked.Increment(ref _id);
        this.Context = gen;
        if (owner is null)
            return;
        this.TopScope = owner;
        owner.Scopes.Add(this);
    }

    public void EnsureExceptionLocal(ILGenerator gen, IdentifierExpression id, int catchId, VeinClass clazz)
    {
        var locIndex = gen.EnsureLocal($"catch${catchId}${id}$", clazz);
        DefineVariable(id, clazz, locIndex);
        gen.Emit(OpCodes.STLOC_S, locIndex);
    }

    public VeinScope ExitScope()
    {
        if (this.TopScope is null)
        {
            return null;
            throw new CannotExistMainScopeException();
        }
        Context.CurrentScope = this.TopScope;
        return this.TopScope;
    }

    public VeinScope ExitScope(bool allowExitFromRoot)
    {
        try
        {
            return ExitScope();
        }
        catch (CannotExistMainScopeException e)
        {
            if (!allowExitFromRoot)
                throw;
        }

        return null;
    }

    public IDisposable EnterScope()
    {
        var result = new VeinScope(Context, this);
        Context.CurrentScope = result;
        return new ScopeTransit(result);
    }

    public bool HasVariable(IdentifierExpression id)
    {
        if (variables.ContainsKey(id))
            return true;
        if (TopScope is null)
            return false;
        return TopScope.HasVariable(id);
    }

    public (VeinComplexType @class, int index) GetVariable(IdentifierExpression id)
    {
        if (variables.TryGetValue(id, out var variable))
            return (variable, locals_index[id]);
        return TopScope.GetVariable(id);
    }

    public VeinScope DefineVariable(IdentifierExpression id, VeinComplexType type, int localIndex)
    {
        if (HasVariable(id))
        {
            Context.LogError($"A local variable named '{id}' is already defined in this scope", id);
            throw new SkipStatementException(true);
        }
        variables.Add(id, type);
        locals_index.Add(id, localIndex);
        return this;
    }
}


public class ScopeTransit(VeinScope scope) : IDisposable
{
    public readonly VeinScope Scope = scope;
    public void Dispose() => Scope.ExitScope(true);
}
