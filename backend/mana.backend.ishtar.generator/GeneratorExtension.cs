namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using mana.extensions;
    using mana.ishtar.emit;
    using mana.reflection;
    using mana.runtime;
    using Spectre.Console;
    using mana.syntax;
    using Xunit;

    public class GeneratorContext
    {
        internal ManaModuleBuilder Module { get; set; }
        internal Dictionary<QualityTypeName, ClassBuilder> Classes { get; } = new();
        internal DocumentDeclaration Document { get; set; }

        public List<string> Errors = new ();

        public Dictionary<ManaMethod, ManaScope> Scopes { get; } = new();

        public ManaMethod CurrentMethod { get; set; }
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

        public ManaScope CreateScope()
        {
            if (CurrentScope is not null)
                return CurrentScope.EnterScope();
            CurrentScope = new ManaScope(this);
            Scopes.Add(CurrentMethod, CurrentScope);
            return CurrentScope;
        }

        public ManaClass ResolveType(TypeSyntax targetTypeTypeword)
            => Module.FindType(targetTypeTypeword.Identifier.ExpressionString,
                Classes[CurrentMethod.Owner.FullName].Includes);
        public ManaClass ResolveType(IdentifierExpression targetTypeTypeword)
            => Module.FindType(targetTypeTypeword.ExpressionString,
                Classes[CurrentMethod.Owner.FullName].Includes);


        public ClassBuilder CreateHiddenType(TypeSyntax targetTypeTypeword)
            => CreateHiddenType(targetTypeTypeword.Identifier);
        public ClassBuilder CreateHiddenType(IdentifierExpression targetTypeTypeword)
            => CreateHiddenType(targetTypeTypeword.ExpressionString);
        public ClassBuilder CreateHiddenType(string name)
        {
            QualityTypeName fullName = $"{Module.Name}%global::internal/{name}";

            var currentType = Module.FindType(fullName, false, false);

            if (currentType is not UnresolvedManaClass)
                return Assert.IsType<ClassBuilder>(currentType);

            var b = Module.DefineClass(fullName);

            b.Flags |= ClassFlags.Internal;

            Classes.Add(b.FullName, b);

            return b;
        }

        public (ManaArgumentRef, int index)? ResolveArgument(IdentifierExpression id)
        {
            foreach (var (argument, index) in CurrentMethod.Arguments.Select((x, i) => (x, i)))
            {
                if (argument.Name.Equals(id.ExpressionString))
                    return (argument, index);
            }
            return null;
        }
        public ManaClass ResolveScopedIdentifierType(IdentifierExpression id)
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
            var modType = Module.FindType(id.ExpressionString, Classes[CurrentMethod.Owner.FullName].Includes);
            if (modType is not null)
                return modType;
            this.LogError($"The name '{id}' does not exist in the current context.", id);
            return null;
        }
        public ManaField ResolveField(ManaClass targetType, IdentifierExpression target, IdentifierExpression id)
        {
            var field = targetType.FindField(id.ExpressionString);

            if (field is not null)
                return field;
            this.LogError($"'{targetType.FullName.NameWithNS}' does not contain " +
                          $"a definition for '{target.ExpressionString}' and " +
                          $"no extension method '{id.ExpressionString}' accepting " +
                          $"a first argument of type '{targetType.FullName.NameWithNS}' could be found.", id);
            return null;
        }
        public ManaField ResolveField(IdentifierExpression id)
            => CurrentMethod.Owner.FindField(id.ExpressionString);

        public ManaField ResolveField(ManaClass targetType, IdentifierExpression id)
        {
            var field = targetType.FindField(id.ExpressionString);

            if (field is not null)
                return field;
            this.LogError($"The name '{id}' does not exist in the current context.", id);
            return null;
        }
        public ManaMethod ResolveMethod(
            ManaClass targetType,
            IdentifierExpression target,
            IdentifierExpression id,
            MethodInvocationExpression invocation)
        {
            var method = targetType.FindMethod(id.ExpressionString, invocation.Arguments.DetermineTypes(this));
            if (method is not null)
                return method;
            if (target is not null)
                this.LogError($"'{targetType.FullName.NameWithNS}' does not contain " +
                          $"a definition for '{target.ExpressionString}' and " +
                          $"no extension method '{id.ExpressionString}' accepting " +
                          $"a first argument of type '{targetType.FullName.NameWithNS}' could be found.", id);
            else
                this.LogError($"The name '{id}' does not exist in the current context.", id);
            return null;
        }

        public ManaMethod ResolveMethod(
            ManaClass targetType,
            IdentifierExpression id,
            MethodInvocationExpression invocation)
        {
            var method = targetType.FindMethod(id.ExpressionString, invocation.Arguments.DetermineTypes(this));
            if (method is not null)
                return method;
            this.LogError($"The name '{id}' does not exist in the current context.", id);
            return null;
        }
    }

    public class CannotExistMainScopeException : Exception {}


    public class ScopeTransit : IDisposable
    {
        private readonly ManaScope _scope;

        public ScopeTransit(ManaScope scope) => _scope = scope;


        public void Dispose() => _scope.ExitScope();
    }

    public class ManaScope
    {
        public ManaScope TopScope { get; }
        public List<ManaScope> Scopes { get; } = new();
        public GeneratorContext Context { get; }

        public Dictionary<IdentifierExpression, ManaClass> variables { get; } = new();
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

        public ManaScope DefineVariable(IdentifierExpression id, ManaClass type, int localIndex)
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
        public static bool ContainsField(this ManaClass @class, IdentifierExpression id)
            => @class.FindField(id.ExpressionString) != null;

        public static ManaField ResolveField(this ManaClass @class, IdentifierExpression id)
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


        public static void EmitBinaryOperator(this ILGenerator gen, ExpressionType op, ManaTypeCode leftType = ManaTypeCode.TYPE_CLASS, ManaTypeCode rightType = ManaTypeCode.TYPE_CLASS, ManaTypeCode resultType = ManaTypeCode.TYPE_CLASS)
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
                    if (leftType == ManaTypeCode.TYPE_BOOLEAN)
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

        public static void EmitDefault(this ILGenerator gen, ManaTypeCode code)
        {
            switch (code)
            {
                case ManaTypeCode.TYPE_OBJECT:
                case ManaTypeCode.TYPE_STRING:
                    gen.Emit(OpCodes.LDNULL);
                    break;
                case ManaTypeCode.TYPE_CHAR:
                case ManaTypeCode.TYPE_I2:
                case ManaTypeCode.TYPE_BOOLEAN:
                    gen.Emit(OpCodes.LDC_I2_0);
                    break;
                case ManaTypeCode.TYPE_I4:
                    gen.Emit(OpCodes.LDC_I4_0);
                    break;
                case ManaTypeCode.TYPE_I8:
                    gen.Emit(OpCodes.LDC_I8_0);
                    break;
                case ManaTypeCode.TYPE_R4:
                    gen.Emit(OpCodes.LDC_F4, .0f);
                    break;
                case ManaTypeCode.TYPE_R8:
                    gen.Emit(OpCodes.LDC_F8, .0d);
                    break;
                default:
                    throw new NotSupportedException($"{code}");
            }
        }

        public static ManaTypeCode GetTypeCodeFromNumber(NumericLiteralExpressionSyntax number) =>
        number switch
        {
            ByteLiteralExpressionSyntax => ManaTypeCode.TYPE_U1,
            SByteLiteralExpressionSyntax => ManaTypeCode.TYPE_I1,
            Int16LiteralExpressionSyntax => ManaTypeCode.TYPE_I2,
            UInt16LiteralExpressionSyntax => ManaTypeCode.TYPE_U2,
            Int32LiteralExpressionSyntax => ManaTypeCode.TYPE_I4,
            UInt32LiteralExpressionSyntax => ManaTypeCode.TYPE_U4,
            Int64LiteralExpressionSyntax => ManaTypeCode.TYPE_I8,
            UInt64LiteralExpressionSyntax => ManaTypeCode.TYPE_U8,
            HalfLiteralExpressionSyntax => ManaTypeCode.TYPE_R2,
            SingleLiteralExpressionSyntax => ManaTypeCode.TYPE_R4,
            DoubleLiteralExpressionSyntax => ManaTypeCode.TYPE_R8,
            DecimalLiteralExpressionSyntax => ManaTypeCode.TYPE_R16,
            _ => throw new NotSupportedException($"{number} is not support number.")
        };

        public static void EmitExpression(this ILGenerator gen, ExpressionSyntax expr)
        {
            if (expr is LiteralExpressionSyntax literal)
            {
                gen.EmitLiteral(literal);
                return;
            }

            if (expr is NewExpressionSyntax @new)
            {
                gen.EmitCreateObject(@new);
                return;
            }

            if (expr is ArgumentExpression arg)
            {
                gen.EmitExpression(arg.Value);
                return;
            }

            if (expr is BinaryExpressionSyntax bin)
            {
                gen.EmitBinaryExpression(bin);
                return;
            }

            if (expr is IdentifierExpression @ref)
            {
                gen.EmitIdentifierReference(@ref);
                return;
            }

            if (expr is MemberAccessExpression { Start: IdentifierExpression } member)
            {
                if (member.Start is IdentifierExpression { ExpressionString: "this" })
                    gen.EmitThis();
                else
                    gen.EmitIdentifierAccess(member);
                return;
            }

            throw new NotImplementedException();
        }

        public static void EmitIdentifierAccess(this ILGenerator gen, MemberAccessExpression member)
        {
            Assert.IsType<IdentifierExpression>(member.Start);
            gen.EmitCall(member);
        }

        public static void EmitThis(this ILGenerator gen) => gen.Emit(OpCodes.LD_THIS);

        public static void EmitIdentifierReference(this ILGenerator gen, IdentifierExpression id)
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



            if (bin.Left is MemberAccessExpression member)
            {
                if (member.Chain.Count() == 1 && member.Start.ExpressionString == "this")
                    gen.EmitIdentifierReference(member.Chain.Single() as IdentifierExpression);
                else
                    throw new NotSupportedException();
                gen.EmitExpression(bin.Right);
                return;
            }

            throw new NotSupportedException();
        }

        public static bool CanImplicitlyCast(this ManaTypeCode code, NumericLiteralExpressionSyntax numeric)
        {
            if (code.IsCompatibleNumber(numeric.GetTypeCode()))
                return true;

            return code switch
            {
                ManaTypeCode.TYPE_I1 => long.Parse(numeric.ExpressionString) is <= sbyte.MaxValue and >= sbyte.MinValue,
                ManaTypeCode.TYPE_I2 => long.Parse(numeric.ExpressionString) is <= short.MaxValue and >= short.MinValue,
                ManaTypeCode.TYPE_I4 => long.Parse(numeric.ExpressionString) is <= int.MaxValue and >= int.MinValue,
                ManaTypeCode.TYPE_U1 => ulong.Parse(numeric.ExpressionString) is <= byte.MaxValue and >= byte.MinValue,
                ManaTypeCode.TYPE_U2 => ulong.Parse(numeric.ExpressionString) is <= ushort.MaxValue and >= ushort.MinValue,
                ManaTypeCode.TYPE_U4 => ulong.Parse(numeric.ExpressionString) is <= uint.MaxValue and >= uint.MinValue,
                _ => false
            };
        }

        public static ManaTypeCode GetTypeCode(this ExpressionSyntax exp)
        {
            if (exp is NumericLiteralExpressionSyntax num)
                return GetTypeCodeFromNumber(num);
            if (exp is BoolLiteralExpressionSyntax)
                return ManaTypeCode.TYPE_BOOLEAN;
            if (exp is StringLiteralExpressionSyntax)
                return ManaTypeCode.TYPE_STRING;
            if (exp is NullLiteralExpressionSyntax)
                return ManaTypeCode.TYPE_NONE;
            return ManaTypeCode.TYPE_CLASS;
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


        public static ManaMethod ConstructArrayTypeInitialization(this ILGenerator gen, TypeExpression expression, ArrayInitializerExpression arrayInitializer)
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
                ManaTypeCode.TYPE_ARRAY.AsClass());

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

        public static IEnumerable<ManaClass> DetermineTypes(this IEnumerable<ExpressionSyntax> exps, GeneratorContext context)
            => exps.Select(x => x.DetermineType(context)).Where(x => x is not null /* if failed determine skip analyze */);

        public static ManaClass DetermineType(this ExpressionSyntax exp, GeneratorContext context)
        {
            if (exp.CanOptimizationApply())
                return exp.ForceOptimization().DetermineType(context);
            if (exp is LiteralExpressionSyntax literal)
                return literal.GetTypeCode().AsClass();
            if (exp is BinaryExpressionSyntax bin)
            {
                if (bin.OperatorType.IsLogic())
                    return ManaTypeCode.TYPE_BOOLEAN.AsClass();
                var (lt, rt) = bin.Fusce(context);

                return lt == rt ? lt : ExplicitConversion(lt, rt);
            }
            if (exp is NewExpressionSyntax @new)
                return context.ResolveType(@new.TargetType.Typeword);
            if (exp is ArgumentExpression { Value: MemberAccessExpression } arg)
                return (arg.Value as MemberAccessExpression).ResolveType(context);
            if (exp is MemberAccessExpression member)
                return member.ResolveType(context);
            if (exp is ArgumentExpression { Value: IdentifierExpression } arg1)
                return arg1.Value.DetermineType(context);
            if (exp is IdentifierExpression id)
                return context.ResolveScopedIdentifierType(id);
            context.LogError($"Cannot determine expression.", exp);
            return null;
        }

        public static ManaClass ResolveType(this MemberAccessExpression member, GeneratorContext context)
        {
            var chain = member.GetChain().ToArray();
            var lastToken = chain.Last();

            if (lastToken is MethodInvocationExpression method)
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

        public static (ManaClass, ManaClass) Fusce(this BinaryExpressionSyntax binary, GeneratorContext context)
        {
            var lt = binary.Left.DetermineType(context);
            var rt = binary.Right.DetermineType(context);

            return (lt, rt);
        }

        public static ManaClass ResolveMemberType(this IEnumerable<ExpressionSyntax> chain, GeneratorContext context)
        {
            var t = default(ManaClass);
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

        public static ManaClass ResolveReturnType(this MethodInvocationExpression member,
            GeneratorContext context, IEnumerable<ExpressionSyntax> chain)
        {
            var t = default(ManaClass);
            var prev_id = default(IdentifierExpression);
            var enumerator = chain.ToArray();
            for (var i = 0; i != enumerator.Length; i++)
            {
                var exp = enumerator[i] as IdentifierExpression;

                if (i + 1 == enumerator.Length - 1)
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


        public static ManaClass ExplicitConversion(ManaClass t1, ManaClass t2)
        {
            throw new NotImplementedException();
        }

        public static void EmitThrow(this ILGenerator generator, QualityTypeName type)
        {
            generator.Emit(OpCodes.NEWOBJ, type);
            generator.Emit(OpCodes.CALL, GetDefaultCtor(type));
            generator.Emit(OpCodes.THROW);
        }
        public static ManaMethod GetDefaultCtor(QualityTypeName t) => throw new NotImplementedException();
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
            else if (expType.TypeCode == ManaTypeCode.TYPE_BOOLEAN)
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
            else if (statement is WhileStatementSyntax @while)
                generator.EmitWhileStatement(@while);
            else if (statement is QualifiedExpressionStatement { Value: MemberAccessExpression } qes1)
                generator.EmitCall((MemberAccessExpression)qes1.Value);
            else if (statement is QualifiedExpressionStatement { Value: BinaryExpressionSyntax } qes2)
                generator.EmitBinaryExpression((BinaryExpressionSyntax)qes2.Value);
            else if (statement is LocalVariableDeclaration localVariable)
                generator.EmitLocalVariable(localVariable);
            else
                throw new NotImplementedException();
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

        public static void EmitCall(this ILGenerator generator, MemberAccessExpression qes)
        {
            var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");

            Assert.IsType<IdentifierExpression>(qes.Start);
            var chain = qes.Chain.ToArray();

            if (chain.Length == 1)
            {
                generator.EmitLocalCall((IdentifierExpression)qes.Start, (MethodInvocationExpression)chain[0]);
                return;
            }

            if (chain.Length == 2)
            {
                if (generator.HasClassIdentifier((IdentifierExpression)qes.Start))
                {
                    generator.EmitGlobalCall((IdentifierExpression)qes.Start,
                        chain[0] as IdentifierExpression, chain[1] as MethodInvocationExpression);
                }
                else
                {
                    generator.EmitReferencedCall((IdentifierExpression)qes.Start,
                        chain[0] as IdentifierExpression, chain[1] as MethodInvocationExpression);
                }
                return;
            }

            if (chain.Length == 3)
            {
                generator.EmitReferencedCall((IdentifierExpression)chain[0],
                    chain[1] as IdentifierExpression, chain[2] as MethodInvocationExpression);
                return;
            }
            throw new NotSupportedException();
        }

        public static bool HasClassIdentifier(this ILGenerator gen, IdentifierExpression id)
        {
            var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

            if (context.CurrentScope.HasVariable(id))
                return false;
            if (context.ResolveArgument(id) is not null)
                return false;
            if (context.ResolveField(id) is not null)
                return false;

            return true;
        }

        public static void EmitReferencedCall(this ILGenerator gen, IdentifierExpression variable, IdentifierExpression func,
            MethodInvocationExpression args)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var clazz = ctx.ResolveScopedIdentifierType(variable);

            var method = ctx.ResolveMethod(clazz, func, args);

            if (method is null)
            {
                ctx.LogError($"'{clazz.FullName.NameWithNS}' does not contain a definition for '{func}'.", func);
                return;
            }
            gen.EmitIdentifierReference(variable);
            foreach (var arg in args.Arguments)
                gen.EmitExpression(arg);

            gen.Emit(OpCodes.CALL, method);
        }

        public static void EmitGlobalCall(this ILGenerator gen, IdentifierExpression className, IdentifierExpression name,
            MethodInvocationExpression args)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var clazz = ctx.ResolveType(className);

            if (clazz is null)
            {
                ctx.LogError($"The name '{className}' does not exist in the current context.", className);
                return;
            }

            var method = ctx.ResolveMethod(clazz, name, args);

            if (method is null)
            {
                ctx.LogError($"'{clazz.FullName.NameWithNS}' does not contain a definition for '{name}'.", name);
                return;
            }

            foreach (var arg in args.Arguments)
                gen.EmitExpression(arg);

            gen.Emit(OpCodes.CALL, method);
        }
        private static void EmitLocalCall(this ILGenerator gen, IdentifierExpression name,
            MethodInvocationExpression args)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var method = ctx.ResolveMethod(ctx.CurrentMethod.Owner, name, args);

            if (method is null)
            {
                ctx.LogError($"'{ctx.CurrentMethod.Owner.FullName.NameWithNS}' does not contain a definition for '{name}'.", name);
                return;
            }
            gen.EmitThis();

            foreach (var arg in args.Arguments)
                gen.EmitExpression(arg);

            gen.Emit(OpCodes.CALL, method);
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

        public static void EmitLiteral(this ILGenerator generator, LiteralExpressionSyntax literal)
        {
            if (literal is NumericLiteralExpressionSyntax numeric)
                generator.EmitNumericLiteral(numeric);
            else if (literal is StringLiteralExpressionSyntax stringLiteral)
                generator.Emit(OpCodes.LDC_STR, UnEscapeSymbols(stringLiteral.Value));
            else if (literal is BoolLiteralExpressionSyntax boolLiteral)
                generator.Emit(boolLiteral.Value ? OpCodes.LDC_I2_1 : OpCodes.LDC_I2_0);
            else if (literal is NullLiteralExpressionSyntax)
                generator.Emit(OpCodes.LDNULL);
        }

        private static string UnEscapeSymbols(string str)
            => Regex.Unescape(str);

        public static void EmitReturn(this ILGenerator generator, ReturnStatementSyntax statement)
        {
            if (statement is not { Expression: { } })
            {
                generator.Emit(OpCodes.RET);
                return;
            }
            if (statement.Expression is LiteralExpressionSyntax literal)
            {
                generator.EmitLiteral(literal);
                generator.Emit(OpCodes.RET);
                return;
            }

            if (statement.Expression.ExpressionString == "Infinity")
            {
                generator.Emit(OpCodes.LDC_F4, float.PositiveInfinity);
                generator.Emit(OpCodes.RET);
                return;
            }
            if (statement.Expression.ExpressionString == "(-Infinity)")
            {
                generator.Emit(OpCodes.LDC_F4, float.NegativeInfinity);
                generator.Emit(OpCodes.RET);
                return;
            }
            if (statement.Expression.ExpressionString == "NaN")
            {
                generator.Emit(OpCodes.LDC_F4, float.NaN);
                generator.Emit(OpCodes.RET);
                return;
            }

            var @class = generator._methodBuilder.classBuilder;
            var @method = generator._methodBuilder;

            if (statement.Expression is MemberAccessExpression unnamed02)
            {
                var type = generator
                    ._methodBuilder
                    .moduleBuilder
                    .FindType(unnamed02.Start.ExpressionString, @class.Includes);

                var methodName = unnamed02.Chain.First().ExpressionString;

                var call_method = type.FindMethod(methodName);

                generator.Emit(OpCodes.CALL, call_method);
                generator.Emit(OpCodes.RET);
                return;
            }

            generator.EmitExpression(statement.Expression);
            generator.Emit(OpCodes.RET);
        }

        public static void EmitWhileStatement(this ILGenerator gen, WhileStatementSyntax @while)
        {
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            var start = gen.DefineLabel();
            var end = gen.DefineLabel();
            var expType = @while.Expression.DetermineType(ctx);
            gen.UseLabel(start);
            if (expType.FullName == ManaTypeCode.TYPE_BOOLEAN.AsClass().FullName)
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
