namespace ishtar;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Text;
using emit;
using Spectre.Console;
using Sprache;
using vein.extensions;
using vein.reflection;
using vein.runtime;
using vein.syntax;
using Xunit;
using static vein.runtime.MethodFlags;
using static vein.runtime.VeinTypeCode;

public record GeneratorContextConfig(bool DisableOptimization)
{
}

public class GeneratorContext(GeneratorContextConfig config)
{
    public bool DisableOptimization => config.DisableOptimization;

    internal VeinModuleBuilder Module { get; set; }
    internal Dictionary<QualityTypeName, ClassBuilder> Classes { get; } = new();
    internal DocumentDeclaration Document { get; set; }

    public List<string> Errors = new ();

    public Dictionary<VeinMethod, VeinScope> Scopes { get; } = new();

    public VeinMethod CurrentMethod { get; set; }
    public VeinScope CurrentScope { get; set; }

    public GeneratorContext LogError(string err, ExpressionSyntax exp)
    {
        var pos = exp.Transform?.pos ?? new Position(0, 0, 0);
        var diff_err = exp.Transform.DiffErrorFull(Document);
        Errors.Add($"[red bold]{err.EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                   $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                   $"in '[orange bold]{Document.FileEntity}[/]'." +
                   $"{diff_err}");
        return this;
    }

    public IDisposable CreateScope()
    {
        if (CurrentScope is not null)
            return CurrentScope.EnterScope();
        CurrentScope = new VeinScope(this, null);
        Scopes.Add(CurrentMethod, CurrentScope);
        return new ScopeTransit(CurrentScope);
    }

    public VeinClass ResolveType(TypeExpression targetTypeExpression)
        => ResolveType(targetTypeExpression.Typeword);
    public VeinClass ResolveType(TypeSyntax targetTypeTypeword)
        => Module.FindType(targetTypeTypeword.Identifier.ExpressionString,
            Classes[CurrentMethod.Owner.FullName].Includes);
    public VeinClass ResolveType(IdentifierExpression targetTypeTypeword)
        => Module.FindType(targetTypeTypeword.ExpressionString,
            Classes[CurrentMethod.Owner.FullName].Includes);

    public bool HasDefinedType(IdentifierExpression id)
    {
        try
        {
            Module.FindType(id.ExpressionString,
                Classes[CurrentMethod.Owner.FullName].Includes);
            return true;
        }
        catch (TypeNotFoundException)
        {
            return false;
        }
    }
    public ClassBuilder CreateHiddenType(string name)
    {
        QualityTypeName fullName = $"{Module.Name}%internal/{name}";

        var currentType = Module.FindType(fullName, false, false);

        if (currentType is not UnresolvedVeinClass)
            return Assert.IsType<ClassBuilder>(currentType);

        var b = Module.DefineClass(fullName);

        b.Flags |= ClassFlags.Internal | ClassFlags.Special;

        Classes.Add(b.FullName, b);

        return b;
    }

    public ClassBuilder CreateHiddenType(string name, VeinClass parent)
    {
        QualityTypeName fullName = $"{Module.Name}%internal/{name}";

        var currentType = Module.FindType(fullName, false, false);

        if (currentType is not UnresolvedVeinClass)
            return Assert.IsType<ClassBuilder>(currentType);

        var b = Module.DefineClass(fullName, parent);

        b.Flags |= ClassFlags.Internal | ClassFlags.Special;

        Classes.Add(b.FullName, b);

        return b;
    }

    public (VeinArgumentRef, int index)? ResolveArgument(IdentifierExpression id)
    {
        foreach (var (argument, index) in CurrentMethod.Signature.Arguments.Select((x, i) => (x, i)))
        {
            if (argument.Name.Equals(id.ExpressionString))
                return (argument, index);
        }
        return null;
    }
    public (VeinArgumentRef, int index) GetCurrentArgument(IdentifierExpression id)
    {
        foreach (var (argument, index) in CurrentMethod.Signature.Arguments.Select((x, i) => (x, i)))
        {
            if (argument.Name.Equals(id.ExpressionString))
                return (argument, index);
        }

        this.LogError($"Argument '{id}' is not found in '{this.CurrentMethod.Name}' function.", id);
        throw new SkipStatementException();
    }
    public VeinComplexType ResolveScopedIdentifierType(IdentifierExpression id)
    {
        if (ResolveArgument(id) is not null)
        {
            var (arg, index) = ResolveArgument(id)!.Value;

            if (arg.IsGeneric)
                return arg.TypeArg;
            return arg.Type;
        }
        if (CurrentScope.HasVariable(id))
            return CurrentScope.GetVariable(id).@class;
        if (CurrentMethod.Owner.ContainsField(id))
            return CurrentMethod.Owner.ResolveField(id)?.FieldType;
        if (CurrentMethod.Owner.FindProperty(id) is not null)
            return CurrentMethod.Owner.FindProperty(id).PropType;
        var modType = Module.FindType(id.ExpressionString, Classes[CurrentMethod.Owner.FullName].Includes, false);
        if (modType is not null)
            return modType;
        var methods = CurrentMethod.Owner.FindAnyMethods(id.ExpressionString);
        if (methods.Count > 1)
        {
            this.LogError($"In the current context detected multiple methods with name '{id}'", id);
            throw new SkipStatementException();
        }
        if (methods.Count == 1)
            return CreateFunctionMulticastGroup(methods.Single().Signature);

        this.LogError($"The name '{id}' does not exist in the current context.", id);
        throw new SkipStatementException();
    }

    public VeinClass CreateFunctionMulticastGroup(VeinMethodSignature sig)
    {
        var @base = CurrentScope.Context.Module
            .FindType(new QualityTypeName("std", "FunctionMulticast", "std"),
                true, true);

        var hiddenClass = CurrentScope.Context
            .CreateHiddenType($"fn_{string.Join(' ', Encoding.UTF8.GetBytes(sig.ToTemplateString()).Select(x => $"{x:X}"))}", @base);

        if (hiddenClass.TypeCode is TYPE_FUNCTION)
            return hiddenClass;


        var types = CurrentScope.Context.Module.Types;

        hiddenClass.TypeCode = TYPE_FUNCTION;

        var objType = TYPE_OBJECT.AsClass(types);
        var rawType = TYPE_RAW.AsClass(types);

        var ctorMethod = hiddenClass.DefineMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR, hiddenClass, [
            new (VeinArgumentRef.THIS_ARGUMENT, hiddenClass),
            new("fn", rawType),
            new("scope",objType)
        ]);

        var scope = hiddenClass.DefineField("_scope", FieldFlags.Internal, objType);
        var ptrRef = hiddenClass.DefineField("_fn", FieldFlags.Internal, rawType);

        var ctorGen = ctorMethod.GetGenerator();

        ctorGen.Emit(OpCodes.LDARG_1); // load ref
        ctorGen.Emit(OpCodes.LDARG_0); // load this
        ctorGen.Emit(OpCodes.STF, ptrRef); // this.ptrRef = ref;
        ctorGen.Emit(OpCodes.LDARG_2); // load scope
        ctorGen.Emit(OpCodes.LDARG_0); // load this
        ctorGen.Emit(OpCodes.STF, scope); // this.scope = scope;
        ctorGen.Emit(OpCodes.LDARG_0); // load this
        ctorGen.Emit(OpCodes.RET); // return this


        var method = hiddenClass.DefineMethod("invoke", Internal | Special,
            sig.ReturnType,sig.Arguments.Where(VeinMethodSignature.NotThis).ToArray());

        var hasNotThis = sig.Arguments.All(VeinMethodSignature.NotThis);

        var generator = method.GetGenerator();
        
        if (!hasNotThis)
        {
            generator.EmitThis();
            generator.Emit(OpCodes.LDF, scope);
        }
        foreach (int i in ..method.Signature.ArgLength)
            generator.Emit(OpCodes.LDARG_S, i + 1); // TODO optimization for LDARG_X

        generator.EmitThis();
        generator.Emit(OpCodes.LDF, ptrRef);
        generator.Emit(OpCodes.CALL_SP);
        generator.Emit(OpCodes.RET);

        return hiddenClass;
    }


    public VeinField ResolveField(VeinClass targetType, IdentifierExpression target, IdentifierExpression id)
    {
        var field = targetType.FindField(id.ExpressionString);

        if (field is not null)
            return field;
        this.LogError($"'{targetType.FullName.NameWithNS}' does not contain " +
                      $"a definition for '{target.ExpressionString}' and " +
                      $"no extension method '{id.ExpressionString}' accepting " +
                      $"a first argument of type '{targetType.FullName.NameWithNS}' could be found.", id);
        throw new SkipStatementException();
    }

    public VeinProperty ResolveProperty(VeinClass targetType, IdentifierExpression target, IdentifierExpression id)
    {
        var field = targetType.FindProperty(id.ExpressionString);

        if (field is not null)
            return field;
        this.LogError($"'{targetType.FullName.NameWithNS}' does not contain " +
                      $"a definition for '{target.ExpressionString}' and " +
                      $"no extension method '{id.ExpressionString}' accepting " +
                      $"a first argument of type '{targetType.FullName.NameWithNS}' could be found.", id);
        throw new SkipStatementException();
    }

    public VeinField ResolveField(IdentifierExpression id)
        => CurrentMethod.Owner.FindField(id.ExpressionString);

    public VeinProperty ResolveProperty(IdentifierExpression id)
        => CurrentMethod.Owner.FindProperty(id.ExpressionString);

    public VeinField ResolveField(VeinClass targetType, IdentifierExpression id)
    {
        var field = targetType.FindField(id.ExpressionString);

        if (field is not null)
            return field;
        this.LogError($"The name '{id}' does not exist in the current context.", id);
        throw new SkipStatementException();
    }

    public VeinProperty ResolveProperty(VeinClass targetType, IdentifierExpression id)
    {
        var field = targetType.FindProperty(id.ExpressionString);

        if (field is not null)
            return field;
        this.LogError($"The name '{id}' does not exist in the current context.", id);
        throw new SkipStatementException();
    }

    public VeinMethod ResolveMethod(
        VeinClass targetType,
        IdentifierExpression target,
        IdentifierExpression id,
        InvocationExpression invocation)
    {
        var method = targetType.FindMethod(id.ExpressionString, invocation.Arguments.DetermineTypes(this));
        if (method is not null)
            return method;
        if (target is not null)
            this.LogError($"'{targetType.FullName.NameWithNS}' does not contain " +
                          $"a definition for '{target.ExpressionString}' and " +
                          $"no extension method '{id.ExpressionString}' accepting " +
                          $"a first argument of type '{targetType.FullName.NameWithNS}' could be found.", id);
        this.LogError($"The name '{id}' does not exist in the current context.", id);
        throw new SkipStatementException();
    }

    public VeinMethod ResolveMethod(
        VeinClass targetType,
        InvocationExpression invocation)
    {
        var args = invocation.Arguments.DetermineTypes(this);
        var method = targetType.FindMethod($"{invocation.Name}", args);
        if (method is not null)
            return method;
        this.LogError($"The name '{invocation.Name}' does not exist in the current context.", invocation.Name);
        throw new SkipStatementException();
    }
}
