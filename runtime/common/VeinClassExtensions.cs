namespace vein;

using System.Collections.Generic;
using System.Linq;
using collections;
using runtime;

public static class VeinClassExtensions
{
    private static IReadOnlyList<VeinArgumentRef> GetArgs(this VeinMethod m, bool includeThis)
        => m.Signature.Arguments.Where(z => includeThis || VeinMethodSignature.NotThis(z)).ToList();
    public static VeinMethod FindMethod(this VeinClass clazz, string rawName, List<VeinComplexType> args, bool includeThis = false)
    {
        var matchingMethods = clazz.Methods.ToList()
            .Where(m => m.RawName.Equals(rawName))
            .ToList();

        if (!matchingMethods.Any()) return null;

        var compatibleMatches = matchingMethods
            .Where(m => m.GetArgs(includeThis).Count == args.Count &&
                        m.GetArgs(includeThis).Zip(args, (methodArg, userArg) => IsCompatible(methodArg.ComplexType, userArg)).All(x => x))
            .ToList();

        if (compatibleMatches.Count == 1)
            return compatibleMatches.First();

        if (compatibleMatches.Count > 1)
            throw new DuplicateItemException("");

        return compatibleMatches.FirstOrDefault();
    }

    public static bool AreArgumentsStrictlyEqual(this VeinComplexType methodArg, VeinComplexType userArg)
    {
        if (methodArg.Equals(userArg))
            return true;

        if (methodArg.IsGeneric && userArg.IsGeneric)
            return methodArg.IsGenericCompatible(userArg);

        return false;
    }

    public static bool IsGenericCompatible(this VeinComplexType methodArg, VeinComplexType userArg)
    {
        if (methodArg.IsGeneric && !userArg.IsGeneric)
            return true; // todo check constraints

        if (userArg.IsGeneric && methodArg.IsGeneric)
            return true;
        return false; // TODO
    }
    public static bool IsCompatible(this VeinComplexType methodArg, VeinComplexType userArg)
    {
        if (methodArg.AreArgumentsStrictlyEqual(userArg))
            return true;

        if (userArg.Class?.TypeCode == VeinTypeCode.TYPE_NULL)
            return true;

        // If the method argument is object, it is compatible with any type
        if (methodArg.Class?.FullName.Name == NameSymbol.Object && methodArg.Class?.FullName.ModuleName == ModuleNameSymbol.Std)
            return true;

        if (methodArg.Class != null && methodArg.Class.FullName.Name == NameSymbol.Object && methodArg.Class.FullName.ModuleName == ModuleNameSymbol.Std)
            return true;

        if (methodArg.IsGeneric)
            return methodArg.IsGenericCompatible(userArg);

        if (methodArg.Class != null && userArg.Class != null && userArg.Class.IsInner(methodArg.Class))
            return true;
        if (methodArg.Class != null && userArg.Class != null && methodArg.Class.TypeCode.HasNumber() && userArg.Class.TypeCode.HasNumber())
            return methodArg.Class.TypeCode.IsCompatibleNumber(userArg.Class.TypeCode);

        if (methodArg.Class?.TypeCode.HasFunction() == true && userArg.Class?.TypeCode.HasFunction() == true)
            return methodArg.Class.CheckCompatibilityFunctionClass(userArg);

        return false;
    }

    public static bool CheckCompatibilityFunctionClass(this VeinClass userArg, VeinClass methodArg)
    {
        var userInvoke = userArg.FindMethod("invoke");
        var methodInvoke = methodArg.FindMethod("invoke");

        if (userInvoke is null || methodInvoke is null)
            return false;

        return userInvoke.Signature.HasCompatibility(methodInvoke.Signature, true);
    }
}
