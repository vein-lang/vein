namespace vein.reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using runtime;

    public class AspectArgument(Aspect aspect, object value, int index)
    {
        public Aspect Owner { get; } = aspect;
        public object Value { get; } = value;
        public int Index { get; } = index;
    }

    public class AspectOfClass(string name, NameSymbol className) : Aspect(name, AspectTarget.Class)
    {
        public NameSymbol ClassName { get; } = className;

        public override string ToString() => $"Aspect '{Name}' for '{ClassName}' class";
    }

    public class AspectOfMethod(string name, NameSymbol className, string methodName)
        : Aspect(name, AspectTarget.Method)
    {
        public NameSymbol ClassName { get; } = className;
        public string MethodName { get; } = methodName;

        public override string ToString() => $"Aspect '{Name}' for '{ClassName}/{MethodName}(..)' method";
    }
    public class AspectOfField(string name, NameSymbol className, string fieldName) : Aspect(name, AspectTarget.Field)
    {
        public NameSymbol ClassName { get; } = className;
        public string FieldName { get; } = fieldName;

        public override string ToString() => $"Aspect '{Name}' for '{ClassName}/{FieldName}' field";
    }

    public class AliasAspect
    {
        public AliasAspect(Aspect aspect)
        {
            Debug.Assert(aspect.Arguments.Count == 1);
            Name = (string)aspect.Arguments.Single().Value;
        }

        public string Name { get; }
    }

    public static class AspectExtensions
    {
        public static AliasAspect AsAlias(this Aspect aspect) => new(aspect);
    }

    public class Aspect
    {
        public const string ASPECT_METADATA_DIVIDER = "#";

        public string Name { get; }
        public AspectTarget Target { get; }

        public List<AspectArgument> Arguments = new ();
        public Aspect(string name, AspectTarget target)
        {
            Name = name;
            Target = target;
        }

        internal void DefineArgument(int index, object value)
            => Arguments.Add(new AspectArgument(this, value, index));

        public AliasAspect AsAlias() => new(this);


        public bool IsNative() => Name.Equals("native", StringComparison.InvariantCultureIgnoreCase);
        public bool IsSpecial() => Name.Equals("special", StringComparison.InvariantCultureIgnoreCase);


        // maybe overhead, need refactoring
        public static Aspect[] Deconstruct(Dictionary<FieldName, object> dictionary)
        {
            static AspectTarget getTarget(FieldName name)
            {
                if (name.fullName.Contains($"{ASPECT_METADATA_DIVIDER}method{ASPECT_METADATA_DIVIDER}"))
                    return AspectTarget.Method;
                if (name.fullName.Contains($"{ASPECT_METADATA_DIVIDER}field{ASPECT_METADATA_DIVIDER}"))
                    return AspectTarget.Field;
                if (name.fullName.Contains($"{ASPECT_METADATA_DIVIDER}class{ASPECT_METADATA_DIVIDER}"))
                    return AspectTarget.Class;

                throw new UnknownAspectTargetException(name);
            }

            var aspects = new List<Aspect>();

            // shit
            var groups =
                dictionary.Where(x => x.Key.fullName.StartsWith($"aspect{ASPECT_METADATA_DIVIDER}"))
                    .Select(x => (getTarget(x.Key), x))
                .GroupBy(x => x.Item1)
                .ToArray();

            var fields = new Dictionary<FieldName, object>();
            var methods = new Dictionary<FieldName, object>();
            var classes = new Dictionary<FieldName, object>();

            foreach (var tuple in groups)
            {
                foreach (var (aspectTarget, (key, value)) in tuple)
                {
                    switch (aspectTarget)
                    {
                        case AspectTarget.Class:
                            classes.Add(key, value);
                            break;
                        case AspectTarget.Field:
                            fields.Add(key, value);
                            break;
                        case AspectTarget.Method:
                            methods.Add(key, value);
                            break;
                        default:
                            break;
                    }
                }
            }


            // rly shit
            var groupClasses = classes.GroupBy(x =>
                x.Key.fullName.Replace($"aspect{ASPECT_METADATA_DIVIDER}", "")
                    .Replace($"{ASPECT_METADATA_DIVIDER}class", "")
                    .Split('.').First());
            var groupMethods = methods.GroupBy(x =>
                x.Key.fullName.Replace($"aspect{ASPECT_METADATA_DIVIDER}", "")
                    .Replace($"{ASPECT_METADATA_DIVIDER}class", "")
                    .Replace($"{ASPECT_METADATA_DIVIDER}method", "")
                    .Split('.').First());
            var groupFields = fields.GroupBy(x =>
                x.Key.fullName.Replace($"aspect{ASPECT_METADATA_DIVIDER}", "")
                    .Replace($"{ASPECT_METADATA_DIVIDER}class", "")
                    .Replace($"{ASPECT_METADATA_DIVIDER}field", "")
                    .Split('.').First());

            foreach (var groupClass in groupClasses)
            {
                var aspectName = groupClass.Key.Split(ASPECT_METADATA_DIVIDER).First();
                var aspectClass = groupClass.Key.Split(ASPECT_METADATA_DIVIDER).Last();
                var aspect = new AspectOfClass(aspectName, new NameSymbol(aspectClass));
                foreach (var (key, value) in groupClass)
                {
                    if (key.fullName.EndsWith("@"))
                        continue;
                    var index = key.fullName.Split("._").Last();
                    aspect.DefineArgument(int.Parse(index), value);
                }
                aspects.Add(aspect);
            }
            foreach (var groupMethod in groupMethods)
            {
                var aspectName = groupMethod.Key.Split(ASPECT_METADATA_DIVIDER)[0];
                var aspectClass = groupMethod.Key.Split(ASPECT_METADATA_DIVIDER)[1];
                var aspectMethod = groupMethod.Key.Split(ASPECT_METADATA_DIVIDER)[2];
                var aspect = new AspectOfMethod(aspectName, new NameSymbol(aspectClass), aspectMethod);
                foreach (var (key, value) in groupMethod)
                {
                    if (key.fullName.EndsWith("@"))
                        continue;
                    var index = key.fullName.Split("._").Last();
                    aspect.DefineArgument(int.Parse(index), value);
                }
                aspects.Add(aspect);
            }
            foreach (var groupMethod in groupFields)
            {
                var aspectName = groupMethod.Key.Split(ASPECT_METADATA_DIVIDER)[0];
                var aspectClass = groupMethod.Key.Split(ASPECT_METADATA_DIVIDER)[1];
                var aspectField = groupMethod.Key.Split(ASPECT_METADATA_DIVIDER)[2];
                var aspect = new AspectOfField(aspectName, new NameSymbol(aspectClass), aspectField);
                foreach (var (key, value) in groupMethod)
                {
                    if (key.fullName.EndsWith("@"))
                        continue;
                    var index = key.fullName.Split("._").Last();
                    aspect.DefineArgument(int.Parse(index), value);
                }
                aspects.Add(aspect);
            }

            return aspects.ToArray();
        }
    }

    public class UnknownAspectTargetException : Exception
    {
        public UnknownAspectTargetException(string name)
            : base($"Unknown target: '{name}'") { }
        public UnknownAspectTargetException(string name, string reason)
            : base($"Unknown target: '{name}', reason: '{reason}'") { }
    }

    public enum AspectTarget
    {
        Class,
        Method,
        Field
    }
}
