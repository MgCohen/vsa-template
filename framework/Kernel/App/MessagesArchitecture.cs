namespace Umpire.Kernels;

using Umpire.Architecture;

sealed class MessagesArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Block block) {
        block.Add<MessagesParts.Contracts>().And<MessagesParts.Handlers>();
    }
}
