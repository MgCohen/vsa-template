namespace VSA.Module;

// The AssemblyMetadata keys that VSA.Sdk records in each module root (VSA.Layers.targets, _VsaRecordLayers).
// MSBuild cannot use these constants, so keep the two the same. Other code reads the records through ModuleRoot.
static class ModuleMetadata {
    // Value: the module root assembly name, the same as Module.Name. One record in each module root.
    public const string Module = "VsaModule";

    // Value: the root folder of the module in the app, for example "Modules" or "Plugins". One record in each module root.
    public const string RootFolder = "VsaRootFolder";

    // The root folder of a plugin module: the base of the same layer in each module.
    public const string Plugins = "Plugins";

    // Value: the assembly name of one layer project of the module, for example "Notes.Handlers.Requests".
    public const string LayerAssembly = "VsaLayerAssembly";

    // Value: the name of one module in the VsaModuleUses list of the module root, for example "Resources".
    // The Domain.Shared and Presenters projects of the module use the Domain.Shared project of each listed module.
    public const string ModuleUse = "VsaModuleUse";
}
