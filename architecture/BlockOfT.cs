namespace Umpire.Architecture;

[Umpire.Markers.RequireDerived]
public abstract class Block<TArchitecture> where TArchitecture : Architecture, new();
