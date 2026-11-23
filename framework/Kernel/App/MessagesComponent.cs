namespace Umpire.Kernels;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public sealed record MessagesComponent : Component {
    public override string Folder => "Messages";

    protected override void Define(Part part) {
        part.Add<MessagesParts.Contracts>().And<MessagesParts.Handlers>();
    }
}
