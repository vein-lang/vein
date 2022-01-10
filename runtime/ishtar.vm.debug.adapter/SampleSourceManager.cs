


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Ookii.CommandLine;
using static System.FormattableString;

namespace ishtar.debugger
{
    class SampleSourceManager
    {
        private IshtarDebugAdapter adapter;
        private List<VeinSource> loadedSources;

        public SampleSourceManager(IshtarDebugAdapter adapter)
        {
            this.adapter = adapter;
            this.loadedSources = new List<VeinSource>();

            this.adapter.RegisterDirective<SourceArgs>("LoadSource", this.DoLoadSource);
            this.adapter.RegisterDirective<SourceArgs>("UnloadSource", this.DoUnloadSource);
        }

        internal SourceResponse HandleSourceRequest(SourceArguments arguments)
        {
            return new SourceResponse("For now all source requests return this line of 'code'.");
        }

        #region Directives

        private class SourceArgs
        {
            [CommandLineArgument("name", IsRequired = true, Position = 0, ValueDescription = "script name")]
            public string Name { get; set; }

            [CommandLineArgument("path", IsRequired = false, Position = 1, ValueDescription = "script path")]
            public string Path { get; set; }

            [CommandLineArgument("sourceReference", IsRequired = false, Position = 2, ValueDescription = "script source reference")]
            public int SourceReference { get; set; }
        }

        #region LoadScript Directive

        private bool DoLoadSource(SourceArgs args, StringBuilder output)
        {
            VeinSource source = VeinSource.Create(output, this, args.Name, args.Path, args.SourceReference);

            output.AppendLine(Invariant($"Loading source '{args.Name}'"));

            this.loadedSources.Add(source);

            this.adapter.Protocol.SendEvent(
                new LoadedSourceEvent(
                    reason: LoadedSourceEvent.ReasonValue.New,
                    source: source.GetProtocolSource()));

            return true;
        }

        #endregion

        #region UnloadScript Directive

        private bool DoUnloadSource(SourceArgs args, StringBuilder output)
        {
            VeinSource source = this.loadedSources.FirstOrDefault(m => String.Equals(m.Name, args.Name, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                output.AppendLine(Invariant($"Error: Unknown source '{args.Name}'!"));
                return false;
            }

            output.AppendLine(Invariant($"Unloading source '{args.Name}'"));
            this.adapter.Protocol.SendEvent(
                new LoadedSourceEvent(
                    reason: LoadedSourceEvent.ReasonValue.Removed,
                    source: source.GetProtocolSource()));

            return true;
        }

        #endregion

        #endregion
    }
}
