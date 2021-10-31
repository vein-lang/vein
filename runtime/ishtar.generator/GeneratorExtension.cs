namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using System.Threading;
    using vein.extensions;
    using emit;
    using vein.reflection;
    using vein.runtime;
    using Spectre.Console;
    using vein.syntax;
    using Xunit;
    using InvocationExpression = vein.syntax.InvocationExpression;

    public class GeneratorContext
    {
        internal VeinModuleBuilder Module { get; set; }
        internal Dictionary<QualityTypeName, ClassBuilder> Classes { get; } = new();
        internal DocumentDeclaration Document { get; set; }

        public List<string> Errors = new ();

        public Dictionary<VeinMethod, ManaScope> Scopes { get; } = new();

        public VeinMethod CurrentMethod { get; set; }
        public ManaScope CurrentScope { get; set; }

        public GeneratorContext LogError(string err, ExpressionSyntax exp)
        {
            var pos = exp.Transform.pos;
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
            CurrentScope = new ManaScope(this);
            Scopes.Add(CurrentMethod, CurrentScope);
            return new ScopeTransit(CurrentScope);
        }

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

        public ClassBuilder CreateHiddenType(TypeSyntax targetTypeTypeword)
            => CreateHiddenType(targetTypeTypeword.Identifier);
        public ClassBuilder CreateHiddenType(IdentifierExpression targetTypeTypeword)
            => CreateHiddenType(targetTypeTypeword.ExpressionString);
        public ClassBuilder CreateHiddenType(string name)
        {
            QualityTypeName fullName = $"{Module.Name}%global::internal/{name}";

            var currentType = Module.FindType(fullName, false, false);

            if (currentType is not UnresolvedVeinClass)
                return Assert.IsType<ClassBuilder>(currentType);

            var b = Module.DefineClass(fullName);

            b.Flags |= ClassFlags.Internal;

            Classes.Add(b.FullName, b);

            return b;
        }

        public (VeinArgumentRef, int index)? ResolveArgument(IdentifierExpression id)
        {
            foreach (var (argument, index) in CurrentMethod.Arguments.Select((x, i) => (x, i)))
            {
                if (argument.Name.Equals(id.ExpressionString))
                    return (argument, index);
            }
            return null;
        }
        public (VeinArgumentRef, int index) GetCurrentArgument(IdentifierExpression id)
        {
            foreach (var (argument, index) in CurrentMethod.Arguments.Select((x, i) => (x, i)))
            {
                if (argument.Name.Equals(id.ExpressionString))
                    return (argument, index);
            }

            this.LogError($"Argument '{id}' is not found in '{this.CurrentMethod.Name}' function.", id);
            throw new SkipStatementException();
        }
        public VeinClass ResolveScopedIdentifierType(IdentifierExpression id)
        {
            if (ResolveArgument(id) is not null)
            {
                var a = ResolveArgument(id);
                return a.Value.Item1.Type;
            }
            if (CurrentScope.HasVariable(id))
                return CurrentScope.variables[id];
            if (CurrentMethod.Owner.ContainsField(id))
                return CurrentMethod.Owner.ResolveField(id)?.FieldType;
            var modType = Module.FindType(id.ExpressionString, Classes[CurrentMethod.Owner.FullName].Includes, false);
            if (modType is not null)
                return modType;
            this.LogError($"The name '{id}' does not exist in the current context.", id);
            throw new SkipStatementException();
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
        public VeinField ResolveField(IdentifierExpression id)
            => CurrentMethod.Owner.FindField(id.ExpressionString);

        public VeinField ResolveField(VeinClass targetType, IdentifierExpression id)
        {
            var field = targetType.FindField(id.ExpressionString);

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
            var method = targetType.FindMethod($"{invocation.Name}", invocation.Arguments.DetermineTypes(this));
            if (method is not null)
                return method;
            this.LogError($"The name '{invocation.Name}' does not exist in the current context.", invocation.Name);
            throw new SkipStatementException();
        }
    }

    public class CannotExistMainScopeException : Exception { }


    public class ScopeTransit : IDisposable
    {
        public readonly ManaScope Scope;

        public ScopeTransit(ManaScope scope) => Scope = scope;


        public void Dispose() => Scope.ExitScope();
    }

    public class ManaScope
    {
        public ManaScope TopScope { get; }
        public List<ManaScope> Scopes { get; } = new();
        public GeneratorContext Context { get; }

        public Dictionary<IdentifierExpression, VeinClass> variables { get; } = new();
        public Dictionary<IdentifierExpression, int> locals_index { get; } = new();


        public ManaScope(GeneratorContext gen, ManaScope owner = null)
        {
            this.Context = gen;
            if (owner is null)
                return;
            this.TopScope = owner;
            owner.Scopes.Add(this);
        }

        public ManaScope ExitScope()
        {
            if (this.TopScope is null)
                throw new CannotExistMainScopeException();
            Context.CurrentScope = this.TopScope;
            return this.TopScope;
        }

        public IDisposable EnterScope()
        {
            var result = new ManaScope(Context, this);
            Context.CurrentScope = result;
            return new ScopeTransit(result);
        }

        public bool HasVariable(IdentifierExpression id)
            => variables.ContainsKey(id);

        public (VeinClass @class, int index) GetVariable(IdentifierExpression id)
            => (variables[id], locals_index[id]);

        public ManaScope DefineVariable(IdentifierExpression id, VeinClass type, int localIndex)
        {
            if (HasVariable(id))
            {
                Context.LogError($"A local variable named '{id}' is already defined in this scope", id);
                return this;
            }
            variables.Add(id, type);
            locals_index.Add(id, localIndex);
            return this;
        }
    }

    public static class GeneratorExtension
    {
        public static bool ContainsField(this VeinClass @class, IdentifierExpression id)
            => @class.FindField(id.ExpressionString) != null;

        public static VeinField ResolveField(this VeinClass @class, IdentifierExpression id)
            => @class.FindField(id.ExpressionString);

        public static void EmitUnary(this ILGenerator gen, UnaryExpressionSyntax node)
        {
            if (node.OperatorType == ExpressionType.NegateChecked && node.Operand.GetTypeCode().HasInteger())
            {
                var type = node.Operand.GetTypeCode();
                gen.EmitDefault(type);
                gen.EmitExpression(node.Operand);
                gen.EmitBinaryOperator(ExpressionType.SubtractChecked, type, type, type);
            }
            else
            {
                gen.EmitExpression(node.Operand);
                //gen.EmitUnaryOperator(node.NodeType, node.Operand.Type, node.Type);
            }
        }


        public static void EmitBinaryOperator(this ILGenerator gen, ExpressionType op, VeinTypeCode leftType = VeinTypeCode.TYPE_CLASS, VeinTypeCode rightType = VeinTypeCode.TYPE_CLASS, VeinTypeCode resultType = VeinTypeCode.TYPE_CLASS)
        {
            switch (op)
            {
                case ExpressionType.Add:
                    gen.Emit(OpCodes.ADD);
                    break;
                case ExpressionType.Subtract:
                    gen.Emit(OpCodes.SUB);
                    break;
                case ExpressionType.Multiply:
                    gen.Emit(OpCodes.MUL);
                    break;
                case ExpressionType.Divide:
                    gen.Emit(OpCodes.DIV);
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    gen.Emit(OpCodes.AND);
                    return;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    gen.Emit(OpCodes.OR);
                    return;
                case ExpressionType.LessThan:
                    gen.Emit(OpCodes.EQL_L);
                    return;
                case ExpressionType.LessThanOrEqual:
                    gen.Emit(OpCodes.EQL_LQ);
                    return;
                case ExpressionType.GreaterThan:
                    gen.Emit(OpCodes.EQL_H);
                    return;
                case ExpressionType.GreaterThanOrEqual:
                    gen.Emit(OpCodes.EQL_HQ);
                    return;
                case ExpressionType.NotEqual:
                    if (leftType == VeinTypeCode.TYPE_BOOLEAN)
                        goto case ExpressionType.ExclusiveOr;
                    gen.Emit(OpCodes.EQL_F);
                    return;
                case ExpressionType.Equal:
                    gen.Emit(OpCodes.EQL_T);
                    return;
                case ExpressionType.ExclusiveOr:
                    gen.Emit(OpCodes.XOR);
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void EmitDefault(this ILGenerator gen, VeinTypeCode code)
        {
            switch (code)
            {
                case VeinTypeCode.TYPE_OBJECT:
                case VeinTypeCode.TYPE_STRING:
                    gen.Emit(OpCodes.LDNULL);
                    break;
                case VeinTypeCode.TYPE_CHAR:
                case VeinTypeCode.TYPE_I2:
                case VeinTypeCode.TYPE_BOOLEAN:
                    gen.Emit(OpCodes.LDC_I2_0);
                    break;
                case VeinTypeCode.TYPE_I4:
                    gen.Emit(OpCodes.LDC_I4_0);
                    break;
                case VeinTypeCode.TYPE_I8:
                    gen.Emit(OpCodes.LDC_I8_0);
                    break;
                case VeinTypeCode.TYPE_R4:
                    gen.Emit(OpCodes.LDC_F4, .0f);
                    break;
                case VeinTypeCode.TYPE_R8:
                    gen.Emit(OpCodes.LDC_F8, .0d);
                    break;
                default:
                    throw new NotSupportedException($"{code}");
            }
        }

        public static VeinTypeCode GetTypeCodeFromNumber(NumericLiteralExpressionSyntax number) =>
        number switch
        {
            ByteLiteralExpressionSyntax => VeinTypeCode.TYPE_U1,
            SByteLiteralExpressionSyntax => VeinTypeCode.TYPE_I1,
            Int16LiteralExpressionSyntax => VeinTypeCode.TYPE_I2,
            UInt16LiteralExpressionSyntax => VeinTypeCode.TYPE_U2,
            Int32LiteralExpressionSyntax => VeinTypeCode.TYPE_I4,
            UInt32LiteralExpressionSyntax => VeinTypeCode.TYPE_U4,
            Int64LiteralExpressionSyntax => VeinTypeCode.TYPE_I8,
            UInt64LiteralExpressionSyntax => VeinTypeCode.TYPE_U8,
            HalfLiteralExpressionSyntax => VeinTypeCode.TYPE_R2,
            SingleLiteralExpressionSyntax => VeinTypeCode.TYPE_R4,
            DoubleLiteralExpressionSyntax => VeinTypeCode.TYPE_R8,
            DecimalLiteralExpressionSyntax => VeinTypeCode.TYPE_R16,
            _ => throw new NotSupportedException($"{number} is not support number.")
        };

        public static ILGenerator EmitExpression(this ILGenerator gen, ExpressionSyntax expr)
        {
            if (expr is AccessExpressionSyntax access)
            {
                gen.EmitAccess(access);
                return gen;
            }

            if (expr is LiteralExpressionSyntax literal)
            {
                gen.EmitLiteral(literal);
                return gen;
            }

            if (expr is NewExpressionSyntax @new)
            {
                gen.EmitCreateObject(@new);
                return gen;
            }

            if (expr is ArgumentExpression arg)
            {
                gen.EmitExpression(arg.Value);
                return gen;
            }

            if (expr is BinaryExpressionSyntax bin)
            {
                gen.EmitBinaryExpression(bin);
                return gen;
            }

            if (expr is IdentifierExpression @ref)
            {
                gen.EmitLoadIdentifierReference(@ref);
                return gen;
            }



            throw new NotImplementedException();
        }

        public static void EmitThis(this ILGenerator gen) => gen.Emit(OpCodes.LD_THIS);

        [Flags]
        public enum AccessFlags
        {
            NONE = 0,
            VARIABLE = 1 << 1,
            ARGUMENT = 1 << 2,
            FIELD = 1 << 3,
            STATIC_FIELD = 1 << 4,
            CLASS = 1 << 5
        }

        public static AccessFlags GetAccessFlags(this ILGenerator gen, IdentifierExpression id)
        {
            var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var flags = AccessFlags.NONE;

            if (context.CurrentScope.HasVariable(id))
                flags |= AccessFlags.VARIABLE;

            // second order: search argument
            var args = context.ResolveArgument(id);
            if (args is not null)
                flags |= AccessFlags.ARGUMENT;

            // third order: find field
            var field = context.ResolveField(id);
            if (field is { IsStatic: false })
                flags |= AccessFlags.FIELD;
            if (field is { IsStatic: true })
                flags |= AccessFlags.STATIC_FIELD;

            // four order: find class
            if (context.HasDefinedType(id))
                flags |= AccessFlags.CLASS;

            return flags;
        }
        public static void EmitLoadIdentifierReference(this ILGenerator gen, IdentifierExpression id)
        {
            var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

            // first order: search variable
            if (context.CurrentScope.HasVariable(id))
            {
                var index = context.CurrentScope.locals_index[id];
                var type = context.CurrentScope.variables[id];
                gen.WriteDebugMetadata($"/* access local, var: '{id}', index: '{index}', type: '{type.FullName.NameWithNS}' */");
                gen.Emit(OpCodes.LDLOC_S, index);
                return;
            }

            // second order: search argument
            var args = context.ResolveArgument(id);
            if (args is not null)
            {
                var (_, index) = args.Value;
                gen.Emit(OpCodes.LDARG_S, index + 1); // todo apply variants
                return;
            }

            // third order: find field
            var field = context.ResolveField(id);
            if (field is not null)
            {
                gen.Emit(field.IsStatic ? OpCodes.LDSF : OpCodes.LDF, field);
                return;
            }

            context.LogError($"The name '{id}' does not exist in the current context.", id);
        }

        public static void EmitBinaryExpression(this ILGenerator gen, BinaryExpressionSyntax bin)
        {
            if (bin.OperatorType == ExpressionType.Assign)
            {
                gen.EmitAssignExpression(bin);
                return;
            }

            var left = bin.Left;
            var right = bin.Right;

            gen.EmitExpression(left);
            gen.EmitBinaryOperator(bin.OperatorType);
            gen.EmitExpression(right);
        }

        public static void EmitAssignExpression(this ILGenerator gen, BinaryExpressionSyntax bin)
        {
            var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

            if (bin.Left is IdentifierExpression id)
            {
                var field = context.ResolveField(context.CurrentMethod.Owner, id);
                gen.EmitExpression(bin.Right);
                gen.Emit(field.IsStatic ? OpCodes.STSF : OpCodes.STF, field);
                return;
            }

            if (bin is { Left: AccessExpressionSyntax { Left: ThisAccessExpression, Right: IdentifierExpression id1 } })
            {
                var field = context.ResolveField(context.CurrentMethod.Owner, id1);
                if (field.IsStatic)
                {
                    context.LogError($"Static member '{id1}' cannot be accessed with an instance reference.", id1);
                    return;
                }
                gen.EmitExpression(bin.Right);
                gen.Emit(OpCodes.STF, field);
                return;
            }

            throw new NotSupportedException();
        }
        public static bool CanImplicitlyCast(this VeinTypeCode code, NumericLiteralExpressionSyntax numeric)
        {
            if (code.IsCompatibleNumber(numeric.GetTypeCode()))
                return true;

            return code switch
            {
                VeinTypeCode.TYPE_I1 => long.Parse(numeric.ExpressionString) is <= sbyte.MaxValue and >= sbyte.MinValue,
                VeinTypeCode.TYPE_I2 => long.Parse(numeric.ExpressionString) is <= short.MaxValue and >= short.MinValue,
                VeinTypeCode.TYPE_I4 => long.Parse(numeric.ExpressionString) is <= int.MaxValue and >= int.MinValue,
                VeinTypeCode.TYPE_U1 => ulong.Parse(numeric.ExpressionString) is <= byte.MaxValue and >= byte.MinValue,
                VeinTypeCode.TYPE_U2 => ulong.Parse(numeric.ExpressionString) is <= ushort.MaxValue and >= ushort.MinValue,
                VeinTypeCode.TYPE_U4 => ulong.Parse(numeric.ExpressionString) is <= uint.MaxValue and >= uint.MinValue,
                _ => false
            };
        }

        public static VeinTypeCode GetTypeCode(this ExpressionSyntax exp)
        {
            if (exp is NumericLiteralExpressionSyntax num)
                return GetTypeCodeFromNumber(num);
            if (exp is BoolLiteralExpressionSyntax)
                return VeinTypeCode.TYPE_BOOLEAN;
            if (exp is StringLiteralExpressionSyntax)
                return VeinTypeCode.TYPE_STRING;
            if (exp is NullLiteralExpressionSyntax)
                return VeinTypeCode.TYPE_NONE;
            return VeinTypeCode.TYPE_CLASS;
        }
        public static void EmitCreateObject(this ILGenerator gen, NewExpressionSyntax @new)
        {
            var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var type = context.ResolveType(@new.TargetType.Typeword);

            if (type.IsStatic)
            {
                context.LogError($"Cannot create an instance of the static class '{type}'", @new.TargetType);
                return;
            }

            if (@new.IsObject)
            {
                var args = ((ObjectCreationExpression)@new.CtorArgs).Args.ToArray();
                foreach (var arg in args)
                    gen.EmitExpression(arg);
                gen.Emit(OpCodes.NEWOBJ, type);
                var ctor = type.FindMethod("ctor", args.DetermineTypes(context));

                if (ctor is null)
                {
                    context.LogError(
                        $"'{type}' does not contain a constructor that takes '{args.Length}' arguments.",
                        @new.TargetType);
                    return;
                }

                gen.Emit(OpCodes.CALL, ctor);
                return;
            }

            if (@new.IsArray)
            {
                var exp = (ArrayInitializerExpression)@new.CtorArgs;
                var init_method = gen.ConstructArrayTypeInitialization(@new.TargetType,
                    exp);

                if (init_method is not null) // when method for init array is successful created/resolved
                {
                    gen.Emit(OpCodes.CALL, init_method);
                    return;
                }

                var sizes = exp.Sizes;

                if (sizes.Length > 1)
                    throw new NotSupportedException($"Currently array rank greater 1 not supported.");

                var size = sizes.Single();

                gen.Emit(OpCodes.LD_TYPE, type);
                gen.EmitExpression(size); // todo maybe need resolve cast problem
                gen.Emit(OpCodes.NEWARR);
                return;
            }

            throw new Exception("EmitCreateObject");
        }


        public static VeinMethod ConstructArrayTypeInitialization(this ILGenerator gen, TypeExpression expression, ArrayInitializerExpression arrayInitializer)
        {
            var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

            var type = context.ResolveType(expression.Typeword);

            if (!type.IsValueType)
                return null;

            var sizes = arrayInitializer.Sizes;
            var ctor = arrayInitializer.Args ?? new (Array.Empty<ExpressionSyntax>());

            if (sizes.Length > 1)
                throw new NotSupportedException($"Currently array rank greater 1 not supported.");
            var size = sizes.Single();

            if (size is not NumericLiteralExpressionSyntax)
                return null; // skip optimized type generation when size is variable and etc

            var size_value = size.ForceOptimization().Eval<int>();

            if (ctor.FillArgs.Length != 0 && ctor.FillArgs.Length != size_value)
                throw new NotSupportedException($"Incorrect array size.");

            var name = $"StaticArray_INIT_{expression.Typeword.Identifier.ExpressionString}_{size_value}_{ctor.FillArgs.Length}";

            var arrayConstructor = context.CreateHiddenType(name);

            if (arrayConstructor.IsSpecial)
                return arrayConstructor.FindMethod("$init"); // skip already defined type

            arrayConstructor.Flags |= ClassFlags.Special;

            var method = arrayConstructor.DefineMethod("$init", MethodFlags.Public | MethodFlags.Static,
                VeinTypeCode.TYPE_ARRAY.AsClass());

            var body = method.GetGenerator();

            body.Emit(OpCodes.NOP);
            body.Emit(OpCodes.LD_TYPE, type);               // load type token
            body.Emit(OpCodes.LDC_I8_S, (long)size_value);  // load size
            body.Emit(OpCodes.NEWARR);                      // load size array and allocate array with fixed size and passed type
            if (size_value == 0)
            {
                body.Emit(OpCodes.RET);
                return method;
            }
            foreach (var i in ..size_value)
            {
                body.EmitExpression(ctor.FillArgs[i]);
                body.Emit(OpCodes.STELEM_S, i);
            }
            body.Emit(OpCodes.RET);
            return method;
        }

        public static IEnumerable<VeinClass> DetermineTypes(this IEnumerable<ExpressionSyntax> exps, GeneratorContext context)
            => exps.Select(x => x.DetermineType(context)).Where(x => x is not null /* if failed determine skip analyze */);

        public static VeinClass DetermineType(this ExpressionSyntax exp, GeneratorContext context)
        {
            if (exp.CanOptimizationApply())
                return exp.ForceOptimization().DetermineType(context);
            if (exp is LiteralExpressionSyntax literal)
                return literal.GetTypeCode().AsClass();
            if (exp is AccessExpressionSyntax access)
                return access.ResolveType(context);
            if (exp is BinaryExpressionSyntax bin)
            {
                if (bin.OperatorType.IsLogic())
                    return VeinTypeCode.TYPE_BOOLEAN.AsClass();
                var (lt, rt) = bin.Fusce(context);

                return lt == rt ? lt : ExplicitConversion(lt, rt);
            }
            if (exp is NewExpressionSyntax { IsArray: false } @new)
                return context.ResolveType(@new.TargetType.Typeword);
            if (exp is NewExpressionSyntax { IsArray: true })
                return VeinTypeCode.TYPE_ARRAY.AsClass();
            if (exp is InvocationExpression inv)
                return inv.ResolveReturnType(context);
            if (exp is ArgumentExpression { Value: IdentifierExpression } arg1)
                return arg1.Value.DetermineType(context);
            if (exp is IdentifierExpression id)
                return context.ResolveScopedIdentifierType(id);
            context.LogError($"Cannot determine expression.", exp);
            throw new SkipStatementException();
        }

        public static VeinClass ResolveType(this AccessExpressionSyntax access, GeneratorContext context)
        {
            var chain = access.ToChain().ToArray();
            var lastToken = chain.Last();

            if (lastToken is InvocationExpression method)
                return method.ResolveReturnType(context, chain);
            if (lastToken is IndexerExpression)
            {
                context.LogError($"indexer is not support.", lastToken);
                return null;
            }
            if (lastToken is OperatorExpressionSyntax)
                return chain.SkipLast(1).ResolveMemberType(context);
            if (lastToken is IdentifierExpression)
                return chain.ResolveMemberType(context);
            context.LogError($"Cannot determine expression.", lastToken);

            return null;
        }

        public static (VeinClass, VeinClass) Fusce(this BinaryExpressionSyntax binary, GeneratorContext context)
        {
            var lt = binary.Left.DetermineType(context);
            var rt = binary.Right.DetermineType(context);

            return (lt, rt);
        }

        public static VeinClass ResolveMemberType(this IEnumerable<ExpressionSyntax> chain, GeneratorContext context)
        {
            var t = default(VeinClass);
            var prev_id = default(IdentifierExpression);
            using var enumerator = chain.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var exp = enumerator.Current;
                if (exp is not IdentifierExpression id)
                {
                    context.LogError($"Incorrect state of expression.", exp);
                    return null;
                }

                t = t is null ?
                    context.ResolveScopedIdentifierType(id) :
                    context.ResolveField(t, prev_id, id)?.FieldType;
                prev_id = id;
            }

            return t;
        }

        public static VeinClass ResolveReturnType(this InvocationExpression inv, GeneratorContext context)
            => context.ResolveMethod(context.CurrentMethod.Owner, inv)?.ReturnType;

        public static VeinClass ResolveReturnType(this InvocationExpression member,
            GeneratorContext context, IEnumerable<ExpressionSyntax> chain)
        {
            var t = default(VeinClass);
            var prev_id = default(IdentifierExpression);
            var enumerator = chain.ToArray();
            for (var i = 0; i != enumerator.Length; i++)
            {
                var exp = enumerator[i] as IdentifierExpression;

                if (exp is null && enumerator[i] is InvocationExpression inv)
                    exp = inv.Name as IdentifierExpression;

                if (i + 1 == enumerator.Length)
                    return context.ResolveMethod(t ?? context.CurrentMethod.Owner, prev_id, exp, member)
                        ?.ReturnType;
                t = t is null ?
                    context.ResolveScopedIdentifierType(exp) :
                    context.ResolveField(t, prev_id, exp)?.FieldType;
                prev_id = exp;
            }

            context.LogError($"Incorrect state of expression.", member);
            return null;
        }

        public static VeinClass ExplicitConversion(VeinClass t1, VeinClass t2) =>
            throw new Exception($"ExplicitConversion: {t1?.FullName.NameWithNS} and {t2?.FullName.NameWithNS}");

        public static void EmitThrow(this ILGenerator generator, QualityTypeName type)
        {
            generator.Emit(OpCodes.NEWOBJ, type);
            generator.Emit(OpCodes.CALL, GetDefaultCtor(type));
            generator.Emit(OpCodes.THROW);
        }
        public static VeinMethod GetDefaultCtor(QualityTypeName t) => throw new NotImplementedException();
        public static void EmitIfElse(this ILGenerator generator, IfStatementSyntax ifStatement)
        {
            var elseLabel = generator.DefineLabel();
            var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
            var expType = ifStatement.Expression.DetermineType(ctx);

            if (ifStatement.Expression is BoolLiteralExpressionSyntax @bool)
            {
                if (@bool.Value)
                    generator.EmitStatement(ifStatement.ThenStatement);
                else
                    generator.Emit(OpCodes.JMP, elseLabel);
            }
            else if (expType.TypeCode == VeinTypeCode.TYPE_BOOLEAN)
            {
                generator.EmitExpression(ifStatement.Expression);
                generator.Emit(OpCodes.JMP_F, elseLabel);
                generator.EmitStatement(ifStatement.ThenStatement);
            }
            else
            {
                ctx.LogError($"Cannot implicitly convert type '{expType}' to 'Boolean'", ifStatement.Expression);
                return;
            }
            generator.UseLabel(elseLabel);

            if (ifStatement.ElseStatement is null)
                return;

            generator.EmitStatement(ifStatement.ElseStatement);
        }

        public static void EmitStatement(this ILGenerator generator, StatementSyntax statement)
        {
            if (statement.IsBrokenToken)
                return;

            if (statement is ReturnStatementSyntax ret1)
                generator.EmitReturn(ret1);
            else if (statement is IfStatementSyntax theIf)
                generator.EmitIfElse(theIf);
            else if (statement is QualifiedExpressionStatement { Value: InvocationExpression invoke })
            {
                var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
                generator.EmitCall(ctx.CurrentMethod.Owner, invoke);
            }
            else if (statement is QualifiedExpressionStatement { Value: AccessExpressionSyntax access })
                generator.EmitAccess(access);
            else if (statement is WhileStatementSyntax @while)
                generator.EmitWhileStatement(@while);
            else if (statement is QualifiedExpressionStatement { Value: BinaryExpressionSyntax } qes2)
                generator.EmitBinaryExpression((BinaryExpressionSyntax)qes2.Value);

            else if (statement is LocalVariableDeclaration localVariable)
                generator.EmitLocalVariable(localVariable);
            else if (statement is ForeachStatementSyntax @foreach)
                generator.EmitForeach(@foreach);
            else
                throw new NotImplementedException();
        }


        public static void EmitForeach(this ILGenerator generator, ForeachStatementSyntax @foreach)
        {
            var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");

            var type = @foreach.Expression.DetermineType(ctx);

            generator.EmitLocalVariableWithType(@foreach.Variable, type);
            using (ctx.CurrentScope.EnterScope())
            {
            }
        }

        public static void EmitLocalVariableWithType(this ILGenerator generator, LocalVariableDeclaration localVar, VeinClass type)
        {
            var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
            var scope = ctx.CurrentScope;

            if (!localVar.Body.IsEmpty)
            {
                ctx.LogError($"Local variable already defined type.", localVar);
                return;
            }

            var locIndex = generator.EnsureLocal(localVar.Identifier.ExpressionString, type);

            if (locIndex < 0)
            {
                ctx.LogError($"Too many variables in '{ctx.CurrentMethod.Name}' function.", localVar);
                return;
            }

            scope.DefineVariable(localVar.Identifier, type, locIndex);

            generator.Emit(OpCodes.LDNULL);
            generator.Emit(OpCodes.STLOC_S, locIndex); // TODO optimization for STLOC_0,1,2 and etc
        }

        public static void EmitLocalVariable(this ILGenerator generator, LocalVariableDeclaration localVar)
        {
            var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
            var scope = ctx.CurrentScope;

            if (localVar.Body.IsEmpty)
            {
                ctx.LogError($"Implicitly-typed local variable must be initialized.", localVar);
                return;
            }

            var exp = localVar.Body.Get();
            var type = exp.DetermineType(scope.Context);


            var locIndex = generator.EnsureLocal(localVar.Identifier.ExpressionString, type);

            if (locIndex < 0)
            {
                ctx.LogError($"Too many variables in '{ctx.CurrentMethod.Name}' function.", localVar);
                return;
            }

            scope.DefineVariable(localVar.Identifier, type, locIndex);

            generator.EmitExpression(exp);
            generator.Emit(OpCodes.STLOC_S, locIndex); // TODO optimization for STLOC_0,1,2 and etc
        }

        public static ILGenerator EmitAccess(this ILGenerator gen, AccessExpressionSyntax access)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

            if (access is { Left: IdentifierExpression id, Right: InvocationExpression invoke })
            {
                var flags = gen.GetAccessFlags(id);

                // first order: variable
                if (flags.HasFlag(AccessFlags.VARIABLE))
                {
                    var (@class, var_index) = ctx.CurrentScope.GetVariable(id);

                    return gen.Emit(OpCodes.LDLOC_S, var_index).
                        EmitCall(@class, invoke);
                }
                // second order: argument
                if (flags.HasFlag(AccessFlags.ARGUMENT))
                {
                    var (arg, index) = ctx.GetCurrentArgument(id);
                    return gen.Emit(OpCodes.LDARG_S, index)
                        .EmitCall(arg.Type, invoke);
                }
                // three order: field
                if (flags.HasFlag(AccessFlags.FIELD))
                {
                    var field = ctx.ResolveField(id);
                    return gen.Emit(OpCodes.LDF, field)
                        .EmitCall(field.FieldType, invoke);
                }
                // four order: static field
                if (flags.HasFlag(AccessFlags.STATIC_FIELD))
                {
                    var field = ctx.ResolveField(id);
                    return gen.Emit(OpCodes.LDSF, field)
                        .EmitCall(field.FieldType, invoke);
                }
                // five order: static class
                if (flags.HasFlag(AccessFlags.CLASS))
                {
                    var @class = ctx.ResolveType(id);
                    return gen.EmitCall(@class, invoke);
                }

                return gen;
            }

            if (access is { Left: ThisAccessExpression, Right: IdentifierExpression id1 })
            {
                var flags = gen.GetAccessFlags(id1);

                // first order: variable
                if (flags.HasFlag(AccessFlags.VARIABLE))
                {
                    var (_, var_index) = ctx.CurrentScope.GetVariable(id1);
                    return gen.Emit(OpCodes.LDLOC_S, var_index);
                }
                // second order: argument
                if (flags.HasFlag(AccessFlags.ARGUMENT))
                {
                    var (_, index) = ctx.GetCurrentArgument(id1);
                    return gen.Emit(OpCodes.LDARG_S, index);
                }
                // three order: field
                if (flags.HasFlag(AccessFlags.FIELD))
                {
                    var field = ctx.ResolveField(id1);
                    return gen.Emit(OpCodes.LDF, field);
                }
                // four order: static field
                if (flags.HasFlag(AccessFlags.STATIC_FIELD))
                    ctx.LogError($"Static member '{id1}' cannot be accessed with an instance reference.", id1);
                // five order: static class
                if (flags.HasFlag(AccessFlags.CLASS))
                    ctx.LogError($"'{id1}' is a type, which is not valid in current context.", id1);

                return gen;
            }

            throw new NotSupportedException();
        }

        public static ILGenerator EmitCall(this ILGenerator gen, VeinClass @class, InvocationExpression invocation)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

            var method = ctx.ResolveMethod(@class, invocation);
            var args = invocation.Arguments;

            foreach (var arg in args)
                gen.EmitExpression(arg);

            gen.Emit(OpCodes.CALL, method);
            return gen;
        }

        public static void EmitNumericLiteral(this ILGenerator generator, NumericLiteralExpressionSyntax literal)
        {
            switch (literal)
            {
                case Int16LiteralExpressionSyntax i16:
                    {
                        if (i16 is { Value: 0 }) generator.Emit(OpCodes.LDC_I2_0);
                        if (i16 is { Value: 1 }) generator.Emit(OpCodes.LDC_I2_1);
                        if (i16 is { Value: 2 }) generator.Emit(OpCodes.LDC_I2_2);
                        if (i16 is { Value: 3 }) generator.Emit(OpCodes.LDC_I2_3);
                        if (i16 is { Value: 4 }) generator.Emit(OpCodes.LDC_I2_4);
                        if (i16 is { Value: 5 }) generator.Emit(OpCodes.LDC_I2_5);
                        if (i16.Value is > 5 or < 0)
                            generator.Emit(OpCodes.LDC_I2_S, i16.Value);
                        break;
                    }
                case Int32LiteralExpressionSyntax i32:
                    {
                        if (i32 is { Value: 0 }) generator.Emit(OpCodes.LDC_I4_0);
                        if (i32 is { Value: 1 }) generator.Emit(OpCodes.LDC_I4_1);
                        if (i32 is { Value: 2 }) generator.Emit(OpCodes.LDC_I4_2);
                        if (i32 is { Value: 3 }) generator.Emit(OpCodes.LDC_I4_3);
                        if (i32 is { Value: 4 }) generator.Emit(OpCodes.LDC_I4_4);
                        if (i32 is { Value: 5 }) generator.Emit(OpCodes.LDC_I4_5);
                        if (i32.Value is > 5 or < 0)
                            generator.Emit(OpCodes.LDC_I4_S, i32.Value);
                        break;
                    }
                case Int64LiteralExpressionSyntax i64:
                    {
                        if (i64 is { Value: 0 }) generator.Emit(OpCodes.LDC_I8_0);
                        if (i64 is { Value: 1 }) generator.Emit(OpCodes.LDC_I8_1);
                        if (i64 is { Value: 2 }) generator.Emit(OpCodes.LDC_I8_2);
                        if (i64 is { Value: 3 }) generator.Emit(OpCodes.LDC_I8_3);
                        if (i64 is { Value: 4 }) generator.Emit(OpCodes.LDC_I8_4);
                        if (i64 is { Value: 5 }) generator.Emit(OpCodes.LDC_I8_5);
                        if (i64.Value is > 5 or < 0)
                            generator.Emit(OpCodes.LDC_I8_S, i64.Value);
                        break;
                    }
                case DecimalLiteralExpressionSyntax f128:
                    {
                        generator.Emit(OpCodes.LDC_F16, f128.Value);
                        break;
                    }
                case DoubleLiteralExpressionSyntax f64:
                    {
                        generator.Emit(OpCodes.LDC_F8, f64.Value);
                        break;
                    }
                case SingleLiteralExpressionSyntax f32:
                    {
                        generator.Emit(OpCodes.LDC_F4, f32.Value);
                        break;
                    }
                case HalfLiteralExpressionSyntax h32:
                    {
                        generator.Emit(OpCodes.LDC_F2, h32.Value);
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static ILGenerator EmitLiteral(this ILGenerator generator, LiteralExpressionSyntax literal)
        {
            if (literal is NumericLiteralExpressionSyntax numeric)
                generator.EmitNumericLiteral(numeric);
            else if (literal is StringLiteralExpressionSyntax stringLiteral)
                generator.Emit(OpCodes.LDC_STR, UnEscapeSymbols(stringLiteral.Value));
            else if (literal is BoolLiteralExpressionSyntax boolLiteral)
                generator.Emit(boolLiteral.Value ? OpCodes.LDC_I2_1 : OpCodes.LDC_I2_0);
            else if (literal is NullLiteralExpressionSyntax)
                generator.Emit(OpCodes.LDNULL);
            return generator;
        }

        private static string UnEscapeSymbols(string str)
            => Regex.Unescape(str);

        public static ILGenerator EmitReturn(this ILGenerator generator, ReturnStatementSyntax statement) => statement switch
        {
            not { Expression: { } } => generator.Emit(OpCodes.RET),
            { Expression: NaNLiteralExpressionSyntax }
                => generator.Emit(OpCodes.LDC_F4, float.NaN).Emit(OpCodes.RET),
            { Expression: NegativeInfinityLiteralExpressionSyntax }
                => generator.Emit(OpCodes.LDC_F4, float.NegativeInfinity).Emit(OpCodes.RET),
            { Expression: InfinityLiteralExpressionSyntax }
                => generator.Emit(OpCodes.LDC_F4, float.PositiveInfinity).Emit(OpCodes.RET),
            { Expression: LiteralExpressionSyntax lit }
                => generator.EmitLiteral(lit).Emit(OpCodes.RET),
            _ => generator.EmitExpression(statement.Expression).Emit(OpCodes.RET),
        };

        public static void EmitWhileStatement(this ILGenerator gen, WhileStatementSyntax @while)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var start = gen.DefineLabel();
            var end = gen.DefineLabel();
            var expType = @while.Expression.DetermineType(ctx);
            gen.UseLabel(start);
            if (expType.FullName == VeinTypeCode.TYPE_BOOLEAN.AsClass().FullName)
            {
                gen.EmitExpression(@while.Expression);
                gen.Emit(OpCodes.JMP_F, end);
            }
            else // todo implicit boolean
            {
                ctx.LogError($"Cannot implicitly convert type '{expType}' to 'Boolean'", @while.Expression);
                return;
            }
            gen.EmitStatement(@while.Statement);
            gen.Emit(OpCodes.JMP, start);
            gen.UseLabel(end);
        }
    }
}
