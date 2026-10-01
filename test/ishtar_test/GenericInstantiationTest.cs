namespace ishtar_test;

using ishtar;
using ishtar.collections;
using ishtar.runtime;
using ishtar.runtime.gc;
using NUnit.Framework;
using System.IO;
using vein.runtime;

/// <summary>
/// Instantiation cache foundation: closed generic types (List&lt;i32&gt; vs List&lt;string&gt;) intern to a
/// stable, uniqued handle via <see cref="VirtualMachine.InstantiateGeneric"/>, giving generic
/// instantiations pointer identity — the base for is / cast / typeof over generics.
/// </summary>
[TestFixture]
public unsafe class GenericInstantiationTest
{
    [Test]
    public void InstantiateGeneric_UniquesByDefinitionAndArgs()
    {
        VirtualMachine.static_init();
        var vm = VirtualMachine.Create("gen-instantiate-test", new AppConfig(VirtualMachine.readBootCfg()));
        var deps = IshtarGC.AllocateList<RuntimeIshtarModule>(null);
        var resolver = vm->Vault.GetResolver();
        resolver.AddSearchPath(new DirectoryInfo("./"));
        var std = resolver.ResolveDep("std", new IshtarVersion(0, 0), deps);
        Assert.That(std != null, "Failed to load std.wll");

        var listDef = FindClass(std, "List<");
        Assert.That(listDef != null, "List<T> not found in std");

        var i32 = VeinTypeCode.TYPE_I4.AsRuntimeClass(vm->Types);
        var str = VeinTypeCode.TYPE_STRING.AsRuntimeClass(vm->Types);

        var listI32a = VirtualMachine.InstantiateGeneric(listDef, ArgsOf(i32));
        var listI32b = VirtualMachine.InstantiateGeneric(listDef, ArgsOf(i32));
        var listStr  = VirtualMachine.InstantiateGeneric(listDef, ArgsOf(str));

        TestContext.WriteLine($"List<i32>    => {listI32a->ToTemplateString()}");
        TestContext.WriteLine($"List<string> => {listStr->ToTemplateString()}");

        // Same (definition, args) interns to the SAME handle; different args to a different one.
        Assert.That(listI32a == listI32b, "List<i32> must intern to the same handle");
        Assert.That(listI32a != listStr, "List<i32> and List<string> must be distinct handles");

        // Identity helper agrees with pointer identity.
        Assert.That(VirtualMachine.SameGenericInstance(listI32a, listI32b), Is.True);
        Assert.That(VirtualMachine.SameGenericInstance(listI32a, listStr), Is.False);

        // The handle is a constructed generic that renders with its real arguments.
        Assert.That(listI32a->IsGenericInstance, Is.True);
        Assert.That(listI32a->ToTemplateString(), Is.Not.EqualTo(listStr->ToTemplateString()));

        // Release the VM so the next fixture in this process can create its own (undisposed VMs corrupt the next Create).
        vm->Dispose();
    }

    private static NativeList<RuntimeComplexType>* ArgsOf(RuntimeIshtarClass* arg)
    {
        var list = IshtarGC.AllocateList<RuntimeComplexType>(null);
        var el = IshtarGC.AllocateImmortal<RuntimeComplexType>(null);
        *el = new RuntimeComplexType(arg);
        list->Add(el);
        return list;
    }

    private static RuntimeIshtarClass* FindClass(RuntimeIshtarModule* mod, string namePrefix)
    {
        var count = mod->class_table->Count;
        for (var i = 0; i < count; i++)
        {
            var c = mod->class_table->Get(i);
            if (c->Name.StartsWith(namePrefix))
                return c;
        }
        return null;
    }
}
