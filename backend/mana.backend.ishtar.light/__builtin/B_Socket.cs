namespace ishtar
{
    using System.Net.Sockets;
    using static mana.runtime.MethodFlags;
    using static mana.runtime.ManaTypeCode;
    public unsafe class B_Socket
    {
        [IshtarExport(0, "@_sock_is_support_ipv6")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* IsSupportIPv6(CallFrame current, IshtarObject** args)
        {
            return null;
        }

        [IshtarExport(2, "@_sock_listen")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* NativeListen(CallFrame current, IshtarObject** args)
        {
            return null;
        }
    }
}
