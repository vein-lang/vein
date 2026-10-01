namespace ishtar_test;

using System.Runtime.CompilerServices;
using ishtar;

/// <summary>
/// Routes fatal VM faults to a managed <see cref="IshtarFaultException"/> instead of
/// <c>Environment.Exit</c> for the whole test assembly. Without this, a VM fault terminates the
/// NUnit host ("test host process crashed") and the real reason — printed to the VM's stdout, which
/// NUnit swallows — is lost. With it, the fault surfaces as an ordinary test failure carrying the
/// message. DEBUG-only behaviour (the interception is compiled out in Release).
/// </summary>
internal static class TestFaultHook
{
    [ModuleInitializer]
    internal static void Enable() => IshtarWatchDog.CaptureFaultsAsExceptions = true;
}
