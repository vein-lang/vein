namespace vein.compilation;

using System;
using ishtar.emit;
using System.Collections.Generic;
using System.Linq;
using ishtar;
using MoreLinq;
using vein.runtime;
using vein.syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    public void GenerateCtor((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax member)
            return;
        var (@class, _) = x;
        Context.Document = member.OwnerDocument;
        var doc = member.OwnerDocument;

        if (@class.GetDefaultCtor() is not MethodBuilder ctor)
        {
            Log.Defer.Error($"[red bold]Class/struct '{@class.Name}' has problem with generate default ctor.[/]\n\t" +
                $"{PleaseReportProblemInto()}",
                null, doc);
            return;
        }

        Context.CurrentMethod = ctor;

        var gen = ctor.GetGenerator();

        if (!gen.HasMetadata("context"))
            gen.StoreIntoMetadata("context", Context);

        // emit calling based ctors
        @class.Parents.Select(z => z.GetDefaultCtor()).Where(z => z != null)
            .Pipe(z => gen.EmitThis().Emit(OpCodes.CALL, z))
            .Consume();


        var pregen = new List<(ExpressionSyntax exp, VeinField field)>();


        foreach (var field in @class.Fields)
        {
            if (field.IsStatic)
                continue;
            if (gen.FieldHasAlreadyInited(field))
                continue;
            if (field.IsLiteral)
                continue; // TODO
            var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.ExpressionString.Equals(field.Name));
            if (stx is null && field.IsSpecial)
            {
                pregen.Add((null, field));
                continue;
            }
            if (stx is null)
            {
                Log.Defer.Error($"[red bold]Field '{field.Name}' in class/struct/interface '{@class.Name}' has undefined.[/]", null, doc);
                continue;
            }
            pregen.Add((stx.Field.Expression, field));
        }

        foreach (var (exp, field) in pregen)
        {
            if (!field.Aspects.Any(x => x.Name.Equals("native", StringComparison.InvariantCultureIgnoreCase)))
            {
                if (exp is null)
                    gen.Emit(OpCodes.LDNULL);
                else
                    gen.EmitExpression(exp);
                gen.EmitStageField(field);
            }
        }
        // ctors has return himself
        gen.Emit(OpCodes.LDARG_0).Emit(OpCodes.RET);
    }

    public void GenerateStaticCtor((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax member)
            return;
        var (@class, _) = x;
        var doc = member.OwnerDocument;

        if (@class.GetStaticCtor() is not MethodBuilder ctor)
        {
            Log.Defer.Error($"[red bold]Class/struct '{@class.Name}' has problem with generate static ctor.[/]\n\t" +
                            $"{PleaseReportProblemInto()}",
                null, doc);
            return;
        }
        Context.CurrentMethod = ctor;

        var gen = ctor.GetGenerator();

        gen.StoreIntoMetadata("context", Context);

        var pregen = new List<(ExpressionSyntax exp, VeinField field)>();

        foreach (var field in @class.Fields)
        {
            // skip non-static field,
            // they do not need to be initialized in the static constructor
            if (!field.IsStatic)
                continue;
            if (gen.FieldHasAlreadyInited(field))
                continue;
            var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.ExpressionString.Equals(field.Name));

            if (stx is null && field.IsSpecial)
            {
                pregen.Add((null, field));
                continue;
            }
            if (stx is null)
            {
                Log.Defer.Error($"[red bold]Field '{field.Name}' in class/struct '{@class.Name}' has undefined.[/]", null, doc);
                continue;
            }
            pregen.Add((stx.Field.Expression, field));
        }

        foreach (var (exp, field) in pregen)
        {
            if (exp is null)
                // value_type can also have a NULL value
                gen.Emit(OpCodes.LDNULL);
            else
                gen.EmitExpression(exp);
            gen.Emit(OpCodes.STSF, field);
        }
    }
}
