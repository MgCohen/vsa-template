namespace Umpire.Vsa.Testing.Unit;

using Umpire.Architecture;
using Umpire.Vsa.Definition;

public abstract class VsaArchitectureTest<TArchitecture> : ArchitectureTest<TArchitecture>
    where TArchitecture : VsaArchitecture, new() {
    protected static string Feature(string feature) => $"Features/{feature}";

    protected static string Slice(string feature, string slice) => $"Features/{feature}/Slices/{slice}";

    protected static string Plugin(string plugin) => $"Plugins/{plugin}";
}

public abstract class VsaArchitectureTest : VsaArchitectureTest<VsaArchitecture>;
