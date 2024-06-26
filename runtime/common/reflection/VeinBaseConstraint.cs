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
    public override string ToString() => $"µ{Name}";
    public string ToTemplateString() => ToString();
}
