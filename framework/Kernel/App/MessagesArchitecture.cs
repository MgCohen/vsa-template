namespace Umpire.Kernels;

using Umpire.Architecture;

sealed class MessagesArchitecture : Architecture {
    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root, Part kernel) {
        root.Add<MessagesComponent>().InParentFolder();
    }
}
