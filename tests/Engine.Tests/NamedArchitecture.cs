namespace Engine.Tests;

using Umpire.Vsa.Definition;

public sealed class NamedArchitecture(string name) : VsaArchitecture {
    public string Name { get; } = name;
}
