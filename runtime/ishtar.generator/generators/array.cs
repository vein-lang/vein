namespace ishtar;

using System.Linq;
using System;
using emit;
using vein.extensions;
using vein.runtime;
using vein.syntax;
using vein;

public static class B_Array
{
    public static ILGenerator EmitArrayCreate(this ILGenerator gen, ArrayCreationExpression arr)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var type = context.ResolveType(arr.Type);
        var exp = arr.Initializer;
        var init_method = gen.ConstructArrayTypeInitialization(arr.Type,
                exp);
        var args = exp.Args;
        var sizes = exp.Sizes;

        if (sizes.Length > 1)
            throw new NotSupportedException($"Currently array rank greater 1 not supported.");
        var size = sizes.SingleOrDefault();

        if (init_method is not null) // when method for init array is successful created/resolved
        {
            if (args is not null) foreach (var arg in args.FillArgs)
                    gen.EmitExpression(arg);
            gen.Emit(OpCodes.CALL, init_method);
            return gen;
        }

        gen.Emit(OpCodes.LD_TYPE, type);
        gen.EmitExpression(size); // todo maybe need resolve cast problem
        gen.Emit(OpCodes.NEWARR);


        if (args is not null)
        {
            foreach (var (arg, x) in args.FillArgs.Select((x, y) => (x, y)))
            {
                gen.EmitExpression(arg);
                gen.Emit(OpCodes.STELEM_S, x);
            }
        }

        return gen;
    }


    public static VeinMethod ConstructArrayTypeInitialization(this ILGenerator gen, TypeExpression expression, ArrayInitializerExpression arrayInitializer)
    {
        if (!AppFlags.HasFlag(ApplicationFlag.use_predef_array_type_initer))
            return null;
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

        var args = Enumerable.Range(0, size_value)
                .Select(x => ($"el_{x:00}", type))
                .Select(x => new VeinArgumentRef(x.Item1, x.type))
                .ToArray();

        var method = arrayConstructor.DefineMethod("$init", MethodFlags.Public | MethodFlags.Static,
                VeinTypeCode.TYPE_ARRAY.AsClass(), args);

        var body = method.GetGenerator();

        body.StoreIntoMetadata("context", context);

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
}
