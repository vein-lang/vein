namespace ishtar.debugger;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

internal interface IIshtarVariableContainer
{
    string Name { get; }
    int VariableReference { get; }

    IReadOnlyCollection<IIshtarVariableContainer> ChildContainers { get; }
    IReadOnlyCollection<IshtarVariable> Variables { get; }

    VariablesResponse HandleVariablesRequest(VariablesArguments args);
    SetVariableResponse HandleSetVariableRequest(SetVariableArguments arguments);

    IIshtarVariableContainer Container { get; }
}
