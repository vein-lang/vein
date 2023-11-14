namespace vein.compilation;

using System.Linq;
using System.Linq.Expressions;
using ishtar;
using ishtar.emit;
using runtime;
using syntax;
using vein.exceptions;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    private FieldFlags GenerateFieldFlags(MemberDeclarationSyntax member)
    {
        var flags = (FieldFlags)0;

        var annotations = member.Aspects;
        var mods = member.Modifiers;

        foreach (var annotation in annotations)
        {
            switch (annotation)
            {
                //case VeinAnnotationKind.Virtual:
                //    flags |= FieldFlags.Virtual;
                //    continue;
                case { IsSpecial: true }:
                    flags |= FieldFlags.Special;
                    continue;
                case { IsNative: true }:
                    flags |= FieldFlags.Special;
                    continue;
                //case VeinAnnotationKind.Readonly:
                //    flags |= FieldFlags.Readonly;
                //    continue;
                //case VeinAnnotationKind.Getter:
                //case VeinAnnotationKind.Setter:
                //    //errors.Add($"In [orange]'{field.Field.Identifier}'[/] field [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                //    continue;
                default:
                    var a = FindAspect(annotation, member.OwnerDocument);
                    if (a is not null)
                        continue;

                    if (member is FieldDeclarationSyntax field)
                    {
                        Log.Defer.Error(
                            $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{annotation.Name}[/] " +
                            $"is not found [orange bold]aspect[/].",
                            annotation, field.OwnerClass.OwnerDocument);
                    }

                    if (member is PropertyDeclarationSyntax prop)
                    {
                        Log.Defer.Error(
                            $"In [orange]'{prop.Identifier}'[/] property [red bold]{annotation.Name}[/] " +
                            $"is not found [orange bold]aspect[/].",
                            annotation, prop.OwnerClass.OwnerDocument);
                    }
                    continue;
            }
        }

        foreach (var mod in mods)
        {
            switch (mod.ModificatorKind)
            {
                case ModificatorKind.Private:
                    continue;
                case ModificatorKind.Public:
                    flags |= FieldFlags.Public;
                    continue;
                case ModificatorKind.Static:
                    flags |= FieldFlags.Static;
                    continue;
                case ModificatorKind.Protected:
                    flags |= FieldFlags.Protected;
                    continue;
                case ModificatorKind.Internal:
                    flags |= FieldFlags.Internal;
                    continue;
                case ModificatorKind.Override:
                    flags |= FieldFlags.Override;
                    continue;
                case ModificatorKind.Const when member is PropertyDeclarationSyntax:
                    goto default;
                case ModificatorKind.Const:
                    flags |= FieldFlags.Literal;
                    continue;
                case ModificatorKind.Readonly:
                    flags |= FieldFlags.Readonly;
                    continue;
                case ModificatorKind.Abstract:
                    flags |= FieldFlags.Abstract;
                    continue;
                case ModificatorKind.Virtual:
                    flags |= FieldFlags.Virtual;
                    continue;
                default:
                    switch (member)
                    {
                        case FieldDeclarationSyntax field:
                            Log.Defer.Error(
                                $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{mod.ModificatorKind}[/] " +
                                $"is not supported [orange bold]modificator[/].",
                                mod, field.OwnerClass.OwnerDocument);
                            break;
                        case PropertyDeclarationSyntax prop:
                            Log.Defer.Error(
                                $"In [orange]'{prop.Identifier}'[/] property [red bold]{mod.ModificatorKind}[/] " +
                                $"is not supported [orange bold]modificator[/].",
                                mod, prop.OwnerClass.OwnerDocument);
                            break;
                    }
                    continue;
            }
        }


        //if (flags.HasFlag(FieldFlags.Private) && flags.HasFlag(MethodFlags.Public))
        //    errors.Add($"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] in [orange]'{field.Field.Identifier}'[/] field.");


        return flags;
    }

    public (VeinField field, FieldDeclarationSyntax member)
        CompileField(FieldDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
    {
        var fieldType = member.Type.IsSelf ?
            clazz :
            FetchType(member.Type, doc);

        if (fieldType is null)
            return default;
        var name = member.Field.Identifier.ExpressionString;
        var @override = member.Aspects.FirstOrDefault(x => x.IsNative);

        if (@override is not null && @override.Args.Any())
        {
            var exp = @override.Args[0];
            if (exp is ArgumentExpression { Value: StringLiteralExpressionSyntax value })
                name = value.Value;
            else
            {
                Log.Defer.Error($"Invalid argument expression", exp);
                throw new SkipStatementException();
            }
        }

        var field = clazz.DefineField(name, GenerateFieldFlags(member), fieldType);

        CompileAspectFor(member, doc, field);
        return (field, member);
    }

    public void GenerateField((VeinField field, FieldDeclarationSyntax member) t)
    {
        if (t == default)
            return;


        var (field, member) = t;
        var doc = member.OwnerClass.OwnerDocument;

        // skip uninited fields
        if (member.Field.Expression is null)
            return;

        // validate type compatible
        if (member.Field.Expression is LiteralExpressionSyntax literal)
        {
            if (literal is NumericLiteralExpressionSyntax numeric)
            {
                if (!field.FieldType.TypeCode.CanImplicitlyCast(numeric))
                {
                    var diff_err = literal.Transform.DiffErrorFull(doc);

                    //var value = numeric.GetTypeCode();
                    //var variable = member.Type.Identifier;
                    //var variable1 = field.FieldType.TypeCode;

                    Log.errors.Enqueue(
                        $"[red bold]Cannot implicitly convert type[/] " +
                        $"'[purple underline]{numeric.GetTypeCode().AsClass()(Types.Storage).Name}[/]' to " +
                        $"'[purple underline]{field.FieldType.Name}[/]'.\n\t" +
                        $"at '[orange bold]{numeric.Transform.pos.Line} line, {numeric.Transform.pos.Column} column[/]' \n\t" +
                        $"in '[orange bold]{doc.FileEntity}[/]'." +
                        $"{diff_err}");
                }
            }
            else if (literal.GetTypeCode() != field.FieldType.TypeCode)
            {
                var diff_err = literal.Transform.DiffErrorFull(doc);
                Log.errors.Enqueue(
                    $"[red bold]Cannot implicitly convert type[/] " +
                    $"'[purple underline]{literal.GetTypeCode().AsClass()(Types.Storage).Name}[/]' to " +
                    $"'[purple underline]{member.Type.Identifier}[/]'.\n\t" +
                    $"at '[orange bold]{literal.Transform.pos.Line} line, {literal.Transform.pos.Column} column[/]' \n\t" +
                    $"in '[orange bold]{doc.FileEntity}[/]'." +
                    $"{diff_err}");
            }
        }

        if (member.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Const))
        {
            var assigner = member.Field.Expression;

            if (assigner is NewExpressionSyntax)
            {
                var diff_err = assigner.Transform.DiffErrorFull(doc);
                Log.errors.Enqueue(
                    $"[red bold]The expression being assigned to[/] '[purple underline]{member.Field.Identifier}[/]' [red bold]must be constant[/]. \n\t" +
                    $"at '[orange bold]{assigner.Transform.pos.Line} line, {assigner.Transform.pos.Column} column[/]' \n\t" +
                    $"in '[orange bold]{doc.FileEntity}[/]'." +
                    $"{diff_err}");
                return;
            }

            try
            {
                var converter = field.GetConverter();

                if (assigner is UnaryExpressionSyntax { OperatorType: ExpressionType.Negate } negate)
                    module.WriteToConstStorage(field.FullName, converter($"-{negate.ExpressionString.Trim('(', ')')}")); // shit
                else
                    module.WriteToConstStorage(field.FullName, converter(assigner.ExpressionString));
            }
            catch (ValueWasIncorrectException e)
            {
                throw new MaybeMismatchTypeException(field, e);
            }
        }
    }
}
