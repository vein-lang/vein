namespace vein.runtime;

using System;
using System.Collections.Generic;

public record VeinBaseConstraint(VeinTypeParameterConstraint Constraint);


public record VeinBaseConstraintConstType(VeinClass classes)
    : VeinBaseConstraint(VeinTypeParameterConstraint.TYPE);

public record VeinBaseConstraintConstBittable()
    : VeinBaseConstraint(VeinTypeParameterConstraint.BITTABLE);

public record VeinBaseConstraintConstClass()
    : VeinBaseConstraint(VeinTypeParameterConstraint.CLASS);

public record VeinBaseConstraintConstSignature(VeinClass @interface)
    : VeinBaseConstraint(VeinTypeParameterConstraint.SIGNATURE);

[Flags]
public enum VeinTypeParameterConstraint : int
{
    TYPE = 1 << 1,      // when T is Type
    BITTABLE = 1 << 2,  // when T is bittable
    SIGNATURE = 1 << 3, // when T is { new(), method(): i32 }
    CLASS = 1 << 4,     // when T is class
}


public record VeinTypeArg(string Name, IReadOnlyList<VeinBaseConstraint> Constraints)
{
    public VeinTypeArg(string Name) : this(Name, new List<VeinBaseConstraint>()) { }

    public override string ToString() => Name;
    public string ToTemplateString() => ToString();

    public VeinDefinedTypeArg AsDefined(VeinClass clazz) => new(clazz, Name, Constraints);
}


public record VeinDefinedTypeArg(VeinClass definedClass, string Name, IReadOnlyList<VeinBaseConstraint> Constraints)
    : VeinTypeArg(Name, Constraints);


public static class GenericConstraintChecker
{
    /// <summary>
    /// Checks a generic definition's type-parameter constraints against the concrete type arguments at a
    /// construction site (e.g. <c>List&lt;i32&gt;</c>), returning one human-readable message per violation
    /// (empty when satisfied). Open (bare type-parameter) arguments are skipped — there is nothing concrete
    /// to validate yet. Constraints are only populated on same-module definitions for now (loaded modules
    /// carry synthetic, constraint-less type parameters), so cross-module enforcement awaits serialization.
    /// Side-effect free, so both the declarative compiler and the IL generator can share it.
    /// </summary>
    public static List<string> CheckViolations(VeinClass definition, IReadOnlyList<VeinComplexType> args)
    {
        var violations = new List<string>();
        if (definition?.TypeArgs is null || definition.TypeArgs.Count == 0 || args is null)
            return violations;

        var count = Math.Min(definition.TypeArgs.Count, args.Count);
        for (var i = 0; i < count; i++)
        {
            var typeParam = definition.TypeArgs[i];
            var arg = args[i];

            if (typeParam.Constraints is null || typeParam.Constraints.Count == 0)
                continue;
            if (arg is null || arg.IsGeneric) // still an open parameter at this site — nothing to validate
                continue;

            var argClass = arg.Class;
            if (argClass is null)
                continue;

            foreach (var constraint in typeParam.Constraints)
            {
                switch (constraint)
                {
                    case VeinBaseConstraintConstBittable when !argClass.IsBittable:
                        violations.Add($"Type argument '{argClass.Name}' does not satisfy the bittable constraint " +
                                       $"on type parameter '{typeParam.Name}' of '{definition.Name}'.");
                        break;
                    case VeinBaseConstraintConstClass when argClass.IsValueType:
                        violations.Add($"Type argument '{argClass.Name}' must be a reference type to satisfy the class constraint " +
                                       $"on type parameter '{typeParam.Name}' of '{definition.Name}'.");
                        break;
                    case VeinBaseConstraintConstType { classes: { } expected }
                        when !argClass.FullName.Equals(expected.FullName) && !argClass.IsInner(expected):
                        violations.Add($"Type argument '{argClass.Name}' must be or derive from '{expected.Name}' " +
                                       $"to satisfy the constraint on type parameter '{typeParam.Name}' of '{definition.Name}'.");
                        break;
                    // SIGNATURE: structural constraints are not enforced yet.
                }
            }
        }

        return violations;
    }
}
