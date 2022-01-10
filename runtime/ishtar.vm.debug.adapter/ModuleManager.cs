namespace ishtar.debugger;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Ookii.CommandLine;

using static System.FormattableString;


internal class ModuleManager
{
    #region Private Fields

    private IshtarDebugAdapter adapter;
    private IList<RuntimeIshtarModule> loadedModules;

    #endregion

    #region Constructor

    internal ModuleManager(IshtarDebugAdapter adapter)
    {
        this.adapter = adapter;
        this.loadedModules = new List<RuntimeIshtarModule>();

        this.adapter.RegisterDirective<LoadModuleArgs>("LoadModule", this.DoLoadModule);
        this.adapter.RegisterDirective<UnloadModuleArgs>("UnloadModule", this.DoUnloadModule);
    }

    #endregion

    #region Internal API

    internal RuntimeIshtarModule GetModuleById(string moduleId)
        => this.loadedModules.FirstOrDefault(m => string.Equals($"{m.ID}", moduleId, StringComparison.Ordinal));

    #endregion

    #region Protocol Members

    internal ModulesResponse HandleModulesRequest(ModulesArguments arguments)
    {
        IEnumerable<Module> modules = this.loadedModules.Select(m => m.GetProtocolModule());

        int startModule = arguments.StartModule ?? 0;
        if (startModule != 0)
        {
            modules = modules.Skip(startModule);
        }

        int moduleCount = arguments.ModuleCount ?? 0;
        if (moduleCount != 0)
        {
            modules = modules.Take(moduleCount);
        }

        return new ModulesResponse() { Modules = modules.ToList(), TotalModules = loadedModules.Count };
    }

    #endregion

    #region LoadModule Directive

    private class LoadModuleArgs
    {
        [CommandLineArgument("name", IsRequired = true, Position = 0, ValueDescription = "module name")]
        public string ModuleName { get; set; }

        [CommandLineArgument("id", IsRequired = false, ValueDescription = "module id")]
        public string Id { get; set; }

        [CommandLineArgument("version", IsRequired = false, ValueDescription = "version")]
        public string Version { get; set; }

        [CommandLineArgument("symbolstatus", IsRequired = false, ValueDescription = "symbol status")]
        public string SymbolStatus { get; set; }

        [CommandLineArgument("loadaddress", IsRequired = false, ValueDescription = "load address")]
        public string LoadAddress { get; set; }

        [CommandLineArgument("preferredloadaddress", IsRequired = false, ValueDescription = "preferred load address")]
        public string PreferredLoadAddress { get; set; }

        [CommandLineArgument("size", IsRequired = false, ValueDescription = "module size")]
        public string Size { get; set; }

        [CommandLineArgument("timestamp", IsRequired = false, ValueDescription = "symbol timestamp")]
        public string Timestamp { get; set; }

        [CommandLineArgument("symbolfile", IsRequired = false, ValueDescription = "symbol file")]
        public string SymbolFile { get; set; }

        [CommandLineArgument("is64bit", IsRequired = false, ValueDescription = "is 64-bit")]
        public bool? Is64Bit { get; set; }

        [CommandLineArgument("optimized", IsRequired = false, ValueDescription = "is optimized")]
        public bool? IsOptimized { get; set; }

        [CommandLineArgument("usercode", IsRequired = false, ValueDescription = "is user code")]
        public bool? IsUserCode { get; set; }
    }

    private bool DoLoadModule(LoadModuleArgs args, StringBuilder output)
    {
        //var resolver = AppVault.CurrentVault.GetResolver();

        //resolver.

        //output.AppendLine(Invariant($"Loading module '{args.ModuleName}'"));

        //this.loadedModules.Add(module);

        //this.adapter.Protocol.SendEvent(
        //    new ModuleEvent(
        //        reason: ModuleEvent.ReasonValue.New,
        //        module: module.GetProtocolModule()));

        return false;
    }

    #endregion

    #region UnloadModule Directive

    private class UnloadModuleArgs
    {
        [CommandLineArgument("name", IsRequired = true, Position = 0, ValueDescription = "module name")]
        public string ModuleName { get; set; }
    }

    private bool DoUnloadModule(UnloadModuleArgs args, StringBuilder output)
    {
        var module = this.loadedModules.FirstOrDefault(m => String.Equals(m.Name, args.ModuleName, StringComparison.OrdinalIgnoreCase));
        if (module == null)
        {
            output.AppendLine(Invariant($"Error: Unknown module '{args.ModuleName}'!"));
            return false;
        }

        output.AppendLine(Invariant($"Unloading module '{args.ModuleName}'"));
        this.adapter.Protocol.SendEvent(
            new ModuleEvent(
                reason: ModuleEvent.ReasonValue.Removed,
                module: new Module(
                    id: module.ID,
                    name: null)));

        return true;
    }

    #endregion
}
