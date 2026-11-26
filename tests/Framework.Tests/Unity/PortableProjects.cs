namespace Framework.Tests.Unity;

public static class PortableProjects {
    public static readonly string[] Names = [
        "Umpire",
        "Umpire.Architecture",
        "Umpire.Config",
        "Umpire.Definition",
        "Umpire.Kernels",
        "Umpire.Kernels.Definition",
        "Umpire.Messages.Contracts",
        "Umpire.Messages.Handlers",
        "Umpire.Vsa",
        "Umpire.Vsa.Composition",
        "Umpire.Vsa.Contracts.Commands",
        "Umpire.Vsa.Contracts.Events",
        "Umpire.Vsa.Contracts.Requests",
        "Umpire.Vsa.Definition",
        "Umpire.Vsa.Domain",
        "Umpire.Vsa.Handlers.Commands",
        "Umpire.Vsa.Handlers.Events",
        "Umpire.Vsa.Handlers.Requests",
        "Umpire.Vsa.Infrastructure",
        "Umpire.Vsa.Presenters",
    ];

    public static readonly string[] Analyzers = ["Umpire.Kernels.Analyzers", "Umpire.Vsa.Analyzers"];

    public static readonly string[] OtherNetstandard20Projects = [
        "Umpire.Markers", "Umpire.CheckModel", "Umpire.CheckModel.Analyzer", "Umpire.Sdk", "Umpire.Sdk.Web",
    ];
}
