namespace ishtar.debugger;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using static System.FormattableString;


public static class IshtarModuleExtensions
{
    public static Module GetProtocolModule(this RuntimeIshtarModule @this)
    {
        var module = new Module
        {
            Id = @this.ID,
            Name = @this.Name,
            Path = @this.ModulePath?.FullName ?? "<unknown location>",
            IsUserCode = true,
            VsIs64Bit = true,
            VsModuleSize = (int)@this.Size,
            Version = $"{@this.Version}",
            SymbolStatus = @this.SymbolPath == null ? "Symbols not found" : "Symbols Loaded",
            AddressRange = Invariant($"0x{0:X16} - 0x{((ulong)@this.Size):X16}")
        };

        return module;
    }
}
