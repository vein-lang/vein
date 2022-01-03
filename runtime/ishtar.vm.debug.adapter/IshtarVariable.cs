namespace ishtar.debugger;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using static System.FormattableString;


internal abstract class IshtarVariable : IshtarObject<Variable, ValueFormat>, IIshtarVariableContainer
{
    #region Constructor

    protected IshtarVariable(IshtarDebugAdapter adapter, string name, string type)
        : base(adapter)
    {
        this.Name = name;
        this.Type = type;
    }

    #endregion

    #region IshtarObject Implementation

    public override void Invalidate()
    {
        base.Invalidate();

        if (this.GetChildren(false) != null)
        {
            foreach (IshtarVariable variable in this.GetChildren())
            {
                variable.Invalidate();
            }
        }
    }

    protected override Variable CreateProtocolObject()
    {
        ValueFormat format = this.Format;

        VariablePresentationHint presentationHint = new VariablePresentationHint();
        if (this.IsReadOnly)
        {
            presentationHint.Attributes = VariablePresentationHint.AttributesValue.ReadOnly;
        }
        else
        {
            presentationHint.Attributes = VariablePresentationHint.AttributesValue.None;
        }

        return new Variable(
            name: this.Name,
            value: this.GetValue(format?.Hex ?? false),
            type: this.Type,
            variablesReference: (this.GetChildren() != null && this.GetChildren().Any()) ? this.Adapter.GetNextId() : 0,
            evaluateName: this.GetEvaluateName(),
            presentationHint: presentationHint);
    }

    internal string GetEvaluateName()
    {
        // Purposefully using a weird format to avoid looking like other languages
        string evalName = this.Name;
        IIshtarVariableContainer container = this.Container;

        while (container != null)
        {
            evalName = Invariant($"{container.Name}~{evalName}");
            container = container.Container;
        }

        return evalName;
    }

    #endregion

    public string Type { get; set; }

    internal void SetContainer(IIshtarVariableContainer container)
    {
        // Variables can be moved to a different scope under some conditions (e.g. if the Globals scope is disabled), but
        //  we want them to remain associated with their original container to avoid conficts
        if (this.Container == null)
        {
            this.Container = container;
        }
    }

    #region Abstract Members

    public IIshtarVariableContainer Container { get; private set; }

    public abstract bool IsReadOnly { get; }

    public abstract string GetValue(bool showInHex);
    public abstract void SetValue(string value);
    protected abstract IReadOnlyCollection<IshtarVariable> GetChildren(bool shouldCreate = true);

    protected override bool IsSameFormat(ValueFormat a, ValueFormat b)
    {
        if (Object.ReferenceEquals(null, a) || Object.ReferenceEquals(null, b))
            return Object.ReferenceEquals(a, b);
        return a.Hex == b.Hex;
    }

    #endregion

    #region IIshtarVariableContainer Implementation

    public string Name { get; private set; }

    public int VariableReference => this.ProtocolObject?.VariablesReference ?? 0;

    public IReadOnlyCollection<IIshtarVariableContainer> ChildContainers
        => this.GetChildren();

    public IReadOnlyCollection<IshtarVariable> Variables
        => this.GetChildren();

    public VariablesResponse HandleVariablesRequest(VariablesArguments args)
        => new VariablesResponse(variables:
            this.GetChildren().Any() ?
            this.GetChildren().Select(c => c.GetProtocolObject(args.Format)).ToList() : null);

    public SetVariableResponse HandleSetVariableRequest(SetVariableArguments args)
        => null;

    #endregion
}

internal class SimpleVariable : IshtarVariable
{
    #region Private Fields

    private string value;
    private List<IshtarVariable> children;

    #endregion

    #region Constructor

    public SimpleVariable(IshtarDebugAdapter adapter, string name, string type, string value)
        : base(adapter, name, type)
    {
        this.value = value;

        this.children = new List<IshtarVariable>();
    }

    #endregion

    #region IshtarVariable Implementation

    public override bool IsReadOnly
    {
        get { return false; }
    }

    internal static string ShowAsHex(bool showInHex, string value)
    {
        int valueAsInt;
        if (showInHex && Int32.TryParse(value, out valueAsInt))
        {
            return Invariant($"0x{valueAsInt:X8}");
        }
        return value;
    }

    public override string GetValue(bool showInHex)
    {
        return ShowAsHex(showInHex, this.value);
    }

    public override void SetValue(string value)
    {
        this.value = value;
    }

    protected override IReadOnlyCollection<IshtarVariable> GetChildren(bool shouldCreate = true)
    {
        return this.children;
    }

    #endregion

    internal void AddChild(IshtarVariable variable)
    {
        this.children.Add(variable);
        variable.SetContainer(this);
    }
}

internal class WrapperVariable : IshtarVariable
{
    #region Private Fields

    private Func<string> valueGetter;
    private Func<IReadOnlyCollection<IshtarVariable>> childrenGetter;

    private IReadOnlyCollection<IshtarVariable> _children;

    #endregion

    #region Constructor

    public WrapperVariable(IshtarDebugAdapter adapter, string name, string type, Func<string> valueGetter, Func<IReadOnlyCollection<IshtarVariable>> childrenGetter = null)
        : base(adapter, name, type)
    {
        this.valueGetter = valueGetter;
        this.childrenGetter = childrenGetter;
    }

    #endregion

    #region IshtarVariable Implementation

    public override bool IsReadOnly
        => true;

    public override string GetValue(bool showInHex)
        => this.valueGetter();

    public override void SetValue(string value)
        => throw new ProtocolException("Wrapper variables are read only!");

    protected override IReadOnlyCollection<IshtarVariable> GetChildren(bool shouldCreate = true)
    {
        if (this._children == null && this.childrenGetter != null && shouldCreate)
        {
            this._children = this.childrenGetter();

            if (this._children != null)
            {
                foreach (IshtarVariable child in this._children)
                {
                    child.SetContainer(this);
                }
            }
        }

        return this._children;
    }

    #endregion

    #region IshtarObject Implementation

    public override void Invalidate()
    {
        base.Invalidate();

        this._children = null;
    }

    #endregion
}
