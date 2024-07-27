using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

public class VeinBuildCommandHandler : ExecuteTypedResponseCommandHandlerBase<string, string>
{
    public VeinBuildCommandHandler(string command, ISerializer serializer) : base(command, serializer)
    {
    }

    public override Task<string> Handle(string arg1, CancellationToken cancellationToken)
    {
        return Task.FromResult("Success build");
    }
}
