


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using static System.FormattableString;

namespace ishtar.debugger
{
    internal class IshtarScope : IshtarObject<Scope, object>, IIshtarVariableContainer
    {
        #region Private Fields

        private bool expensive;
        private List<IshtarVariable> variables;

        #endregion

        #region Constructor

        internal IshtarScope(IshtarDebugAdapter adapter, string name, bool expensive)
            : base(adapter)
        {
            this.variables = new List<IshtarVariable>();

            this.Name = name;
            this.expensive = expensive;
        }

        #endregion

        #region IshtarObject Implementation

        public override void Invalidate()
        {
            base.Invalidate();

            foreach (IshtarVariable variable in this.variables)
            {
                variable.Invalidate();
            }
        }

        protected override bool IsSameFormat(object a, object b)
        {
            // Scopes don't have formatting.
            return true;
        }

        protected override Scope CreateProtocolObject()
        {
            return new Scope(
                name: this.Name,
                variablesReference: this.Adapter.GetNextId(),
                expensive: this.expensive);
        }

        #endregion

        internal void AddVariable(IshtarVariable variable)
        {
            this.variables.Add(variable);
            variable.SetContainer(this);
        }

        #region IIshtarVariableContainer Implementation

        public string Name { get; private set; }

        public IReadOnlyCollection<IIshtarVariableContainer> ChildContainers
        {
            get { return this.variables; }
        }

        public IReadOnlyCollection<IshtarVariable> Variables
        {
            get { return this.variables; }
        }

        public int VariableReference
        {
            get
            {
                return this.ProtocolObject?.VariablesReference ?? 0;
            }
        }

        public VariablesResponse HandleVariablesRequest(VariablesArguments args)
        {
            return new VariablesResponse(variables: this.variables.Select(v => v.GetProtocolObject(args.Format)).ToList());
        }

        public SetVariableResponse HandleSetVariableRequest(SetVariableArguments args)
        {
            IshtarVariable variable = this.variables.FirstOrDefault(v => String.Equals(v.Name, args.Name, StringComparison.Ordinal));
            if (variable == null)
            {
                throw new ProtocolException(Invariant($"Scope '{this.Name}' (varRef: {this.VariableReference}) does not contain a variable called '{args.Name}'!"));
            }

            variable.SetValue(args.Value);
            variable.Invalidate();

            return new SetVariableResponse(value: variable.GetValue(args.Format?.Hex ?? false));
        }

        public IIshtarVariableContainer Container
        {
            get { return null; }
        }

        #endregion
    }
}
