namespace SampleModule.Domain.Shared;

// The published domain of SampleModule: other modules read these types. A type here is immutable and holds data only.
public sealed record SampleModuleKey(string Name);
