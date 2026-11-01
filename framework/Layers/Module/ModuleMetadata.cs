namespace VSA.Module;

// The AssemblyMetadata keys that VSA.Sdk records in each module root (VSA.Layers.targets, _VsaRecordLayers).
// They are a public contract: the host, the vsa tool and the architecture rules read them.
public static class ModuleMetadata {
    // Value: the module root assembly name, the same as Module.Name. One record in each module root.
    public const string Module = "VsaModule";

    // Value: the assembly name of one layer project of the module, for example "Notes.Handlers.Requests".
    public const string Layer = "VsaLayer";
}
