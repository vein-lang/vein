namespace vein.compilation
{
    using MoreLinq;
    using Spectre.Console;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading;
    using ishtar;
    using vein;
    using cmd;
    using exceptions;
    using ishtar.emit;
    using extensions;
    using pipes;
    using project;
    using runtime;
    using stl;
    using syntax;
    using static runtime.VeinTypeCode;
    using static Spectre.Console.AnsiConsole;
    using reflection;

    public class Compiler
    {

        public static Compiler Process(FileInfo[] entity, VeinProject project, CompileSettings flags)
        {
            var c = new Compiler(project, flags);

            return Status()
                .Spinner(Spinner.Known.Dots8Bit)
                .Start("Processing...", ctx =>
                {
                    c.Status = ctx;
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

        public Compiler(VeinProject project, CompileSettings flags)
        {
            _flags = flags;
            Project = project;
            var pack = project.SDK.GetPackByAlias(project.Runtime);
            resolver = new(this);
            resolver.AddSearchPath(new(project.WorkDir));
            resolver.AddSearchPath(new(project.SDK.GetFullPath(pack).FullName));
        }

        internal VeinProject Project { get; set; }

        internal readonly CompileSettings _flags;
        internal readonly VeinSyntax syntax = new();
        internal readonly AssemblyResolver resolver;
        internal readonly Dictionary<FileInfo, string> Sources = new ();
        internal readonly Dictionary<FileInfo, DocumentDeclaration> Ast = new();
        internal StatusContext Status;
        public readonly List<string> warnings = new ();
        public readonly List<string> errors = new ();
        public readonly List<string> infos = new ();
        internal VeinModuleBuilder module;
        internal GeneratorContext Context;

        private void ProcessFiles(FileInfo[] files)
        {
            if (_flags.IsNeedDebuggerAttach)
            {
                while (!Debugger.IsAttached)
                {
                    Status.ManaStatus($"[green]Waiting debugger[/]...");
                    Thread.Sleep(400);
                }
            }
            var deps = new List<VeinModule>();
            foreach (var (name, version) in Project.Packages)
            {
                Status.ManaStatus($"Resolve [grey]'{name}, {version}'[/]...");
                deps.Add(resolver.ResolveDep(name, version.Version, deps));
            }
            foreach (var file in files)
            {
                Status.ManaStatus($"Read [grey]'{file.Name}'[/]...");
                var text = File.ReadAllText(file.FullName);
                if (text.StartsWith("#ignore"))
                    continue;
                Sources.Add(file, text);
            }

            foreach (var (key, value) in Sources)
            {
                Status.ManaStatus($"Compile [grey]'{key.Name}'[/]...");
                try
                {
                    var result = syntax.CompilationUnit.ParseVein(value);
                    result.FileEntity = key;
                    result.SourceText = value;
                    // apply root namespace into includes
                    result.Includes.Add($"global::{result.Name}");
                    Ast.Add(key, result);
                }
                catch (VeinParseException e)
                {
                    errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}[/] \n\t" +
                               $"at '[orange bold]{e.Position.Line} line, {e.Position.Column} column[/]' \n\t" +
                               $"in '[orange bold]{key}[/]'.");
                }
            }

            Context = new GeneratorContext();

            module = new VeinModuleBuilder(Project.Name);

            Context.Module = module;
            Context.Module.Deps.AddRange(deps);

            Ast.Select(x => (x.Key, x.Value))
                .Pipe(x => Status.ManaStatus($"Linking [grey]'{x.Key.Name}'[/]..."))
                .SelectMany(x => LinkClasses(x.Value))
                .ToList()
                .Pipe(LinkMetadata)
                .Select(x => (LinkMethods(x), x))
                .ToList()
                .Pipe(x => x.Item1.Transition(
                    methods => methods.ForEach(GenerateBody),
                    fields => fields.ForEach(GenerateField),
                    props => props.ForEach(GenerateProp)))
                .Select(x => x.x)
                .Pipe(GenerateCtor)
                .Pipe(GenerateStaticCtor)
                .Pipe(ValidateInheritance)
                .Consume();
            errors.AddRange(Context.Errors);
            if (errors.Count == 0)
                PipelineRunner.Run(this);
            MarkupLine($"[blue]INF[/]: Result assembly [orange]'{module.Name}, {module.Version}'[/].");
            if (_flags.PrintResultType)
            {
                var table = new Table();
                table.AddColumn(new TableColumn("Type").Centered());
                table.Border(TableBorder.Rounded);
                foreach (var @class in module.class_table)
                    table.AddRow(new Markup($"[blue]{@class.FullName.NameWithNS}[/]"));
                Render(table);
            }
        }

        public List<(ClassBuilder clazz, ClassDeclarationSyntax member)> LinkClasses(DocumentDeclaration doc)
        {
            var classes = new List<(ClassBuilder clazz, ClassDeclarationSyntax member)>();

            foreach (var member in doc.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    Status.ManaStatus($"Regeneration class [grey]'{clazz.Identifier}'[/]");
                    clazz.OwnerDocument = doc;
                    var result = CompileClass(clazz, doc);
                    Context.Classes.Add(result.FullName, result);
                    classes.Add((result, clazz));
                }
                else
                    warnings.Add($"[grey]Member[/] [yellow underline]'{member.GetType().Name}'[/] [grey]is not supported.[/]");
            }

            return classes;
        }

        public ClassBuilder CompileClass(ClassDeclarationSyntax member, DocumentDeclaration doc)
        {

            if (member.IsForwardedType)
            {
                var result = VeinCore.All.
                    FirstOrDefault(x => x.FullName.Name.Equals(member.Identifier.ExpressionString));

                if (result is not null)
                {
                    var clz = new ClassBuilder(module, result);
                    module.class_table.Add(clz);

                    clz.Includes.AddRange(doc.Includes);

                    CompileAnnotation(member, doc, clz);
                    return clz;
                }

                throw new ForwardedTypeNotDefinedException(member.Identifier.ExpressionString);
            }

            var clazz = module.DefineClass($"global::{doc.Name}/{member.Identifier.ExpressionString}")
                .WithIncludes(doc.Includes);
            CompileAnnotation(member, doc, clazz);
            return clazz;
        }
        public void CompileAnnotation(FieldDeclarationSyntax field, DocumentDeclaration doc, IAspectable aspectable) =>
            CompileAnnotation(field.Annotations, x =>
                $"aspect/{x.AnnotationKind}/class/{field.OwnerClass.Identifier}/field/{field.Field.Identifier}.",
                doc, aspectable, AspectTarget.Field);

        public void CompileAnnotation(MethodDeclarationSyntax method, DocumentDeclaration doc, IAspectable aspectable) =>
            CompileAnnotation(method.Annotations, x =>
                $"aspect/{x.AnnotationKind}/class/{method.OwnerClass.Identifier}/method/{method.Identifier}.",
                doc, aspectable, AspectTarget.Method);

        public void CompileAnnotation(ClassDeclarationSyntax clazz, DocumentDeclaration doc, IAspectable aspectable) =>
            CompileAnnotation(clazz.Annotations, x =>
                $"aspect/{x.AnnotationKind}/class/{clazz.Identifier}.", doc, aspectable, AspectTarget.Class);

        private void CompileAnnotation(
            List<AnnotationSyntax> annotations,
            Func<AnnotationSyntax, string> nameGenerator,
            DocumentDeclaration doc, IAspectable aspectable,
            AspectTarget target)
        {
            foreach (var annotation in annotations.TrimNull().Where(annotation => annotation.Args.Length != 0))
            {
                var aspect = new Aspect(annotation.AnnotationKind.ToString(), target);
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
                    {
                        var diff_err = annotation.Transform.DiffErrorFull(doc);
                        errors.Add($"[red bold]Annotations require compile-time constant.[/] \n\t" +
                                   $"at '[orange bold]{annotation.Transform.pos.Line} line, {annotation.Transform.pos.Column} column[/]' \n\t" +
                                   $"in '[orange bold]{doc.FileEntity}[/]'." +
                                   $"{diff_err}");
                    }
                }
                aspectable.Aspects.Add(aspect);
            }
        }

        public void LinkMetadata((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            var doc = member.OwnerDocument;
            @class.Flags = GenerateClassFlags(member);

            if (member.IsInterface)
            {
                @class.Flags |= ClassFlags.Abstract;
                @class.Flags |= ClassFlags.Interface;
                @class.Flags |= ClassFlags.Public;
            }

            var owners = member.Inheritances;

            // ignore core base types
            if (member.Identifier.ExpressionString is "Object" or "ValueType") // TODO
                return;
            if (!owners.Any() && !@class.Parents.Any())
            {
                // fallback transform
                if (member.IsStruct)
                {
                    owners.Add(new TypeSyntax(new IdentifierExpression("ValueType"))
                        .SetPos<TypeSyntax>(member.Identifier.Transform));
                }
                else
                {
                    owners.Add(new TypeSyntax(new IdentifierExpression("Object"))
                        .SetPos<TypeSyntax>(member.Identifier.Transform));
                }
            }

            owners
                .Select(owner => FetchType(owner, doc))
                .Where(p => !@class.Parents.Contains(p))
                .Pipe(p => @class.Parents.Add(p)).Consume();
        }

        public void ValidateInheritance((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            ValidateInheritanceInterfaces(@class, member);
            ValidateCollisionsMethods(@class, member);
        }

        public void ValidateInheritanceInterfaces(ClassBuilder @class, ClassDeclarationSyntax member)
        {
            var prepairedAbstracts = @class.Parents
                .SelectMany(x => x.Methods)
                .Where(x => !x.IsPrivate)
                .Where(x => !x.IsStatic)
                .Where(x => x.IsAbstract);

            foreach (var method in prepairedAbstracts.Where(x => !@class.ContainsImpl(x)))
            {
                PrintError(
                    $"[red]'{@class.Name}'[/] does not implement inherited abstract member [red]'{method.Owner.Name}.{method.Name}'[/]"
                    , member.Identifier, member.OwnerDocument);
            }
        }
        public void ValidateCollisionsMethods(ClassBuilder @class, ClassDeclarationSyntax member)
        {
            var prepairedOthers = @class.Parents
                .SelectMany(x => x.Methods)
                .Where(x => !x.IsPrivate)
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsSpecial);

            foreach (var method in prepairedOthers.Where(@class.Contains))
            {
                PrintWarning(
                    $"[yellow]'{method.Name}' hides inherited member '{method.Name}'.[/]",
                    member.Identifier, member.OwnerDocument);
            }
        }

        public (
            List<(MethodBuilder method, MethodDeclarationSyntax syntax)> methods,
            List<(VeinField field, FieldDeclarationSyntax syntax)> fields,
            List<(VeinProperty field, PropertyDeclarationSyntax syntax)> props)
            LinkMethods((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, clazzSyntax) = x;
            var doc = clazzSyntax.OwnerDocument;
            var methods = new List<(MethodBuilder method, MethodDeclarationSyntax syntax)>();
            var fields = new List<(VeinField field, FieldDeclarationSyntax syntax)>();
            var props = new List<(VeinProperty field, PropertyDeclarationSyntax syntax)>();
            foreach (var member in clazzSyntax.Members)
            {
                switch (member)
                {
                    case IPassiveParseTransition transition when member.IsBrokenToken:
                        var e = transition.Error;
                        var pos = member.Transform.pos;
                        var err_line = member.Transform.DiffErrorFull(doc);
                        errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                                   $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                                   $"in '[orange bold]{doc.FileEntity}[/]'." +
                                   $"{err_line}");
                        break;
                    case MethodDeclarationSyntax method:
                        Status.ManaStatus($"Regeneration method [grey]'{method.Identifier}'[/]");
                        method.OwnerClass = clazzSyntax;
                        methods.Add(CompileMethod(method, @class, doc));
                        break;
                    case FieldDeclarationSyntax field:
                        Status.ManaStatus($"Regeneration field [grey]'{field.Field.Identifier}'[/]");
                        field.OwnerClass = clazzSyntax;
                        fields.Add(CompileField(field, @class, doc));
                        break;
                    case PropertyDeclarationSyntax prop:
                        Status.ManaStatus($"Regeneration property [grey]'{prop.Identifier}'[/]");
                        prop.OwnerClass = clazzSyntax;
                        props.Add(CompileProperty(prop, @class, doc));
                        break;
                    default:
                        warnings.Add($"[grey]Member[/] '[yellow underline]{member.GetType().Name}[/]' [grey]is not supported.[/]");
                        break;
                }
            }
            return (methods, fields, props);
        }

        public (MethodBuilder method, MethodDeclarationSyntax syntax)
            CompileMethod(MethodDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            var retType = FetchType(member.ReturnType, doc);

            if (retType is null)
                return default;

            var args = GenerateArgument(member, doc);

            var method = clazz.DefineMethod(member.Identifier.ExpressionString, GenerateMethodFlags(member), retType, args);

            method.Owner = clazz;

            if (clazz.IsInterface)
            {
                method.Flags |= MethodFlags.Abstract;
                method.Flags |= MethodFlags.Public;
            }

            CompileAnnotation(member, doc, method);

            return (method, member);
        }

        public (VeinField field, FieldDeclarationSyntax member)
            CompileField(FieldDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {
            var fieldType = FetchType(member.Type, doc);

            if (fieldType is null)
                return default;

            var field = clazz.DefineField(member.Field.Identifier.ExpressionString, GenerateFieldFlags(member), fieldType);

            CompileAnnotation(member, doc, field);
            return (field, member);
        }

        public (VeinProperty prop, PropertyDeclarationSyntax member)
            CompileProperty(PropertyDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
        {

            var propType = FetchType(member.Type, doc);

            if (propType is null)
            {
                PrintError($"[red bold]Unknown type detected. Can't resolve [italic]{member.Type.Identifier}[/][/] \n\t" +
                           PleaseReportProblemInto(),
                    member.Identifier, doc);
                return default;
            }

            if (member is { Setter: { IsEmpty: true }, Getter: { IsEmpty: true } })
                return (clazz.DefineAutoProperty(member.Identifier, GenerateFieldFlags(member), propType), member);
            return (clazz.DefineEmptyProperty(member.Identifier, GenerateFieldFlags(member), propType), member);
        }


        private static string PleaseReportProblemInto()
            => $"Please report the problem into 'https://github.com/vein-lang/vein/issues'.";

        public void GenerateCtor((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            Context.Document = member.OwnerDocument;
            Status.ManaStatus($"Regenerate default ctor for [grey]'{member.Identifier}'[/].");
            var doc = member.OwnerDocument;

            if (@class.GetDefaultCtor() is not MethodBuilder ctor)
            {
                errors.Add($"[red bold]Class/struct '{@class.Name}' has problem with generate default ctor.[/] \n\t" +
                           PleaseReportProblemInto() +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
                return;
            }

            Context.CurrentMethod = ctor;

            var gen = ctor.GetGenerator();

            gen.StoreIntoMetadata("context", Context);

            // emit calling based ctors
            @class.Parents.Select(z => z.GetDefaultCtor()).Where(z => z != null)
                .Pipe(z => gen.Emit(OpCodes.CALL, z))
                .Consume();


            var pregen = new List<(ExpressionSyntax exp, VeinField field)>();


            foreach (var field in @class.Fields)
            {
                if (field.IsStatic)
                    continue;
                if (field.IsLiteral)
                    continue;
                if (field.IsSpecial)
                    continue;
                var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.ExpressionString.Equals(field.Name));
                if (stx is null)
                {
                    errors.Add($"[red bold]Field '{field.Name}' in class/struct/interface '{@class.Name}' has undefined.[/] \n\t" +
                               $"in '[orange bold]{doc.FileEntity}[/]'.");
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
                    gen.Emit(OpCodes.STF, field);
                }
            }
        }

        public void GenerateStaticCtor((ClassBuilder @class, ClassDeclarationSyntax member) x)
        {
            var (@class, member) = x;
            Status.ManaStatus($"Regenerate static ctor for [grey]'{member.Identifier}'[/].");
            var ctor = @class.GetStaticCtor() as MethodBuilder;
            var doc = member.OwnerDocument;

            if (ctor is null)
            {
                errors.Add($"[red bold]Class/struct '{@class.Name}' has problem with generate static ctor.[/] \n\t" +
                           $"Please report the problem into 'https://github.com/vein-lang/vein/issues'." +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
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
                var stx = member.Fields
                    .SingleOrDefault(x => x.Field.Identifier.ExpressionString.Equals(field.Name));
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
                    // value_type can also have a NULL value
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

            GenerateBody(method, member.Body, member.OwnerClass.OwnerDocument);
        }

        private void GenerateBody(MethodBuilder method, BlockSyntax block, DocumentDeclaration doc)
        {
            foreach (var pr in block.Statements.SelectMany(x => x.ChildNodes.Concat(new[] { x })))
                AnalyzeStatement(pr, doc);

            if (method.IsAbstract)
                return;

            var generator = method.GetGenerator();
            Context.Document = doc;
            Context.CurrentMethod = method;
            Context.CreateScope();
            generator.StoreIntoMetadata("context", Context);

            foreach (var statement in block.Statements)
            {
                try
                {
                    generator.EmitStatement(statement);
                }
                catch (Exception e)
                {
                    PrintError($"[red bold]{e.Message.EscapeMarkup()}[/] in [italic]EmitStatement(...);[/]", statement, Context.Document);
                }
            }
            // fucking shit fucking
            // VM needs the end-of-method notation, which is RETURN.
            // but in case of the VOID method, user may not write it
            // and i didnt think of anything smarter than checking last OpCode
            if (!generator._opcodes.Any() && method.ReturnType.TypeCode == TYPE_VOID)
                generator.Emit(OpCodes.RET);
            if (generator._opcodes.Any() && generator._opcodes.Last() != OpCodes.RET.Value && method.ReturnType.TypeCode == TYPE_VOID)
                generator.Emit(OpCodes.RET);
        }

        private void AnalyzeStatement(BaseSyntax statement, DocumentDeclaration doc)
        {
            if (statement is not IPassiveParseTransition { IsBrokenToken: true } transition)
                return;
            var pos = statement.Transform.pos;
            var e = transition.Error;
            var diff_err = statement.Transform.DiffErrorFull(doc);
            errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                       $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                       $"in '[orange bold]{doc.FileEntity}[/]'." +
                       $"{diff_err}");
        }

        public void GenerateProp((VeinProperty prop, PropertyDeclarationSyntax member) t)
        {
            if (t == default)
            {
                errors.Add($"[red bold]Unknown error[/] in [italic]GenerateBody(...);[/]");
                return;
            }

            var (prop, member) = t;
            var doc = member.OwnerClass.OwnerDocument;

            if (prop.Owner is not ClassBuilder clazz)
            {
                errors.Add($"[red bold]Unknown error[/] in [italic]GenerateProp(...);[/]");
                return;
            }

            if (prop.Setter is not null || prop.Getter is not null)
                return; // skip auto property (already generated).

            if (member.Setter is not null)
            {
                prop.Setter = clazz.DefineMethod($"set_{prop.Name}",
                    VeinProperty.ConvertShadowFlags(prop.Flags), prop.PropType,
                    new VeinArgumentRef("value", prop.PropType));

                GenerateBody((MethodBuilder)prop.Setter, member.Setter.Body, doc);
            }

            if (member.Getter is not null)
            {
                prop.Getter = clazz.DefineMethod($"get_{prop.Name}",
                    VeinProperty.ConvertShadowFlags(prop.Flags), prop.PropType);

                GenerateBody((MethodBuilder)prop.Getter, member.Getter.Body, doc);
            }
        }

        public void GenerateField((VeinField field, FieldDeclarationSyntax member) t)
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
                        var diff_err = literal.Transform.DiffErrorFull(doc);

                        var value = numeric.GetTypeCode();
                        var variable = member.Type.Identifier;
                        var variable1 = field.FieldType.TypeCode;

                        errors.Add(
                            $"[red bold]Cannot implicitly convert type[/] " +
                            $"'[purple underline]{numeric.GetTypeCode().AsClass().Name}[/]' to " +
                            $"'[purple underline]{field.FieldType.Name}[/]'.\n\t" +
                            $"at '[orange bold]{numeric.Transform.pos.Line} line, {numeric.Transform.pos.Column} column[/]' \n\t" +
                            $"in '[orange bold]{doc.FileEntity}[/]'." +
                            $"{diff_err}");
                    }
                }
                else if (literal.GetTypeCode() != field.FieldType.TypeCode)
                {
                    var diff_err = literal.Transform.DiffErrorFull(doc);
                    errors.Add(
                        $"[red bold]Cannot implicitly convert type[/] " +
                        $"'[purple underline]{literal.GetTypeCode().AsClass().Name}[/]' to " +
                        $"'[purple underline]{member.Type.Identifier}[/]'.\n\t" +
                        $"at '[orange bold]{literal.Transform.pos.Line} line, {literal.Transform.pos.Column} column[/]' \n\t" +
                        $"in '[orange bold]{doc.FileEntity}[/]'." +
                        $"{diff_err}");
                }
            }

            var clazz = field.Owner;

            if (member.Modifiers.Any(x => x.ModificatorKind == ModificatorKind.Const))
            {
                var assigner = member.Field.Expression;

                if (assigner is NewExpressionSyntax)
                {
                    var diff_err = assigner.Transform.DiffErrorFull(doc);
                    errors.Add(
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

        private VeinClass FetchType(TypeSyntax typename, DocumentDeclaration doc)
        {
            var retType = module.TryFindType(typename.Identifier.ExpressionString, doc.Includes);

            if (retType is null)
                PrintError($"[red bold]Cannot resolve type[/] '[purple underline]{typename.Identifier}[/]'", typename, doc);
            return retType;
        }

        private VeinArgumentRef[] GenerateArgument(MethodDeclarationSyntax method, DocumentDeclaration doc)
        {
            if (method.Parameters.Count == 0)
                return Array.Empty<VeinArgumentRef>();
            return method.Parameters.Select(parameter => new VeinArgumentRef
            { Type = FetchType(parameter.Type, doc), Name = parameter.Identifier.ExpressionString })
                .ToArray();
        }
        private ClassFlags GenerateClassFlags(ClassDeclarationSyntax clazz)
        {
            var flags = (ClassFlags) 0;

            var annotations = clazz.Annotations;
            var mods = clazz.Modifiers;

            foreach (var annotation in annotations)
            {
                var kind = annotation.AnnotationKind;
                switch (kind)
                {
                    case VeinAnnotationKind.Getter:
                    case VeinAnnotationKind.Setter:
                    case VeinAnnotationKind.Virtual:
                        PrintError(
                            $"Cannot apply [orange bold]annotation[/] [red bold]{kind}[/] to [orange]'{clazz.Identifier}'[/] " +
                            $"class/struct/interface declaration.",
                            clazz.Identifier, clazz.OwnerDocument);
                        continue;
                    case VeinAnnotationKind.Special:
                        flags |= ClassFlags.Special;
                        continue;
                    case VeinAnnotationKind.Native:
                        continue;
                    case VeinAnnotationKind.Readonly when !clazz.IsStruct:
                        PrintError(
                            $"[orange bold]Annotation[/] [red bold]{kind}[/] can only be applied to a structure declaration.",
                            clazz.Identifier, clazz.OwnerDocument);
                        continue;
                    case VeinAnnotationKind.Readonly when clazz.IsStruct:
                        // TODO
                        continue;
                    case VeinAnnotationKind.Forwarded:
                        continue;
                    default:
                        PrintError(
                            $"In [orange]'{clazz.Identifier}'[/] class/struct/interface [red bold]{kind}[/] " +
                            $"is not supported [orange bold]annotation[/].",
                            clazz.Identifier, clazz.OwnerDocument);
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
                    case "extern":
                        continue;
                    default:
                        PrintError($"In [orange]'{clazz.Identifier}'[/] class/struct/interface " +
                                   $"[red bold]{mod}[/] is not supported [orange bold]modificator[/].",
                            clazz.Identifier, clazz.OwnerDocument);

                        continue;
                }
            }

            return flags;
        }

        private FieldFlags GenerateFieldFlags(MemberDeclarationSyntax member)
        {
            var flags = (FieldFlags)0;

            var annotations = member.Annotations;
            var mods = member.Modifiers;
            
            foreach (var annotation in annotations)
            {
                switch (annotation.AnnotationKind)
                {
                    case VeinAnnotationKind.Virtual:
                        flags |= FieldFlags.Virtual;
                        continue;
                    case VeinAnnotationKind.Special:
                        flags |= FieldFlags.Special;
                        continue;
                    case VeinAnnotationKind.Native:
                        flags |= FieldFlags.Special;
                        continue;
                    case VeinAnnotationKind.Readonly:
                        flags |= FieldFlags.Readonly;
                        continue;
                    case VeinAnnotationKind.Getter:
                    case VeinAnnotationKind.Setter:
                        //errors.Add($"In [orange]'{field.Field.Identifier}'[/] field [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                    default:
                        if (member is FieldDeclarationSyntax field)
                        {
                            PrintError(
                                $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{annotation.AnnotationKind}[/] " +
                                $"is not supported [orange bold]annotation[/].",
                                annotation, field.OwnerClass.OwnerDocument);
                        }

                        if (member is PropertyDeclarationSyntax prop)
                        {
                            PrintError(
                                $"In [orange]'{prop.Identifier}'[/] property [red bold]{annotation.AnnotationKind}[/] " +
                                $"is not supported [orange bold]annotation[/].",
                                annotation, prop.OwnerClass.OwnerDocument);
                        }
                        continue;
                }
            }

            foreach (var mod in mods)
            {
                switch (mod.ModificatorKind.ToString().ToLower())
                {
                    case "private":
                        continue;
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
                    case "const" when member is PropertyDeclarationSyntax:
                        goto default;
                    case "const":
                        flags |= FieldFlags.Literal;
                        continue;
                    default:
                        switch (member)
                        {
                            case FieldDeclarationSyntax field:
                                PrintError(
                                    $"In [orange]'{field.Field.Identifier}'[/] field [red bold]{mod.ModificatorKind}[/] " +
                                    $"is not supported [orange bold]modificator[/].",
                                    mod, field.OwnerClass.OwnerDocument);
                                break;
                            case PropertyDeclarationSyntax prop:
                                PrintError(
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

        private void _print(string text, BaseSyntax posed, DocumentDeclaration doc, List<string> qlist)
        {
            if (posed is { Transform: null })
            {
                errors.Add($"INTERNAL ERROR: TOKEN '{posed.GetType().Name}' HAS INCORRECT TRANSFORM POSITION.");
                return;
            }

            var strBuilder = new StringBuilder();

            strBuilder.Append($"{text.EscapeArgumentSymbols()}\n");



            if (posed is not null)
            {
                strBuilder.Append(
                    $"\tat '[orange bold]{posed.Transform.pos.Line} line, {posed.Transform.pos.Column} column[/]' \n");
            }

            if (doc is not null)
            {
                strBuilder.Append(
                    $"\tin '[orange bold]{doc.FileEntity}[/]'.");
            }

            if (posed is not null && doc is not null)
            {
                var diff_err = posed.Transform.DiffErrorFull(doc);
                strBuilder.Append($"\t\t{diff_err}");
            }

            qlist.Add(strBuilder.ToString());
        }
        internal void PrintWarning(string text) => _print(text, null, null, warnings);
        internal void PrintWarning(string text, BaseSyntax posed) => _print(text, posed, null, warnings);
        internal void PrintWarning(string text, BaseSyntax posed, DocumentDeclaration doc)
            => _print(text, posed, doc, warnings);

        internal void PrintError(string text) => _print(text, null, null, errors);
        internal void PrintError(string text, BaseSyntax posed) => _print(text, posed, null, errors);
        internal void PrintError(string text, BaseSyntax posed, DocumentDeclaration doc)
            => _print(text, posed, doc, errors);

        internal void PrintInfo(string text) => _print(text, null, null, infos);
        internal void PrintInfo(string text, BaseSyntax posed) => _print(text, posed, null, infos);
        internal void PrintInfo(string text, BaseSyntax posed, DocumentDeclaration doc)
            => _print(text, posed, doc, infos);


        private MethodFlags GenerateMethodFlags(MethodDeclarationSyntax method)
        {
            var flags = (MethodFlags)0;

            var annotations = method.Annotations;
            var mods = method.Modifiers;

            foreach (var annotation in annotations)
            {
                switch (annotation.AnnotationKind)
                {
                    case VeinAnnotationKind.Virtual:
                        flags |= MethodFlags.Virtual;
                        continue;
                    case VeinAnnotationKind.Special:
                        flags |= MethodFlags.Special;
                        continue;
                    case VeinAnnotationKind.Native:
                        continue;
                    case VeinAnnotationKind.Readonly:
                    case VeinAnnotationKind.Getter:
                    case VeinAnnotationKind.Setter:
                        PrintError(
                            $"In [orange]'{method.Identifier}'[/] method [red bold]{annotation.AnnotationKind}[/] " +
                            $"is not supported [orange bold]annotation[/].",
                            method.Identifier, method.OwnerClass.OwnerDocument);
                        continue;
                    default:
                        PrintError(
                            $"In [orange]'{method.Identifier}'[/] method [red bold]{annotation.AnnotationKind}[/] " +
                            $"is not supported [orange bold]annotation[/].",
                            method.Identifier, method.OwnerClass.OwnerDocument);
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
                    case "override":
                        flags |= MethodFlags.Override;
                        continue;
                    default:
                        PrintError(
                            $"In [orange]'{method.Identifier}'[/] method [red bold]{mod}[/] " +
                            $"is not supported [orange bold]modificator[/].",
                            method.Identifier, method.OwnerClass.OwnerDocument);
                        continue;
                }
            }


            if (flags.HasFlag(MethodFlags.Private) && flags.HasFlag(MethodFlags.Public))
                PrintError(
                    $"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] " +
                    $"in [orange]'{method.Identifier}'[/] method.",
                    method.ReturnType, method.OwnerClass.OwnerDocument);


            return flags;
        }
    }

    public static class ManaStatusContextEx
    {
        /// <summary>Sets the status message.</summary>
        /// <param name="context">The status context.</param>
        /// <param name="status">The status message.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static StatusContext ManaStatus(this StatusContext context, string status)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            Thread.Sleep(10); // so, i need it :(
            context.Status = status;
            return context;
        }
    }
}
