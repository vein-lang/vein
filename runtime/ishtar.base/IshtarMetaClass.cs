using System;
using System.Diagnostics.CodeAnalysis;
using vein.runtime;

[ExcludeFromCodeCoverage]
public sealed class IshtarMetaClass : VeinClass
{
    public class CannotUseMetaClassInRuntime : Exception { }

    private IshtarMetaClass(QualityTypeName name) => this.FullName = name;

    public sealed override VeinMethod GetDefaultCtor() => throw new CannotUseMetaClassInRuntime();
    public sealed override VeinMethod GetDefaultDtor() => throw new CannotUseMetaClassInRuntime();
    public sealed override VeinMethod GetStaticCtor() => throw new CannotUseMetaClassInRuntime();
    protected sealed override VeinMethod GetOrCreateTor(string name, bool isStatic = false)
        => throw new CannotUseMetaClassInRuntime();

    public sealed override ClassFlags Flags => throw new CannotUseMetaClassInRuntime();

    public static IshtarMetaClass Define(string space, string name)
        => new IshtarMetaClass(new QualityTypeName("std", name, space));
    public static IshtarMetaClass Define(QualityTypeName q)
        => new IshtarMetaClass(q);
}
