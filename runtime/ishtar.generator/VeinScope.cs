namespace ishtar;

using System;
using System.Collections.Generic;
using emit;
using vein.runtime;
using vein.syntax;

public class VeinScope
{
    public VeinScope TopScope { get; }
    public List<VeinScope> Scopes { get; } = new();
    public GeneratorContext Context { get; }

    public Dictionary<IdentifierExpression, VeinClass> variables { get; } = new();
    public Dictionary<IdentifierExpression, int> locals_index { get; } = new();


    public VeinScope(GeneratorContext gen, VeinScope owner = null)
    {
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
            throw new CannotExistMainScopeException();
        Context.CurrentScope = this.TopScope;
        return this.TopScope;
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

    public (VeinClass @class, int index) GetVariable(IdentifierExpression id)
    {
        if (variables.TryGetValue(id, out var variable))
            return (variable, locals_index[id]);
        return TopScope.GetVariable(id);
    }

    public VeinScope DefineVariable(IdentifierExpression id, VeinClass type, int localIndex)
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


public class ScopeTransit : IDisposable
{
    public readonly VeinScope Scope;
    public ScopeTransit(VeinScope scope) => Scope = scope;
    public void Dispose() => Scope.ExitScope();
}
