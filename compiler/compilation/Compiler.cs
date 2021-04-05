namespace insomnia.compilation
{
    using emit;
    using Spectre.Console;
    using stl;
    using syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using extensions;
    using fs;
    using MoreLinq;
    using project;
    using Sprache;
    using wave.etc;
    using static Spectre.Console.AnsiConsole;
    using Console = System.Console;

    public static class Extensions
    {
        public static void Transition<T1, T2>(this (T1 t1, T2 t2) tuple, Action<T1> ft1, Action<T2> ft2)
        {
            ft1(tuple.t1);
            ft2(tuple.t2);
        }
    }

    public class Compiler
    {
        public static Compiler Process(FileInfo[] entity, WaveProject project)
        {
            var c = new Compiler(project);
            
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

        public Compiler(WaveProject project)
        {
            Project = project;
        }

        private WaveProject Project { get; set; }

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
                    result.SourceText = value;
                    // apply root namespace into includes
                    result.Includes.Add($"global::{result.Name}");
                    Ast.Add(key, result);
                }
                catch (WaveParseException e)
                {
                    errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}[/] \n\t" +
                               $"at '[orange bold]{e.Position.Line} line, {e.Position.Column} column[/]' \n\t" +
                               $"in '[orange bold]{key}[/]'.");
                }
            }

            module = new WaveModuleBuilder(Project.Name);

            Ast.Select(x => (x.Key, x.Value))
                .Pipe(x => ctx.WaveStatus($"Linking [grey]'{x.Key.Name}'[/]..."))
                .Select(x => LinkClasses(x.Value))
                .ToList()
                .Pipe(z => z
                    .Pipe(LinkMetadata)
                    .Pipe(x => LinkMethods(x)
                        .Transition(
                            methods => methods.ForEach(GenerateBody),
                            fields => fields.ForEach(GenerateField)))
                    .Pipe(GenerateStaticCtor)
                    .Pipe(GenerateCtor)
                    .Consume())
                .Consume();
            if (errors.Count == 0)
                WriteOutput(module);
        }

        public void WriteOutput(WaveModuleBuilder builder)
        {
            var dirInfo = new DirectoryInfo(Path.Combine(Project.WorkDir, "bin"));

            if (!dirInfo.Exists)
                dirInfo.Create();
            else
                dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).ForEach(x => x.Delete());


            var asm_file = new FileInfo(Path.Combine(dirInfo.FullName, $"{Project.Name}.wll"));
            var wil_file = new FileInfo(Path.Combine(dirInfo.FullName, $"{Project.Name}.wvil.bin"));

            var wil_data = builder.BakeByteArray();


            var asm = new InsomniaAssembly(builder);

            InsomniaAssembly.WriteTo(asm, asm_file.FullName);

            File.WriteAllBytes(wil_file.FullName, wil_data);
        }


        public List<(ClassBuilder clazz, ClassDeclarationSyntax member)> LinkClasses(DocumentDeclaration doc)
        {
            var classes = new List<(ClassBuilder clazz, ClassDeclarationSyntax member)>();
            
            foreach (var member in doc.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    ctx.WaveStatus($"Regeneration class [grey]'{clazz.Identifier}'[/]");
                    clazz.OwnerDocument = doc;
                    classes.Add((CompileClass(clazz, doc), clazz));
                }
                else
                    warnings.Add($"[grey]Member[/] [yellow underline]'{member.GetType().Name}'[/] [grey]is not supported.[/]");
            }

            return classes;
        }

        public ClassBuilder CompileClass(ClassDeclarationSyntax member, DocumentDeclaration doc)
        {
            CompileAnnotation(member, doc);
            if (module.Name.Equals("wcorlib"))
            {
                var result = WaveCore.All.FirstOrDefault(x => x.FullName.Name.Equals(member.Identifier));
               
                if (result is not null)
                {
                    var clz = new ClassBuilder(module, result);
                    module.class_table.Add(clz);

                    clz.Includes.AddRange(doc.Includes);
                    return clz;
                }
            }
            return module.DefineClass($"global::{doc.Name}/{member.Identifier}").WithIncludes(doc.Includes);
        }

        public void CompileAnnotation(MethodDeclarationSyntax method, DocumentDeclaration doc)
        {
            CompileAnnotation(method.Annotations, x =>
                $"{method.OwnerClass.Identifier}_{method.Identifier}.annotation_{x.AnnotationKind}", doc);
        }
        public void CompileAnnotation(ClassDeclarationSyntax clazz, DocumentDeclaration doc)
        {
            CompileAnnotation(clazz.Annotations, x =>
                $"{clazz.Identifier}.annotation_{x.AnnotationKind}", doc);
        }

        private void CompileAnnotation(
            List<AnnotationSyntax> annotations, 
            Func<AnnotationSyntax, string> nameGenerator, 
            DocumentDeclaration doc)
        {
            foreach (var annotation in annotations.Where(annotation => annotation.Args.Length != 0))
            {
                foreach (var (exp, index) in annotation.Args.Select((x, y) => (x, y)))
                {
                    if (exp.CanOptimization())
                    {
                        var optimized = exp.ForceOptimization();
                        var converter = optimized.GetTypeCode().GetConverter();
                        module.WriteToConstStorage($"{nameGenerator(annotation)}_{index}",
                            converter(optimized.ExpressionString));
                    }
                    else
                    {
                        var diff_err = DiffErrorFull(annotation.Transform, doc);
                        errors.Add($"[red bold]Annotations require compile-time constant.[/] \n\t" +
                                   $"at '[orange bold]{annotation.Transform.pos.Line} line, {annotation.Transform.pos.Column} column[/]' \n\t" +
                                   $"in '[orange bold]{doc.FileEntity}[/]'." +
                                   $"{diff_err}");
                    }
                }
            }
        }

        public void LinkMetadata((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            var doc = member.OwnerDocument;
            @class.Flags = GenerateClassFlags(member);
            
            var owner = member.Inheritances.FirstOrDefault();

            // ignore core base types
            if (member.Identifier is "Object" or "ValueType") // TODO
                return;
            
            owner ??= new TypeSyntax("Object"); // TODO set for struct ValueType owner

            @class.Parent = FetchType(owner, doc)?.AsClass();
        }
        public (
            List<(MethodBuilder method, MethodDeclarationSyntax syntax)> methods, 
            List<(WaveField field, FieldDeclarationSyntax syntax)>) 
            LinkMethods((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, clazzSyntax) = x;
            var doc = clazzSyntax.OwnerDocument;
            var methods = new List<(MethodBuilder method, MethodDeclarationSyntax syntax)>();
            var fields = new List<(WaveField field, FieldDeclarationSyntax syntax)>();
            foreach (var member in clazzSyntax.Members)
            {
                switch (member)
                {
                    case IPassiveParseTransition transition when member.IsBrokenToken:
                        var e = transition.Error;
                        var pos = member.Transform.pos;
                        var err_line = DiffErrorFull(member.Transform, doc);
                        errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                                   $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                                   $"in '[orange bold]{doc.FileEntity}[/]'." +
                                   $"{err_line}");
                        break;
                    case MethodDeclarationSyntax method:
                        ctx.WaveStatus($"Regeneration method [grey]'{method.Identifier}'[/]");
                        method.OwnerClass = clazzSyntax;
                        methods.Add(CompileMethod(method, @class, doc));
                        break;
                    case FieldDeclarationSyntax field:
                        ctx.WaveStatus($"Regeneration field [grey]'{field.Field.Identifier}'[/]");
                        field.OwnerClass = clazzSyntax;
                        fields.Add(CompileField(field, @class, doc));
                        break;
                    default:
                        warnings.Add($"[grey]Member[/] '[yellow underline]{member.GetType().Name}[/]' [grey]is not supported.[/]");
                        break;
                }
            }
            return (methods, fields);
        }

        public (MethodBuilder method, MethodDeclarationSyntax syntax) 
            CompileMethod(MethodDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            CompileAnnotation(member, doc);

            var retType = FetchType(member.ReturnType, doc);

            if (retType is null)
                return default;
            
            var args = GenerateArgument(member, doc);
            
            var method = clazz.DefineMethod(member.Identifier, GenerateMethodFlags(member), retType, args);
            
            return (method, member);
        }

        public (WaveField field, FieldDeclarationSyntax member)
            CompileField(FieldDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            var fieldType = FetchType(member.Type, doc);

            if (fieldType is null)
                return default;

            var field = clazz.DefineField(member.Field.Identifier, GenerateFieldFlags(member), fieldType);
            return (field, member);
        }

        public void GenerateCtor((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            ctx.WaveStatus($"Regenerate default ctor for [grey]'{member.Identifier}'[/].");
            var ctor = @class.GetDefaultCtor() as MethodBuilder;
            var doc = member.OwnerDocument;

            if (ctor is null)
            {
                errors.Add($"[red bold]Class/struct '{@class.Name}' has problem with generate default ctor.[/] \n\t" +
                           $"Please report the problem into 'https://github.com/0xF6/wave_lang/issues'." +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
                return;
            }

            var gen = ctor.GetGenerator();


            var pctor = @class.Parent?.GetDefaultCtor();

            if (pctor is not null) // for Object, ValueType
                gen.Emit(OpCodes.CALL, pctor); // call parent ctor
            var pregen = new List<(ExpressionSyntax exp, WaveField field)>();


            foreach (var field in @class.Fields)
            {
                if (field.IsStatic)
                    continue;
                if (field.IsLiteral)
                    continue;
                var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.Equals(field.Name));
                if (stx is null)
                {
                    errors.Add($"[red bold]Field '{field.Name}' in class/struct '{@class.Name}' has undefined.[/] \n\t" +
                               $"in '[orange bold]{doc.FileEntity}[/]'.");
                    continue;
                }
                pregen.Add((stx.Field.Expression, field));
            }

            foreach (var (exp, field) in pregen)
            {
                if (exp is null)
                    gen.Emit(OpCodes.LDNULL);
                else
                    gen.EmitExpression(exp);
                gen.Emit(OpCodes.STF, field);
            }
        }

        public void GenerateStaticCtor((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            ctx.WaveStatus($"Regenerate static ctor for [grey]'{member.Identifier}'[/].");
            var ctor = @class.GetStaticCtor() as MethodBuilder;
            var doc = member.OwnerDocument;

            if (ctor is null)
            {
                errors.Add($"[red bold]Class/struct '{@class.Name}' has problem with generate static ctor.[/] \n\t" +
                           $"Please report the problem into 'https://github.com/0xF6/wave_lang/issues'." +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
                return;
            }

            var gen = ctor.GetGenerator();
            var pregen = new List<(ExpressionSyntax exp, WaveField field)>();

            foreach (var field in @class.Fields)
            {
                if (!field.IsStatic)
                    continue;
                var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.Equals(field.Name));
                if (stx is null)
                {
                    errors.Add($"[red bold]Field '{field.Name}' in class/struct '{@class.Name}' has undefined.[/] \n\t" +
                               $"in '[orange bold]{doc.FileEntity}[/]'.");
                    continue;
                }
                pregen.Add((stx.Field.Expression, field));
            }

            foreach (var (exp, field) in pregen)
            {
                if (exp is null)
                    gen.Emit(OpCodes.LDNULL);
                else
                    gen.EmitExpression(exp);
                gen.Emit(OpCodes.STSF, field);
            }
        }

        public void GenerateBody((MethodBuilder method, MethodDeclarationSyntax member) t)
        {
            if (t == default)
            {
                errors.Add($"[red bold]Unknown error[/] in [italic]GenerateBody(...);[/]");
                return;
            }
            var (method, member) = t;

            foreach (var pr in member.Body.Statements.SelectMany(x => x.ChildNodes.Concat(new []{x})))
                AnalyzeStatement(pr, member);
        }

        private void AnalyzeStatement(BaseSyntax statement, MethodDeclarationSyntax member)
        {
            if (statement is not IPassiveParseTransition {IsBrokenToken: true} transition) 
                return;
            var doc = member.OwnerClass.OwnerDocument;
            var pos = statement.Transform.pos;
            var e = transition.Error;
            var diff_err = DiffErrorFull(statement.Transform, doc);
            errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                       $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                       $"in '[orange bold]{doc.FileEntity}[/]'."+
                       $"{diff_err}");
        }

        private static string DiffErrorFull(Transform t, DocumentDeclaration doc)
        {
            try
            {
                var (diff, arrow_line) = DiffError(t, doc);
                return $"\n\t[grey] {diff.EscapeMarkup().EscapeArgumentSymbols()} [/]\n\t[red] {arrow_line.EscapeMarkup().EscapeArgumentSymbols()} [/]";
            }
            catch
            {
                return ""; // TODO analytic
            }
        }
        private static (string line, string arrow_line) DiffError(Transform t, DocumentDeclaration doc)
        {
            var line = doc.SourceLines[t.pos.Line].Length < t.len ? 
                t.pos.Line - 1 : 
                /*t.pos.Line*/throw new Exception("cannot detect line");

            var original = doc.SourceLines[line];
            var err_line = original[(t.pos.Column - 1)..];
            var space1 = original[..(t.pos.Column - 1)];
            var space2 = (t.pos.Column - 1) + t.len > original.Length ? "" : original[((t.pos.Column - 1) + t.len)..];

            return (original,
                $"{new string(' ', space1.Length)}{new string('^', err_line.Length)}{new string(' ', space2.Length)}");
        }
        public void GenerateField((WaveField field, FieldDeclarationSyntax member) t)
        {
            if (t == default)
            {
                errors.Add($"[red bold]Unknown error[/] in [italic]GenerateBody(...);[/]");
                return;
            }
            

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
                        var diff_err = DiffErrorFull(literal.Transform, doc);
                        
                        var value = numeric.GetTypeCode();
                        var variable = member.Type.Identifier;
                        var variable1 = field.FieldType.TypeCode;

                        errors.Add(
                            $"[red bold]Cannot implicitly convert type[/] " +
                            $"'[purple underline]{numeric.GetTypeCode().AsType().Name}[/]' to " +
                            $"'[purple underline]{field.FieldType.Name}[/]'.\n\t" +
                            $"at '[orange bold]{numeric.Transform.pos.Line} line, {numeric.Transform.pos.Column} column[/]' \n\t" +
                            $"in '[orange bold]{doc.FileEntity}[/]'."+
                            $"{diff_err}");
                    }
                }
                else if (literal.GetTypeCode() != field.FieldType.TypeCode)
                {
                    var diff_err = DiffErrorFull(literal.Transform, doc);
                    errors.Add(
                        $"[red bold]Cannot implicitly convert type[/] " +
                        $"'[purple underline]{literal.GetTypeCode().AsType().Name}[/]' to " +
                        $"'[purple underline]{member.Type.Identifier}[/]'.\n\t" +
                        $"at '[orange bold]{literal.Transform.pos.Line} line, {literal.Transform.pos.Column} column[/]' \n\t" +
                        $"in '[orange bold]{doc.FileEntity}[/]'."+
                        $"{diff_err}");
                }
            }

            var clazz = field.Owner;
            

            if (member.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Const))
            {
                var assigner = member.Field.Expression;

                if (assigner is NewExpressionSyntax)
                {
                    var diff_err = DiffErrorFull(assigner.Transform, doc);
                    errors.Add(
                        $"[red bold]The expression being assigned to[/] '[purple underline]{member.Field.Identifier}[/]' [red bold]must be constant[/]. \n\t" +
                        $"at '[orange bold]{assigner.Transform.pos.Line} line, {assigner.Transform.pos.Column} column[/]' \n\t" +
                        $"in '[orange bold]{doc.FileEntity}[/]'."+
                        $"{diff_err}");
                    return;
                }

                var converter = field.GetConverter();

                if (assigner is UnaryExpressionSyntax { OperatorType: ExpressionType.Negate } negate)
                    module.WriteToConstStorage(field.FullName, converter(negate.ExpressionString.Trim('(', ')'))); // shit
                else
                    module.WriteToConstStorage(field.FullName, converter(assigner.ExpressionString));

            }
            
        }
        private WaveType FetchType(TypeSyntax typename, DocumentDeclaration doc)
        {
            var retType = module.TryFindType(typename.Identifier, doc.Includes);
            
            if (retType is null) 
                errors.Add($"[red bold]Cannot resolve type[/] '[purple underline]{typename.Identifier}[/]' \n\t" +
                           $"at '[orange bold]{typename.Transform.pos.Line} line, {typename.Transform.pos.Column} column[/]' \n\t" +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
            return retType;
        }
        
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
                        flags |= ClassFlags.Special;
                        continue;
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
                        flags |= ClassFlags.Public;
                        continue;
                    case "private":
                        flags |= ClassFlags.Private;
                        continue;
                    case "static":
                        flags |= ClassFlags.Static;
                        continue;
                    case "internal":
                        flags |= ClassFlags.Internal;
                        continue;
                    case "abstract":
                        flags |= ClassFlags.Abstract;
                        continue;
                    default:
                        errors.Add(
                            $"In [orange]'{clazz.Identifier}'[/] class/struct/interface [red bold]{mod}[/] is not supported [orange bold]modificator[/].");
                        continue;
                }
            }

            return flags;
        }

        private FieldFlags GenerateFieldFlags(FieldDeclarationSyntax field)
        {
            var flags = (FieldFlags)0;

            var annotation = field.Annotations;
            var mods = field.Modifiers;

            foreach (var kind in annotation.Select(x => x.AnnotationKind))
            {
                switch (kind)
                {
                    case WaveAnnotationKind.Virtual:
                        flags |= FieldFlags.Virtual;
                        continue;
                    case WaveAnnotationKind.Special:
                        flags |= FieldFlags.Special;
                        continue;
                    case WaveAnnotationKind.Native:
                        continue;
                    case WaveAnnotationKind.Readonly:
                        flags |= FieldFlags.Readonly;
                        continue;
                    case WaveAnnotationKind.Getter:
                    case WaveAnnotationKind.Setter:
                        //errors.Add($"In [orange]'{field.Field.Identifier}'[/] field [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                    default:
                        errors.Add(
                            $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind.ToString().ToLower())
                {
                    case "public":
                        flags |= FieldFlags.Public;
                        continue;
                    case "static":
                        flags |= FieldFlags.Static;
                        continue;
                    case "protected":
                        flags |= FieldFlags.Protected;
                        continue;
                    case "internal":
                        flags |= FieldFlags.Internal;
                        continue;
                    case "override":
                        flags |= FieldFlags.Override;
                        continue;
                    case "abstract":
                        flags |= FieldFlags.Abstract;
                        continue;
                    case "const":
                        flags |= FieldFlags.Literal;
                        continue;
                    default:
                        errors.Add($"In [orange]'{field.Field.Identifier}'[/] field [red bold]{mod}[/] is not supported [orange bold]modificator[/].");
                        continue;
                }
            }


            //if (flags.HasFlag(FieldFlags.Private) && flags.HasFlag(MethodFlags.Public))
            //    errors.Add($"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] in [orange]'{field.Field.Identifier}'[/] field.");


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
                        flags |= MethodFlags.Virtual;
                        continue;
                    case WaveAnnotationKind.Special:
                        flags |= MethodFlags.Special;
                        continue;
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
                        flags |= MethodFlags.Public;
                        continue;
                    case "extern":
                        flags |= MethodFlags.Extern;
                        continue;
                    case "private":
                        flags |= MethodFlags.Private;
                        continue;
                    case "static":
                        flags |= MethodFlags.Static;
                        continue;
                    case "protected":
                        flags |= MethodFlags.Protected;
                        continue;
                    case "internal":
                        flags |= MethodFlags.Internal;
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
            Thread.Sleep(100); // so, i need it :(
            context.Status = status;
            return context;
        }
    }
}