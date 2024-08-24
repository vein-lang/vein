namespace ishtar.networks;

using System.Net;
using LLVMSharp;

public static unsafe class B_Dns
{
    private static IshtarObject* _dns_query_domain_details(CallFrame* current, IshtarObject** args)
    {
        var host = IshtarMarshal.ToDotnetString(args[0], current);
        var domain = new Vein_DomainDetails(args[1]);
        var ip = domain.address;
        try
        {
            var data = Dns.GetHostEntry(host);

            var addr = data.AddressList.FirstOrDefault();

            var bytes = addr.GetAddressBytes();
            ip.first = bytes[0];
            ip.second = bytes[1];
            ip.third = bytes[2];
            ip.fourth = bytes[3];

            return domain.Object;
        }
        catch (Exception e)
        {
            current->ThrowException(KnowTypes.SocketFault(current));
            return null;
        }
    }

    public static void InitTable(ForeignFunctionInterface ffi)
    {
        ffi.Add("_dns_query_domain_details([std]::std::String,[std]::std::DomainDetails) -> [std]::std::Void", ffi.AsNative(&_dns_query_domain_details));
    }
}
