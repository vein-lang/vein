namespace vein.reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using vein.runtime;

    public class AspectArgument
    {
        public Aspect Owner { get; }
        public object Value { get; }
        public int Index { get; }

        public AspectArgument(Aspect aspect, object value, int index)
        {
            Owner = aspect;
            Value = value;
            Index = index;
        }
    }

    public class AspectOfClass : Aspect
    {
        public string ClassName { get; }
        public AspectOfClass(string name, string className) : base(name, AspectTarget.Class)
            => ClassName = className;
        public override string ToString() => $"Aspect '{Name}' for '{ClassName}' class";
    }

    public class AspectOfMethod : Aspect
    {
        public string ClassName { get; }
        public string MethodName { get; }
        public AspectOfMethod(string name, string className, string methodName) : base(name, AspectTarget.Method)
        {
            ClassName = className;
            MethodName = methodName;
        }
        public override string ToString() => $"Aspect '{Name}' for '{ClassName}/{MethodName}(..)' method";
    }
    public class AspectOfField : Aspect
    {
        public string ClassName { get; }
        public string FieldName { get; }
        public AspectOfField(string name, string className, string fieldName) : base(name, AspectTarget.Field)
        {
            ClassName = className;
            FieldName = fieldName;
        }
        public override string ToString() => $"Aspect '{Name}' for '{ClassName}/{FieldName}' field";
    }

    public class AliasAspect
    {
        public AliasAspect(Aspect aspect)
        {
            Debug.Assert(aspect.IsAlias());
            Debug.Assert(aspect.Arguments.Count == 1);
            Name = (string)aspect.Arguments.Single().Value;
        }

        public string Name { get; }
    }

    public static class AspectExtensions
    {
        public static AliasAspect AsAlias(this Aspect aspect) => new AliasAspect(aspect);
    }

    public class Aspect
    {
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

        public AliasAspect AsAlias() => new AliasAspect(this);


        public bool IsAlias() => Name.Equals("alias", StringComparison.InvariantCultureIgnoreCase);
        public bool IsNative() => Name.Equals("native", StringComparison.InvariantCultureIgnoreCase);
        public bool IsSpecial() => Name.Equals("special", StringComparison.InvariantCultureIgnoreCase);


        // maybe overhead, need refactoring
        public static Aspect[] Deconstruct(Dictionary<FieldName, object> dictionary)
        {
            AspectTarget getTarget(FieldName name)
            {
                if (name.fullName.Contains("/method/"))
                    return AspectTarget.Method;
                if (name.fullName.Contains("/field/"))
                    return AspectTarget.Field;
                if (name.fullName.Contains("/class/"))
                    return AspectTarget.Class;

                throw new UnknownAspectTargetException(name);
            }

            var aspects = new List<Aspect>();

            // shit
            var groups =
                dictionary.Where(x => x.Key.fullName.StartsWith("aspect/"))
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
                x.Key.fullName.Replace("aspect/", "")
                    .Replace("/class", "")
                    .Split('.').First());
            var groupMethods = methods.GroupBy(x =>
                x.Key.fullName.Replace("aspect/", "")
                    .Replace("/class", "")
                    .Replace("/method", "")
                    .Split('.').First());
            var groupFields = fields.GroupBy(x =>
                x.Key.fullName.Replace("aspect/", "")
                    .Replace("/class", "")
                    .Replace("/field", "")
                    .Split('.').First());

            foreach (var groupClass in groupClasses)
            {
                var aspectName = groupClass.Key.Split('/').First();
                var aspectClass = groupClass.Key.Split('/').Last();
                var aspect = new AspectOfClass(aspectName, aspectClass);
                foreach (var (key, value) in groupClass)
                {
                    var index = key.fullName.Split("._").Last();
                    aspect.DefineArgument(int.Parse(index), value);
                }
                aspects.Add(aspect);
            }
            foreach (var groupMethod in groupMethods)
            {
                var aspectName = groupMethod.Key.Split('/')[0];
                var aspectClass = groupMethod.Key.Split('/')[1];
                var aspectMethod = groupMethod.Key.Split('/')[2];
                var aspect = new AspectOfMethod(aspectName, aspectClass, aspectMethod);
                foreach (var (key, value) in groupMethod)
                {
                    var index = key.fullName.Split("._").Last();
                    aspect.DefineArgument(int.Parse(index), value);
                }
                aspects.Add(aspect);
            }
            foreach (var groupMethod in groupFields)
            {
                var aspectName = groupMethod.Key.Split('/')[0];
                var aspectClass = groupMethod.Key.Split('/')[1];
                var aspectField = groupMethod.Key.Split('/')[2];
                var aspect = new AspectOfField(aspectName, aspectClass, aspectField);
                foreach (var (key, value) in groupMethod)
                {
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
