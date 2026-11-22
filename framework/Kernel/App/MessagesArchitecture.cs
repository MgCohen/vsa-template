namespace Umpire.Kernels;

using Umpire.Architecture;

sealed class MessagesArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root) {
        root.Add<MessagesParts.Contracts>().And<MessagesParts.Handlers>();
    }
}
