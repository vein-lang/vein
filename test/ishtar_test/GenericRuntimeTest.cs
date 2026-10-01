namespace ishtar_test;

using ishtar;
using ishtar.collections;
using ishtar.runtime;
using ishtar.runtime.gc;
using NUnit.Framework;
using System;
using System.IO;
using vein.runtime;

/// <summary>
/// Runtime acceptance for reified generics + the natively-backed std <c>List&lt;T&gt;</c>/<c>Array&lt;T&gt;</c>:
/// loads std and executes the GenSmoke entry points, which construct and use a closed generic instance
/// (<c>List&lt;i32&gt;</c>, internally <c>Array&lt;T&gt;</c>) end-to-end through the VM — add, typed get,
/// count, capacity growth, remove (indexOf via _sys_eql) and clear.
///
/// Both probes run inside a single VM on purpose: each scenario passes in isolation, but creating a
/// fresh VM per test trips a pre-existing cross-VM static-state leak in the test harness (the same
/// class of ordering issue as FECall-after-Async). One VM, sequential exec, sidesteps that.
/// </summary>
[TestFixture]
public unsafe class GenericRuntimeTest
{
    [Test]
    public void List_i32_ConstructAddCountAndGrow()
    {
        VirtualMachine.static_init();
        var appCfg = new AppConfig(VirtualMachine.readBootCfg());
        var vm = VirtualMachine.Create("gen-runtime-test", appCfg);
        var deps = IshtarGC.AllocateList<RuntimeIshtarModule>(null);

        var resolver = vm->Vault.GetResolver();
        resolver.AddSearchPath(new DirectoryInfo("./"));
        var std = resolver.ResolveDep("std", new IshtarVersion(0, 0), deps);
        Assert.That(std != null, "Failed to load std.wll");

        // new List<i32>(); Add(42); Add(7); return Count();  -> 2
        // Proves end-to-end reified generic execution: NEWOBJ instantiation, LD_TYPE_G (new Array<T>),
        // generic dispatch, field access, and Array.Set storing through native backing.
        Assert.That(RunInt(vm, std, "gen_smoke"), Is.EqualTo(2));

        // Add(42); Add(7); return Get(0) + Get(1);  -> 49
        // Round-trips values through the native Array<T> storage and exercises call-site substitution
        // of Get's open return type T -> i32 plus the generic-return path (Array.Get returns `Any`).
        Assert.That(RunInt(vm, std, "gen_smoke_get"), Is.EqualTo(49));

        // Adds 6 elements (past the initial capacity of 4) so List.ensureCapacity runs: it builds a
        // second Array<T> via LD_TYPE_G and copies elements across with Array.Get/Set, then returns
        // Get(5) -> 5, proving the element survived the grow/copy.
        Assert.That(RunInt(vm, std, "gen_smoke_grow"), Is.EqualTo(5));

        // [10,20,30].Remove(20) -> indexOf (via _sys_eql) + RemoveAt shift -> [10,30]; Get(0)+Get(1) = 40.
        Assert.That(RunInt(vm, std, "gen_smoke_remove"), Is.EqualTo(40));

        // Add(1); Add(2); Clear(); Count() -> 0.
        Assert.That(RunInt(vm, std, "gen_smoke_clear"), Is.EqualTo(0));

        // Generic method gen_id<T>(x:T):T called with explicit type-args: gen_id<i32>(42) + gen_id<i32>(7) = 49.
        // Exercises method type-parameter binding, call-site <i32> parsing, and return-type substitution.
        Assert.That(RunInt(vm, std, "gen_smoke_genmethod"), Is.EqualTo(49));

        // Same, but T is INFERRED from the argument type (no explicit <i32>): gen_id(42) + gen_id(7) = 49.
        Assert.That(RunInt(vm, std, "gen_smoke_genmethod_infer"), Is.EqualTo(49));

        // Release the VM so the next fixture in this process can create its own (undisposed VMs corrupt the next Create).
        vm->Dispose();
    }

    private static int RunInt(VirtualMachine* vm, RuntimeIshtarModule* std, string rawName)
    {
        var method = FindMethod(std, rawName);
        Assert.That(method != null, $"GenSmoke.{rawName} not found in std");

        var args = stackalloc stackval[1];
        var frame = CallFrame.Create(method, null);
        frame->args = args;

        vm->exec_method(frame);

        Assert.That(frame->exception.value == null, $"{rawName} faulted");
        var result = frame->returnValue.Ref->data.i;
        TestContext.WriteLine($"{rawName} returned: {result}");
        return result;
    }

    private static RuntimeIshtarMethod* FindMethod(RuntimeIshtarModule* mod, string rawName)
    {
        var classCount = mod->class_table->Count;
        for (var i = 0; i < classCount; i++)
        {
            var clazz = mod->class_table->Get(i);
            var methodCount = clazz->Methods->Count;
            for (var j = 0; j < methodCount; j++)
            {
                var m = clazz->Methods->Get(j);
                if (m->RawName.Equals(rawName))
                    return m;
            }
        }
        return null;
    }
}
