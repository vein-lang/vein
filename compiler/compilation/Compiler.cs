namespace wave.compilation
{
    using emit;
    using Spectre.Console;
    using stl;
    using syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using static Spectre.Console.AnsiConsole;
    using Console = System.Console;

    public static class MarkupExtensions
    {
        public static string EscapeArgumentSymbols(this string str) 
            => str.Replace("{", "{{").Replace("}", "}}");
    }

    public class Compiler
    {
        public static Compiler Process(FileInfo[] entity)
        {
            var c = new Compiler();
            
            return Status()
                .Spinner(Spinner.Known.Dots8Bit)
                .Start("Processing...", ctx =>
                {
                    c.ctx = ctx;
                    try
                    {
                        c.ProcessFiles(entity);
                    }
                    catch (Exception e)
                    {
                        MarkupLine("failed compilation.");
                        WriteException(e);
                    }
                    return c;
                });
        }

        private readonly WaveSyntax syntax = new();
        private StatusContext ctx;
        private readonly Dictionary<FileInfo, string> Sources = new ();
        private readonly Dictionary<FileInfo, DocumentDeclaration> Ast = new();
        public readonly List<string> warnings = new ();
        public readonly List<string> errors = new ();
        private WaveModuleBuilder module;

        private void ProcessFiles(FileInfo[] files)
        {
            foreach (var file in files)
            {
                ctx.WaveStatus($"Read [grey]'{file.Name}'[/]...");
                Sources.Add(file, File.ReadAllText(file.FullName));
            }

            foreach (var (key, value) in Sources)
            {
                ctx.WaveStatus($"Compile [grey]'{key.Name}'[/]...");
                try
                {
                    var result = syntax.CompilationUnit.ParseWave(value);
                    result.FileEntity = key;
                    Ast.Add(key, result);
                }
                catch (WaveParseException e)
                {
                    errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}[/] \n\t" +
                               $"at '[orange bold]{e.Position.Line} line, {e.Position.Column} column[/]' \n\t" +
                               $"in '[orange bold]{key}[/]'.");
                }
            }

            module = new WaveModuleBuilder("wcorlib");
            
            foreach (var (key, value) in Ast)
            {
                ctx.WaveStatus($"Linking [grey]'{key.Name}'[/]...");
                CompileInto(value);
            }
        }
        public void CompileInto(DocumentDeclaration doc)
        {
            var classes = new List<(WaveClass clazz, ClassDeclarationSyntax member)>();

            // apply root namespace into includes
            doc.Includes.Add(doc.Name);

            foreach (var member in doc.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    ctx.WaveStatus($"Regeneration class [grey]'{clazz.Identifier}'[/]");
                    classes.Add((CompileInto(clazz, doc), clazz));
                }
                else
                    warnings.Add($"[grey]Member[/] [yellow underline]'{member.GetType().Name}'[/] [grey]is not supported.[/]");
            }

            foreach (var (clazz, member) in classes)
            {
                CompileMethods(member, clazz, doc);
            }
        }

        public WaveClass CompileInto(ClassDeclarationSyntax member, DocumentDeclaration doc)
        {
            var clazz = module.DefineClass($"global::{doc.Name}/{member.Identifier}");

            var owner = member.Inheritance.FirstOrDefault();

            if (member.Identifier != "Object")
            {
                // TODO
                owner ??= new TypeSyntax("Object");

                clazz.Parent = FetchType(owner, doc)?.AsClass();
            }

            clazz.Flags = GenerateClassFlags(member);

            return clazz;
        }

        public void CompileMethods(ClassDeclarationSyntax clazzSyntax, WaveClass clazz, DocumentDeclaration doc)
        {
            foreach (var member in clazzSyntax.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        ctx.WaveStatus($"Regeneration method [grey]'{method.Identifier}'[/]");
                        CompileInto(method, clazz, doc);
                        break;
                    case IPassiveParseTransition transition when member.IsBrokenToken:
                        var e = transition.Error;
                        var pos = member.Transform.pos;
                        errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                                   $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                                   $"in '[orange bold]{doc.FileEntity}[/]'.");
                        break;
                    default:
                        warnings.Add($"[grey]Member[/] '[yellow underline]{member.GetType().Name}[/]' [grey]is not supported.[/]");
                        break;
                }

                
            }
        }

        public void CompileInto(MethodDeclarationSyntax member, WaveClass clazz, DocumentDeclaration doc)
        {
            var retType = FetchType(member.ReturnType, doc);

            if (retType is null)
                return;

            var args = GenerateArgument(member, doc);
            
            var method = clazz.DefineMethod(member.Identifier, retType, GenerateMethodFlags(member), args);
        }


        private WaveType FetchType(TypeSyntax typename, DocumentDeclaration doc)
        {
            ctx.WaveStatus("Collecting metadata...");
            var retType = module.TryFindType(typename.Identifier, doc.Includes);

            if (HasNativeType(typename) && retType is null)
            {
                errors.Add($"[red bold]Cannot resolve type[/] '[purple underline]{typename.Identifier}[/]' \n\t" +
                           $"[red bold]Native type is not loaded.[/]\n\t" +
                           $"at '[orange bold]{typename.Transform.pos.Line} line, {typename.Transform.pos.Column} column[/]' \n\t" +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
                return null;
            }

            if (retType is null) 
                errors.Add($"[red bold]Cannot resolve type[/] '[purple underline]{typename.Identifier}[/]' \n\t" +
                           $"at '[orange bold]{typename.Transform.pos.Line} line, {typename.Transform.pos.Column} column[/]' \n\t" +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
            return retType;
        }

        private bool HasNativeType(TypeSyntax typename) 
            => WaveCore.All.Any(x => x.Name.Equals(typename.Identifier, StringComparison.InvariantCultureIgnoreCase));

        private WaveArgumentRef[] GenerateArgument(MethodDeclarationSyntax method, DocumentDeclaration doc)
        {
            if (method.Parameters.Count == 0)
                return Array.Empty<WaveArgumentRef>();
            return method.Parameters.Select(parameter => new WaveArgumentRef
                {Type = FetchType(parameter.Type, doc), Name = parameter.Identifier})
                .ToArray();
        }

        private ClassFlags GenerateClassFlags(ClassDeclarationSyntax clazz)
        {
            var flags = (ClassFlags) 0;

            var annotation = clazz.Annotations;
            var mods = clazz.Modifiers;

            foreach (var kind in annotation.Select(s => s.AnnotationKind))
            {
                switch (kind)
                {
                    case WaveAnnotationKind.Getter:
                    case WaveAnnotationKind.Setter:
                    case WaveAnnotationKind.Virtual:
                        errors.Add($"Cannot apply [orange bold]annotation[/] [red bold]{kind}[/] to [orange]'{clazz.Identifier}'[/] " +
                                   $"class/struct/interface declaration.");
                        continue;
                    case WaveAnnotationKind.Special:
                    case WaveAnnotationKind.Native:
                        continue;
                    case WaveAnnotationKind.Readonly when !clazz.IsStruct:
                        errors.Add($"[orange bold]Annotation[/] [red bold]{kind}[/] can only be applied to a structure declaration.");
                        continue;
                    case WaveAnnotationKind.Readonly when clazz.IsStruct:
                        // TODO
                        continue;
                    default:
                        errors.Add(
                            $"In [orange]'{clazz.Identifier}'[/] class/struct/interface [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind.ToString().ToLower())
                {
                    case "public":
                        flags &= ClassFlags.Public;
                        continue;
                    case "private":
                        flags &= ClassFlags.Private;
                        continue;
                    case "static":
                        flags &= ClassFlags.Static;
                        continue;
                    case "protected":
                        flags &= ClassFlags.Protected;
                        continue;
                    case "internal":
                        flags &= ClassFlags.Internal;
                        continue;
                    case "abstract":
                        flags &= ClassFlags.Abstract;
                        continue;
                    default:
                        errors.Add(
                            $"In [orange]'{clazz.Identifier}'[/] class/struct/interface [red bold]{mod}[/] is not supported [orange bold]modificator[/].");
                        continue;
                }
            }

            return flags;
        }

        private MethodFlags GenerateMethodFlags(MethodDeclarationSyntax method)
        {
            var flags = (MethodFlags)0;

            var annotation = method.Annotations;
            var mods = method.Modifiers;

            foreach (var kind in annotation.Select(x => x.AnnotationKind))
            {
                switch (kind)
                {
                    case WaveAnnotationKind.Virtual:
                        flags &= MethodFlags.Virtual;
                        continue;
                    case WaveAnnotationKind.Special:
                    case WaveAnnotationKind.Native:
                        continue;
                    case WaveAnnotationKind.Readonly:
                    case WaveAnnotationKind.Getter:
                    case WaveAnnotationKind.Setter:
                        errors.Add($"In [orange]'{method.Identifier}'[/] method [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                    default:
                        errors.Add(
                            $"In [orange]'{method.Identifier}'[/] method [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind.ToString().ToLower())
                {
                    case "public":
                        flags &= MethodFlags.Public;
                        continue;
                    case "extern":
                        flags &= MethodFlags.Extern;
                        continue;
                    case "private":
                        flags &= MethodFlags.Private;
                        continue;
                    case "static":
                        flags &= MethodFlags.Static;
                        continue;
                    case "protected":
                        flags &= MethodFlags.Protected;
                        continue;
                    case "internal":
                        flags &= MethodFlags.Internal;
                        continue;
                    default:
                        errors.Add($"In [orange]'{method.Identifier}'[/] method [red bold]{mod}[/] is not supported [orange bold]modificator[/].");
                        continue;
                }
            }


            if (flags.HasFlag(MethodFlags.Private) && flags.HasFlag(MethodFlags.Public))
                errors.Add($"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] in [orange]'{method.Identifier}'[/] method.");


            return flags;
        }
    }

    public static class WaveStatusContextEx
    {
        /// <summary>Sets the status message.</summary>
        /// <param name="context">The status context.</param>
        /// <param name="status">The status message.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static StatusContext WaveStatus(this StatusContext context, string status)
        {
            if (context == null)
                throw new ArgumentNullException(nameof (context));
            Thread.Sleep(200); // so, i need it :(
            context.Status = status;
            return context;
        }
    }
}