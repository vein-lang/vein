namespace vein.compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using extensions;
using ishtar;
using ishtar.emit;
using runtime;
using syntax;
using vein.reflection;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    // TODO validate scope for aspect usage (method, field, prop, class, assembly)
    public Aspect FindAspect(AspectSyntax syntax, DocumentDeclaration doc)
    {
        var name = $"{syntax.Name}".EndsWith("Aspect") ?
            $"{syntax.Name}" :
            $"{syntax.Name}Aspect";
        var includes = doc.Includes;

        var aspect = this.module.FindType(name, includes, false);

        if (aspect is null)
            return null;

        return new Aspect(aspect.Name, AspectTarget.Class);
    }
    public ClassBuilder CompileAspect(AspectDeclarationSyntax member, DocumentDeclaration doc)
    {
        var name = member.Identifier.ExpressionString.EndsWith("Aspect")
            ? $"{doc.Name}/{member.Identifier.ExpressionString}"
            : $"{doc.Name}/{member.Identifier.ExpressionString}Aspect";

        var clazz = module.DefineClass(name)
            .WithIncludes(doc.Includes);

        clazz.Flags |= ClassFlags.Public; // temporary all aspect has public
        clazz.Flags |= ClassFlags.Aspect; // indicate when it class is a aspect

        clazz.Parents.Clear(); // remove Object ref
        clazz.Parents.Add(Types.Storage.AspectClass);

        var getUsages = clazz.DefineMethod(AspectDeclarationSyntax.GET_USAGES_METHOD_NAME,
            MethodFlags.Override | MethodFlags.Public,
            TYPE_I4.AsClass()(Types.Storage));

        var aspectUsage = member.Aspects.FirstOrDefault(x => x.IsAspectUsage);
        var flags = 0;

        if (aspectUsage is null)
            flags = 1 << 2;
        else
        {
            throw new NotImplementedException("'aspectUsage is not null' is not implemented branch.");
            // TODO
        }

        getUsages
            .GetGenerator()
            .Emit(OpCodes.LDC_I4_S, flags)
            .Emit(OpCodes.RET);

        return clazz;
    }


    public void CompileAspectFor(FieldDeclarationSyntax dec, DocumentDeclaration doc, VeinField field) =>
           CompileAspectFor(dec.Aspects, x =>
               $"aspect{Aspect.ASPECT_METADATA_DIVIDER}{x.Name}{Aspect.ASPECT_METADATA_DIVIDER}class{Aspect.ASPECT_METADATA_DIVIDER}{field.Owner.Name}{Aspect.ASPECT_METADATA_DIVIDER}field{Aspect.ASPECT_METADATA_DIVIDER}{dec.Field.Identifier}.",
               doc, field, AspectTarget.Field);

    public void CompileAspectFor(MethodDeclarationSyntax dec, DocumentDeclaration doc, VeinMethod method) =>
        CompileAspectFor(dec.Aspects, x =>
            $"aspect{Aspect.ASPECT_METADATA_DIVIDER}{x.Name}{Aspect.ASPECT_METADATA_DIVIDER}class{Aspect.ASPECT_METADATA_DIVIDER}{method.Owner.Name}{Aspect.ASPECT_METADATA_DIVIDER}method{Aspect.ASPECT_METADATA_DIVIDER}{method.Name}.",
            doc, method, AspectTarget.Method);

    public void CompileAspectFor(ClassDeclarationSyntax dec, DocumentDeclaration doc, VeinClass clazz) =>
        CompileAspectFor(dec.Aspects, x =>
            $"aspect{Aspect.ASPECT_METADATA_DIVIDER}{x.Name}{Aspect.ASPECT_METADATA_DIVIDER}class{Aspect.ASPECT_METADATA_DIVIDER}{clazz.Name}.", doc, clazz, AspectTarget.Class);

    private void CompileAspectFor(
        List<AspectSyntax> aspects,
        Func<AspectSyntax, string> nameGenerator,
        DocumentDeclaration doc, IAspectable aspectable,
        AspectTarget target)
    {
        foreach (var annotation in aspects.TrimNull().Where(annotation => annotation.Args.Length != 0))
        {
            var aspect = new Aspect(annotation.Name.ToString(), target);
            foreach (var (exp, index) in annotation.Args.Select((x, y) => (x, y)))
            {

                if (exp.CanOptimizationApply())
                {
                    var optimized = exp.ForceOptimization();
                    var converter = optimized.GetTypeCode().GetConverter();
                    var calculated = converter(optimized.ExpressionString);
                    module.WriteToConstStorage($"{nameGenerator(annotation)}_{index}",
                        calculated);
                    aspect.DefineArgument(index, calculated);
                }
                else
                    Log.Defer.Error("[red bold]Aspects require compile-time constant.[/]", annotation, doc);
            }
            aspectable.Aspects.Add(aspect);
        }
    }
}
