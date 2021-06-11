namespace ishtar
{
    using System.Net.Sockets;
    using static mana.runtime.MethodFlags;
    using static mana.runtime.ManaTypeCode;
    public unsafe class B_Socket
    {
        [IshtarExport(0, "@_sock_is_support_ipv6")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* IsSupportIPv6(CallFrame current, IshtarObject** _)
            => IshtarMarshal.ToIshtarObject(Socket.OSSupportsIPv6, current);

        [IshtarExport(0, "@_sock_is_support_ipv4")]
        [IshtarExportFlags(Public | Static)]
        public static IshtarObject* IsSupportIPv4(CallFrame current, IshtarObject** _)
            => IshtarMarshal.ToIshtarObject(Socket.OSSupportsIPv4, current);
    }
}
