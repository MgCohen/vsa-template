namespace VSA.Module;

// The AssemblyMetadata keys that VSA.Sdk records in each module root (VSA.Layers.targets, _VsaRecordLayers).
// MSBuild cannot use these constants, so keep the two the same. Other code reads the records through ModuleRoot.
static class ModuleMetadata {
    // Value: the module root assembly name, the same as Module.Name. One record in each module root.
    public const string Module = "VsaModule";

    // Value: the assembly name of one layer project of the module, for example "Notes.Handlers.Requests".
    public const string LayerAssembly = "VsaLayerAssembly";
}
