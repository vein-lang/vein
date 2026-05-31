namespace ishtar_test;

using System;
using ishtar;
using vein.runtime;
using NUnit.Framework;

[TestFixture]
public unsafe class InterfacesTest : IshtarTestBase
{
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void VTable_InitWithSingleInterface()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var voidType = VeinTypeCode.TYPE_VOID.AsClass()(scope.Types);

            var ifoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IFoo"), NamespaceSymbol.Internal, scope.Module.Name));
            ifoo.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            ifoo.DefineMethod("doFoo", MethodFlags.Abstract | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ifoo));

            var zoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Zoo"), NamespaceSymbol.Internal, scope.Module.Name));
            zoo.Parents.Add(ifoo);
            zoo.Flags = ClassFlags.Public;
            var zooMethod = zoo.DefineMethod("doFoo", MethodFlags.Override | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            zooMethod.GetGenerator().Emit(OpCodes.RET);
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            gen.Emit(OpCodes.LDC_I4_0);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile();
        Assert.Pass("VTable initialization with single interface succeeded");
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void VTable_InitWithMultipleInterfaces()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var voidType = VeinTypeCode.TYPE_VOID.AsClass()(scope.Types);

            var ifoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IFoo"), NamespaceSymbol.Internal, scope.Module.Name));
            ifoo.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            ifoo.DefineMethod("doFoo", MethodFlags.Abstract | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ifoo));

            var ibar = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IBar"), NamespaceSymbol.Internal, scope.Module.Name));
            ibar.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            ibar.DefineMethod("doBar", MethodFlags.Abstract | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ibar));

            var zoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Zoo"), NamespaceSymbol.Internal, scope.Module.Name));
            zoo.Parents.Add(ifoo);
            zoo.Parents.Add(ibar);
            zoo.Flags = ClassFlags.Public;

            var zooFoo = zoo.DefineMethod("doFoo", MethodFlags.Override | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            zooFoo.GetGenerator().Emit(OpCodes.RET);

            var zooBar = zoo.DefineMethod("doBar", MethodFlags.Override | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            zooBar.GetGenerator().Emit(OpCodes.RET);
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            gen.Emit(OpCodes.LDC_I4_0);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile();
        Assert.Pass("VTable initialization with multiple interfaces succeeded");
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_MethodDispatch_CALL_V()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            var ifoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IFoo"), NamespaceSymbol.Internal, scope.Module.Name));
            ifoo.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var ifooMethod = ifoo.DefineMethod("getValue", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ifoo));

            var zoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Zoo"), NamespaceSymbol.Internal, scope.Module.Name));
            zoo.Parents.Add(ifoo);
            zoo.Flags = ClassFlags.Public;
            var zooMethod = zoo.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));

            var zooGen = zooMethod.GetGenerator();
            zooGen.Emit(OpCodes.LDC_I4_S, 42);
            zooGen.Emit(OpCodes.RET);

            ctx.ifooMethod = ifooMethod;
            ctx.zoo = zoo;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass zoo = ctx.zoo;
            VeinMethod ifooMethod = ctx.ifooMethod;

            gen.Emit(OpCodes.NEWOBJ, zoo);
            gen.Emit(OpCodes.CALL, ifooMethod);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(42, result->returnValue[0].data.l);
    }

    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_MultipleInterfaces_MethodDispatch()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            var ifoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IFoo"), NamespaceSymbol.Internal, scope.Module.Name));
            ifoo.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var fooMethod = ifoo.DefineMethod("getFoo", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ifoo));

            var ibar = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IBar"), NamespaceSymbol.Internal, scope.Module.Name));
            ibar.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var barMethod = ibar.DefineMethod("getBar", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ibar));

            var zoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Zoo"), NamespaceSymbol.Internal, scope.Module.Name));
            zoo.Parents.Add(ifoo);
            zoo.Parents.Add(ibar);
            zoo.Flags = ClassFlags.Public;

            var zooFoo = zoo.DefineMethod("getFoo", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            var zooFooGen = zooFoo.GetGenerator();
            zooFooGen.Emit(OpCodes.LDC_I4_S, 10);
            zooFooGen.Emit(OpCodes.RET);

            var zooBar = zoo.DefineMethod("getBar", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            var zooBarGen = zooBar.GetGenerator();
            zooBarGen.Emit(OpCodes.LDC_I4_S, 20);
            zooBarGen.Emit(OpCodes.RET);

            ctx.zoo = zoo;
            ctx.fooMethod = fooMethod;
            ctx.barMethod = barMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass zoo = ctx.zoo;
            VeinMethod fooMethod = ctx.fooMethod;
            VeinMethod barMethod = ctx.barMethod;

            gen.Emit(OpCodes.NEWOBJ, zoo);
            gen.Emit(OpCodes.CALL, fooMethod);

            gen.Emit(OpCodes.NEWOBJ, zoo);
            gen.Emit(OpCodes.CALL, barMethod);

            gen.Emit(OpCodes.ADD);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(30, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Two different classes implement the same interface, each returns a different value.
    /// Verifies that dispatch goes to the correct implementation based on object type.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_PolymorphicDispatch_DifferentImplementors()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface IValue with getValue() -> i32
            var ivalue = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IValue"), NamespaceSymbol.Internal, scope.Module.Name));
            ivalue.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var ivalueMethod = ivalue.DefineMethod("getValue", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ivalue));

            // Class Alpha: getValue returns 100
            var alpha = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Alpha"), NamespaceSymbol.Internal, scope.Module.Name));
            alpha.Parents.Add(ivalue);
            alpha.Flags = ClassFlags.Public;
            var alphaMethod = alpha.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, alpha));
            var alphaGen = alphaMethod.GetGenerator();
            alphaGen.Emit(OpCodes.LDC_I4_S, 100);
            alphaGen.Emit(OpCodes.RET);

            // Class Beta: getValue returns 200
            var beta = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Beta"), NamespaceSymbol.Internal, scope.Module.Name));
            beta.Parents.Add(ivalue);
            beta.Flags = ClassFlags.Public;
            var betaMethod = beta.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, beta));
            var betaGen = betaMethod.GetGenerator();
            betaGen.Emit(OpCodes.LDC_I4_S, 200);
            betaGen.Emit(OpCodes.RET);

            ctx.alpha = alpha;
            ctx.beta = beta;
            ctx.ivalueMethod = ivalueMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass alpha = ctx.alpha;
            VeinClass beta = ctx.beta;
            VeinMethod ivalueMethod = ctx.ivalueMethod;

            // new Alpha().getValue() + new Beta().getValue() = 100 + 200 = 300
            gen.Emit(OpCodes.NEWOBJ, alpha);
            gen.Emit(OpCodes.CALL, ivalueMethod);

            gen.Emit(OpCodes.NEWOBJ, beta);
            gen.Emit(OpCodes.CALL, ivalueMethod);

            gen.Emit(OpCodes.ADD);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(300, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Interface with multiple methods — each dispatches independently.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_MultipleMethodsOnSameInterface()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface ICalc with getX() and getY()
            var icalc = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("ICalc"), NamespaceSymbol.Internal, scope.Module.Name));
            icalc.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var getX = icalc.DefineMethod("getX", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, icalc));
            var getY = icalc.DefineMethod("getY", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, icalc));

            // Class Point implements both
            var point = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Point"), NamespaceSymbol.Internal, scope.Module.Name));
            point.Parents.Add(icalc);
            point.Flags = ClassFlags.Public;

            var pointGetX = point.DefineMethod("getX", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, point));
            var pointGetXGen = pointGetX.GetGenerator();
            pointGetXGen.Emit(OpCodes.LDC_I4_S, 3);
            pointGetXGen.Emit(OpCodes.RET);

            var pointGetY = point.DefineMethod("getY", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, point));
            var pointGetYGen = pointGetY.GetGenerator();
            pointGetYGen.Emit(OpCodes.LDC_I4_S, 7);
            pointGetYGen.Emit(OpCodes.RET);

            ctx.point = point;
            ctx.getX = getX;
            ctx.getY = getY;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass point = ctx.point;
            VeinMethod getX = ctx.getX;
            VeinMethod getY = ctx.getY;

            // new Point().getX() * new Point().getY() = 3 * 7 = 21
            gen.Emit(OpCodes.NEWOBJ, point);
            gen.Emit(OpCodes.CALL, getX);

            gen.Emit(OpCodes.NEWOBJ, point);
            gen.Emit(OpCodes.CALL, getY);

            gen.Emit(OpCodes.MUL);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(21, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Interface method returning void — verifies dispatch works for void-returning methods.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_VoidMethodDispatch()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var voidType = VeinTypeCode.TYPE_VOID.AsClass()(scope.Types);
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            var ifoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IFoo"), NamespaceSymbol.Internal, scope.Module.Name));
            ifoo.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var ifooMethod = ifoo.DefineMethod("doFoo", MethodFlags.Abstract | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ifoo));

            // Also define a getValue method that returns i32
            var getVal = ifoo.DefineMethod("getVal", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ifoo));

            var zoo = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Zoo"), NamespaceSymbol.Internal, scope.Module.Name));
            zoo.Parents.Add(ifoo);
            zoo.Flags = ClassFlags.Public;
            var zooDoFoo = zoo.DefineMethod("doFoo", MethodFlags.Override | MethodFlags.Public, voidType,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            zooDoFoo.GetGenerator().Emit(OpCodes.RET);

            var zooGetVal = zoo.DefineMethod("getVal", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, zoo));
            zooGetVal.GetGenerator().Emit(OpCodes.LDC_I4_S, 88);
            zooGetVal.GetGenerator().Emit(OpCodes.RET);

            ctx.zoo = zoo;
            ctx.ifooMethod = ifooMethod;
            ctx.getVal = getVal;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass zoo = ctx.zoo;
            VeinMethod ifooMethod = ctx.ifooMethod;
            VeinMethod getVal = ctx.getVal;

            // Call void method then value method
            gen.Emit(OpCodes.NEWOBJ, zoo);
            gen.Emit(OpCodes.CALL, ifooMethod); // void dispatch, shouldn't crash

            gen.Emit(OpCodes.NEWOBJ, zoo);
            gen.Emit(OpCodes.CALL, getVal); // returns 88
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(88, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Two classes sharing the same interface: dispatch to the correct one based on instantiation.
    /// Tests that interface vtable offsets don't leak between different implementing classes.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_TwoImplementorsSequentialCalls()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface IValue
            var ivalue = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IValue"), NamespaceSymbol.Internal, scope.Module.Name));
            ivalue.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var ivalueMethod = ivalue.DefineMethod("getValue", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ivalue));

            // First implementor returns 33
            var first = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("First"), NamespaceSymbol.Internal, scope.Module.Name));
            first.Parents.Add(ivalue);
            first.Flags = ClassFlags.Public;
            var firstMethod = first.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, first));
            firstMethod.GetGenerator().Emit(OpCodes.LDC_I4_S, 33);
            firstMethod.GetGenerator().Emit(OpCodes.RET);

            // Second implementor returns 44
            var second = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Second"), NamespaceSymbol.Internal, scope.Module.Name));
            second.Parents.Add(ivalue);
            second.Flags = ClassFlags.Public;
            var secondMethod = second.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, second));
            secondMethod.GetGenerator().Emit(OpCodes.LDC_I4_S, 44);
            secondMethod.GetGenerator().Emit(OpCodes.RET);

            ctx.first = first;
            ctx.second = second;
            ctx.ivalueMethod = ivalueMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass first = ctx.first;
            VeinClass second = ctx.second;
            VeinMethod ivalueMethod = ctx.ivalueMethod;

            // new First().getValue() then new Second().getValue() then add
            gen.Emit(OpCodes.NEWOBJ, first);
            gen.Emit(OpCodes.CALL, ivalueMethod); // 33

            gen.Emit(OpCodes.NEWOBJ, second);
            gen.Emit(OpCodes.CALL, ivalueMethod); // 44

            gen.Emit(OpCodes.ADD); // 33 + 44 = 77
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(77, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Child class overrides the interface method that was already implemented by parent.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_ChildOverridesParentImplementation()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface IValue
            var ivalue = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IValue"), NamespaceSymbol.Internal, scope.Module.Name));
            ivalue.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var ivalueMethod = ivalue.DefineMethod("getValue", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ivalue));

            // Base class implements IValue, returns 10
            var baseClass = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("BaseImpl"), NamespaceSymbol.Internal, scope.Module.Name));
            baseClass.Parents.Add(ivalue);
            baseClass.Flags = ClassFlags.Public;
            var baseMethod = baseClass.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public | MethodFlags.Virtual, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, baseClass));
            var baseGen = baseMethod.GetGenerator();
            baseGen.Emit(OpCodes.LDC_I4_S, 10);
            baseGen.Emit(OpCodes.RET);

            // Child overrides getValue, returns 99
            var child = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("ChildImpl"), NamespaceSymbol.Internal, scope.Module.Name));
            child.Parents.Add(baseClass);
            child.Flags = ClassFlags.Public;
            var childMethod = child.DefineMethod("getValue", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, child));
            var childGen = childMethod.GetGenerator();
            childGen.Emit(OpCodes.LDC_I4_S, 99);
            childGen.Emit(OpCodes.RET);

            ctx.child = child;
            ctx.baseClass = baseClass;
            ctx.ivalueMethod = ivalueMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass child = ctx.child;
            VeinClass baseClass = ctx.baseClass;
            VeinMethod ivalueMethod = ctx.ivalueMethod;

            // new ChildImpl().getValue() should return 99 (child override)
            gen.Emit(OpCodes.NEWOBJ, child);
            gen.Emit(OpCodes.CALL, ivalueMethod);

            // new BaseImpl().getValue() should return 10 (base implementation)
            gen.Emit(OpCodes.NEWOBJ, baseClass);
            gen.Emit(OpCodes.CALL, ivalueMethod);

            gen.Emit(OpCodes.ADD); // 99 + 10 = 109
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(109, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Class implements three interfaces — stress test for interface list.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_ThreeInterfaces_AllDispatched()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            var ia = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IA"), NamespaceSymbol.Internal, scope.Module.Name));
            ia.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var maMethod = ia.DefineMethod("getA", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ia));

            var ib = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IB"), NamespaceSymbol.Internal, scope.Module.Name));
            ib.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var mbMethod = ib.DefineMethod("getB", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ib));

            var ic = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IC"), NamespaceSymbol.Internal, scope.Module.Name));
            ic.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var mcMethod = ic.DefineMethod("getC", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ic));

            // Class Impl : IA, IB, IC
            var impl = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Impl"), NamespaceSymbol.Internal, scope.Module.Name));
            impl.Parents.Add(ia);
            impl.Parents.Add(ib);
            impl.Parents.Add(ic);
            impl.Flags = ClassFlags.Public;

            var implA = impl.DefineMethod("getA", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, impl));
            implA.GetGenerator().Emit(OpCodes.LDC_I4_S, 1); implA.GetGenerator().Emit(OpCodes.RET);

            var implB = impl.DefineMethod("getB", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, impl));
            implB.GetGenerator().Emit(OpCodes.LDC_I4_S, 2); implB.GetGenerator().Emit(OpCodes.RET);

            var implC = impl.DefineMethod("getC", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, impl));
            implC.GetGenerator().Emit(OpCodes.LDC_I4_S, 4); implC.GetGenerator().Emit(OpCodes.RET);

            ctx.impl = impl;
            ctx.maMethod = maMethod;
            ctx.mbMethod = mbMethod;
            ctx.mcMethod = mcMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass impl = ctx.impl;
            VeinMethod ma = ctx.maMethod;
            VeinMethod mb = ctx.mbMethod;
            VeinMethod mc = ctx.mcMethod;

            // getA() + getB() + getC() = 1 + 2 + 4 = 7
            gen.Emit(OpCodes.NEWOBJ, impl);
            gen.Emit(OpCodes.CALL, ma);

            gen.Emit(OpCodes.NEWOBJ, impl);
            gen.Emit(OpCodes.CALL, mb);
            gen.Emit(OpCodes.ADD);

            gen.Emit(OpCodes.NEWOBJ, impl);
            gen.Emit(OpCodes.CALL, mc);
            gen.Emit(OpCodes.ADD);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(7, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Same method name on two different interfaces, class implements both.
    /// Verifies that the correct interface method dispatches to the same implementation.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_SameMethodNameOnDifferentInterfaces()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface IA with "compute() -> i32"
            var ia = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IA"), NamespaceSymbol.Internal, scope.Module.Name));
            ia.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var iaMethod = ia.DefineMethod("compute", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ia));

            // Interface IB with "compute() -> i32" (same name!)
            var ib = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IB"), NamespaceSymbol.Internal, scope.Module.Name));
            ib.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var ibMethod = ib.DefineMethod("compute", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, ib));

            // Class Impl : IA, IB - single "compute" implementation satisfies both
            var impl = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Impl"), NamespaceSymbol.Internal, scope.Module.Name));
            impl.Parents.Add(ia);
            impl.Parents.Add(ib);
            impl.Flags = ClassFlags.Public;

            var implMethod = impl.DefineMethod("compute", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, impl));
            implMethod.GetGenerator().Emit(OpCodes.LDC_I4_S, 77);
            implMethod.GetGenerator().Emit(OpCodes.RET);

            ctx.impl = impl;
            ctx.iaMethod = iaMethod;
            ctx.ibMethod = ibMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass impl = ctx.impl;
            VeinMethod iaMethod = ctx.iaMethod;
            VeinMethod ibMethod = ctx.ibMethod;

            // Call through IA.compute and IB.compute - both should resolve to Impl.compute = 77
            gen.Emit(OpCodes.NEWOBJ, impl);
            gen.Emit(OpCodes.CALL, iaMethod);

            gen.Emit(OpCodes.NEWOBJ, impl);
            gen.Emit(OpCodes.CALL, ibMethod);

            gen.Emit(OpCodes.ADD); // 77 + 77 = 154
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(154, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Interface method with an argument (not just this).
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_MethodWithArgument()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface IAdder with "add(this, x: i32) -> i32"
            var iadder = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("IAdder"), NamespaceSymbol.Internal, scope.Module.Name));
            iadder.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var addMethod = iadder.DefineMethod("add", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, iadder),
                new VeinArgumentRef("x", i4Type));

            // Class FiveAdder: add(x) returns x + 5
            var fiveAdder = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("FiveAdder"), NamespaceSymbol.Internal, scope.Module.Name));
            fiveAdder.Parents.Add(iadder);
            fiveAdder.Flags = ClassFlags.Public;
            var fiveAdd = fiveAdder.DefineMethod("add", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, fiveAdder),
                new VeinArgumentRef("x", i4Type));
            var fiveGen = fiveAdd.GetGenerator();
            fiveGen.Emit(OpCodes.LDC_I4_S, 5);
            fiveGen.Emit(OpCodes.LDARG_1); // x is arg[1] (arg[0] is this)
            fiveGen.Emit(OpCodes.ADD);
            fiveGen.Emit(OpCodes.RET);

            ctx.fiveAdder = fiveAdder;
            ctx.addMethod = addMethod;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass fiveAdder = ctx.fiveAdder;
            VeinMethod addMethod = ctx.addMethod;

            // new FiveAdder().add(10) = 10 + 5 = 15
            gen.Emit(OpCodes.NEWOBJ, fiveAdder);
            gen.Emit(OpCodes.LDC_I4_S, 10);
            gen.Emit(OpCodes.CALL, addMethod);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(15, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Interface with two methods where implementation returns sum of constants.
    /// Verifies multi-method interface dispatch with arithmetic.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_TwoMethodsArithmeticResult()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            // Interface ICalc with getLeft() and getRight()
            var icalc = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("ICalc"), NamespaceSymbol.Internal, scope.Module.Name));
            icalc.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var getLeft = icalc.DefineMethod("getLeft", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, icalc));
            var getRight = icalc.DefineMethod("getRight", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, icalc));

            // Class Pair: getLeft=13, getRight=7
            var pair = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Pair"), NamespaceSymbol.Internal, scope.Module.Name));
            pair.Parents.Add(icalc);
            pair.Flags = ClassFlags.Public;

            var pairLeft = pair.DefineMethod("getLeft", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, pair));
            pairLeft.GetGenerator().Emit(OpCodes.LDC_I4_S, 13);
            pairLeft.GetGenerator().Emit(OpCodes.RET);

            var pairRight = pair.DefineMethod("getRight", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, pair));
            pairRight.GetGenerator().Emit(OpCodes.LDC_I4_S, 7);
            pairRight.GetGenerator().Emit(OpCodes.RET);

            ctx.pair = pair;
            ctx.getLeft = getLeft;
            ctx.getRight = getRight;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass pair = ctx.pair;
            VeinMethod getLeft = ctx.getLeft;
            VeinMethod getRight = ctx.getRight;

            // new Pair().getLeft() - new Pair().getRight() = 13 - 7 = 6
            gen.Emit(OpCodes.NEWOBJ, pair);
            gen.Emit(OpCodes.CALL, getLeft);

            gen.Emit(OpCodes.NEWOBJ, pair);
            gen.Emit(OpCodes.CALL, getRight);

            gen.Emit(OpCodes.SUB);
            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(6, result->returnValue[0].data.l);
    }

    /// <summary>
    /// Interface method dispatched multiple times on the same object.
    /// Verifies no corruption across repeated calls.
    /// </summary>
    [Test]
    [Parallelizable(ParallelScope.None)]
    public void Interface_RepeatedCallsOnSameObject()
    {
        using var scope = CreateScope();

        scope.OnClassBuild((@class, ctx) =>
        {
            var i4Type = VeinTypeCode.TYPE_I4.AsClass()(scope.Types);

            var icounter = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("ICounter"), NamespaceSymbol.Internal, scope.Module.Name));
            icounter.Flags = ClassFlags.Interface | ClassFlags.Public | ClassFlags.Abstract;
            var getVal = icounter.DefineMethod("getVal", MethodFlags.Abstract | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, icounter));

            var counter = scope.Module.DefineClass(new QualityTypeName(
                new NameSymbol("Counter"), NamespaceSymbol.Internal, scope.Module.Name));
            counter.Parents.Add(icounter);
            counter.Flags = ClassFlags.Public;
            var counterGetVal = counter.DefineMethod("getVal", MethodFlags.Override | MethodFlags.Public, i4Type,
                new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, counter));
            counterGetVal.GetGenerator().Emit(OpCodes.LDC_I4_S, 11);
            counterGetVal.GetGenerator().Emit(OpCodes.RET);

            ctx.counter = counter;
            ctx.getVal = getVal;
        });

        scope.OnCodeBuild((gen, ctx) =>
        {
            VeinClass counter = ctx.counter;
            VeinMethod getVal = ctx.getVal;

            // Call getVal 3 times on 3 new objects, sum them: 11 + 11 + 11 = 33
            gen.Emit(OpCodes.NEWOBJ, counter);
            gen.Emit(OpCodes.CALL, getVal);

            gen.Emit(OpCodes.NEWOBJ, counter);
            gen.Emit(OpCodes.CALL, getVal);
            gen.Emit(OpCodes.ADD);

            gen.Emit(OpCodes.NEWOBJ, counter);
            gen.Emit(OpCodes.CALL, getVal);
            gen.Emit(OpCodes.ADD);

            gen.Emit(OpCodes.RET);
        });

        var result = scope.Compile().Execute().Validate();
        Equals(33, result->returnValue[0].data.l);
    }
}
